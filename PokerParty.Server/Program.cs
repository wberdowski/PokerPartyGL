using BitSerializer;
using PokerParty.Common;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using static PokerParty.Common.Chips;
using static PokerParty.Common.ControlPacket;

namespace PokerParty.Server
{
    public class Program
    {
        private static Socket controlSocket;
        private static IPEndPoint controlEp;
        private static byte[] recvBuff = new byte[16 * 1024];
        private static Dictionary<Socket, ClientData> clients = new Dictionary<Socket, ClientData>();
        private static Random rand = new Random();

        private static GameState gameState;

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
                    if (cmdArgs[0] == "stop" || cmdArgs[0] == "")
                    {
                        return;
                    }
                    else if (cmdArgs[0] == "s")
                    {
                        BroadcastGameState();
                    }
                    else
                    {
                        Console.WriteLine($"Unknown command: {cmdArgs[0]}");
                    }
                }
            }

            Console.WriteLine("Stopping server...");

            lock (clients)
            {
                foreach (var client in clients.Keys)
                {
                    client.Close();
                    client.Dispose();
                }
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
                        // Nickname length invalid
                        var resPacket = new ControlPacket(OpCode.LoginResponse, OpStatus.Failure, ErrorCode.PlayerNicknameIsInvalid);
                        NonBlockingSend(clientSocket, BinarySerializer.Serialize(resPacket));
                    }
                    else if (clients.Values.Where(x => x.PlayerData != null && x.PlayerData.Nickname == username).Count() > 0)
                    {
                        // Nickname taken
                        var resPacket = new ControlPacket(OpCode.LoginResponse, OpStatus.Failure, ErrorCode.PlayerNicknameTaken);
                        NonBlockingSend(clientSocket, BinarySerializer.Serialize(resPacket));
                    }
                    else
                    {
                        if (clients.TryGetValue(clientSocket, out var data))
                        {
                            // Successfully registered
                            data.Authorized = true;
                            data.PlayerData = new PlayerData(username);

                            GenGameState();
                            gameState.players.Last().Chips = new Chips();
                            gameState.players.Last().Chips[ChipColor.Black] = 5;
                            gameState.players.Last().Chips[ChipColor.Red] = 10;
                            gameState.players.Last().Chips[ChipColor.Green] = 6;
                            gameState.players.Last().Chips[ChipColor.Blue] = 4;
                            gameState.players.Last().Chips[ChipColor.White] = 2;

                            var resPacket = new ControlPacket(OpCode.LoginResponse, OpStatus.Success);
                            NonBlockingSend(clientSocket, BinarySerializer.Serialize(resPacket));
                            BroadcastGameState();
                        }
                        else
                        {
                            // Unknown error
                            var resPacket = new ControlPacket(OpCode.LoginResponse, OpStatus.Failure, ErrorCode.Unknown);
                            NonBlockingSend(clientSocket, BinarySerializer.Serialize(resPacket));
                        }
                    }
                }
            }

            clientSocket.BeginReceive(recvBuff, 0, recvBuff.Length, SocketFlags.None, OnControlReceive, clientSocket);
        }

        [Obsolete()]
        private static void GenGameState()
        {
            gameState = new GameState();
            gameState.isActive = true;
            gameState.players = clients.Values.Where(x => x.Authorized).Select(x => x.PlayerData).ToArray();

            gameState.cardsOnTheTable = new PlayingCard[]
            {
                //PlayingCard.GetByIndex((byte)rand.Next(0, 51)),
                //PlayingCard.GetByIndex((byte)rand.Next(0, 51)),
                //PlayingCard.GetByIndex((byte)rand.Next(0, 51)),
                //PlayingCard.GetByIndex((byte)rand.Next(0, 51)),
                PlayingCard.Back,
                PlayingCard.Back,
                PlayingCard.Back,
                PlayingCard.Back,
                PlayingCard.GetByIndex((byte)rand.Next(0, 51))
            };
        }

        private static void DisconnectClient(Socket clientSocket)
        {
            Console.WriteLine($"Client disconnected: {(IPEndPoint)clientSocket.RemoteEndPoint}");

            // Broadcast new game state

            if (clients.TryGetValue(clientSocket, out var clientData))
            {
                clients.Remove(clientSocket);

                if (clientData.PlayerData != null && gameState != null)
                {
                    var playerInstance = gameState.players.Where(x => x.Nickname == clientData.PlayerData.Nickname);

                    foreach (var player in playerInstance)
                    {
                        player.Online = false;
                    }

                    BroadcastGameState();
                }
            }

            clientSocket.Close();
            clientSocket.Dispose();
        }

        private static void BroadcastGameState()
        {
            var payload = BinarySerializer.Serialize(gameState);
            var packet = BinarySerializer.Serialize(new ControlPacket(OpCode.GameStateUpdate, payload));

            lock (clients)
            {
                foreach (var c in clients)
                {
                    NonBlockingSend(c.Key, packet);
                }
            }
        }

        private static void SendGameState(Socket clientSocket)
        {
            var payload = BinarySerializer.Serialize(gameState);
            var packet = BinarySerializer.Serialize(new ControlPacket(OpCode.GameStateUpdate, payload));

            NonBlockingSend(clientSocket, packet);
        }

        private static void NonBlockingSend(Socket socket, byte[] data)
        {
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, OnSend, socket);
        }

        private static void OnSend(IAsyncResult ar)
        {
            var clientSocket = (Socket)ar.AsyncState;

            try
            {
                clientSocket.EndSend(ar);
            }
            catch (SocketException ex)
            {
                DisconnectClient(clientSocket);
                return;
            }
        }
    }
}