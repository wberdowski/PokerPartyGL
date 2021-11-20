using BitSerializer;
using PokerParty.Common;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using static PokerParty.Common.ControlPacket;

namespace PokerParty.Server
{
    public class Program
    {
        private static Socket controlSocket;
        private static IPEndPoint controlEp;
        private static byte[] recvBuff = new byte[16 * 1024];
        private static Dictionary<Socket, ClientData> clients = new Dictionary<Socket, ClientData>();

        public static void Main(string[] args)
        {
            Console.WriteLine($"PokerParty Server v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}");
            Console.WriteLine("Starting server...");

            controlSocket = new Socket(SocketType.Stream, ProtocolType.IP);

            int size = Marshal.SizeOf(new uint());
            var inOptionValues = new byte[size * 3];
            BitConverter.GetBytes((uint)(true ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)10000).CopyTo(inOptionValues, size);
            BitConverter.GetBytes((uint)3000).CopyTo(inOptionValues, size * 2);
            controlSocket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);

            controlEp = new IPEndPoint(IPAddress.Any, 55555);
            controlSocket.Bind(controlEp);
            controlSocket.Listen(5);
            controlSocket.BeginAccept(OnControlAccept, null);

            Console.WriteLine("Server started.\nType \"stop\" to save and stop the server.");

            while (true)
            {
                var cmd = Console.ReadLine();
                var cmdArgs = cmd.Split(' ');

                if (cmdArgs.Length > 0)
                {
                    if (cmdArgs[0] == "stop")
                    {
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"Unknown command: {cmdArgs[0]}");
                    }
                }
            }

            Console.WriteLine("Stopping server...");

            foreach (var client in clients.Keys)
            {
                client.Close();
                client.Dispose();
            }

            controlSocket.Close();
            controlSocket.Shutdown(SocketShutdown.Both);
            controlSocket.Dispose();

            Console.WriteLine("Server stopped.");
        }

        private static void OnControlAccept(IAsyncResult ar)
        {
            var clientSocket = controlSocket.EndAccept(ar);
            clients.Add(clientSocket, new ClientData());

            Console.WriteLine($"Client connected: {(IPEndPoint)clientSocket.RemoteEndPoint}");

            clientSocket.BeginReceive(recvBuff, 0, recvBuff.Length, SocketFlags.None, OnControlReceive, clientSocket);

            controlSocket.BeginAccept(OnControlAccept, null);
        }

        private static void OnControlReceive(IAsyncResult ar)
        {
            var clientSocket = (Socket)ar.AsyncState;

            lock (recvBuff)
            {
                int len = 0;

                try
                {
                    len = clientSocket.EndReceive(ar);
                }
                catch (SocketException ex)
                {
                    DisconnectClient(clientSocket);
                    return;
                }

                if (!clientSocket.Connected || clientSocket.Poll(0, SelectMode.SelectRead))
                {
                    DisconnectClient(clientSocket);
                    return;
                }

                Console.WriteLine($"Received {len} bytes from {(IPEndPoint)clientSocket.RemoteEndPoint}: {Encoding.UTF8.GetString(recvBuff, 0, len)}");

                var packet = BinarySerializer.Deserialize<ControlPacket>(recvBuff);
                Console.WriteLine(packet);

                if (packet.Code == OpCode.LoginRequest)
                {
                    var username = Encoding.UTF8.GetString(packet.Payload);

                    if (username.Length < 3 || username.Length > 32)
                    {
                        var resPacket = new ControlPacket(OpCode.LoginResponse, OpStatus.Failure, ErrorCode.PlayerNicknameIsInvalid);
                        clientSocket.Send(BinarySerializer.Serialize(resPacket));
                    }
                    else if (clients.Values.Where(x => x.PlayerData != null && x.PlayerData.Nickname == username).Count() > 0)
                    {
                        var resPacket = new ControlPacket(OpCode.LoginResponse, OpStatus.Failure, ErrorCode.PlayerNicknameTaken);
                        clientSocket.Send(BinarySerializer.Serialize(resPacket));
                    }
                    else
                    {
                        if (clients.TryGetValue(clientSocket, out var data))
                        {
                            data.Authorized = true;
                            data.PlayerData = new PlayerData(username);
                            var resPacket = new ControlPacket(OpCode.LoginResponse, OpStatus.Success);
                            clientSocket.Send(BinarySerializer.Serialize(resPacket));
                        }
                        else
                        {
                            var resPacket = new ControlPacket(OpCode.LoginResponse, OpStatus.Failure, ErrorCode.Unknown);
                            clientSocket.Send(BinarySerializer.Serialize(resPacket));
                        }
                    }
                }
            }

            clientSocket.BeginReceive(recvBuff, 0, recvBuff.Length, SocketFlags.None, OnControlReceive, clientSocket);
        }

        private static void DisconnectClient(Socket clientSocket)
        {
            Console.WriteLine($"Client disconnected: {(IPEndPoint)clientSocket.RemoteEndPoint}");
            clients.Remove(clientSocket);
            clientSocket.Close();
            clientSocket.Dispose();
        }
    }
}