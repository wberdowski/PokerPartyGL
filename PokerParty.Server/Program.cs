using BitSerializer;
using PokerParty.Common;
using System.Diagnostics;
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
        internal static Socket controlSocket;
        internal static IPEndPoint controlEp;
        internal static byte[] recvBuff = new byte[16 * 1024];
        internal static Dictionary<Socket, ClientData> clients = new Dictionary<Socket, ClientData>();
        internal static Random rand = new Random();
        internal static GameState gameState;

#if DEBUG
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(int hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
#endif

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

            Commands.RegisterAll();

            Console.WriteLine("Server started.\nType \"stop\" to save and stop the server.");

            while (true)
            {
                var cmd = Console.ReadLine();
                var cmdArgs = cmd.Split(' ');

                if (cmdArgs.Length > 0)
                {
                    // TODO: Remove ""
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

                var packet = BinarySerializer.Deserialize<ControlPacket>(recvBuff);

                foreach (var cmd in Commands.Registered)
                {
                    if (cmd.Key == packet.Code)
                    {
                        cmd.Value.Invoke(clientSocket, packet);
                    }
                }
            }

            clientSocket.BeginReceive(recvBuff, 0, recvBuff.Length, SocketFlags.None, OnControlReceive, clientSocket);
        }

        [Obsolete()]
        internal static void GenGameState()
        {
            CardDeck.ResetAndShuffle();

            gameState = new GameState();
            gameState.players = clients.Values.Where(x => x.Authorized).Select(x => x.PlayerData).ToArray();
            gameState.allTableCards = new PlayingCard[0];
            gameState.shownTableCards = new PlayingCard[0];

            foreach (var p in gameState.players)
            {
                p.Cards[0] = CardDeck.DrawOne();
                p.Cards[1] = CardDeck.DrawOne();
                p.State = PlayerState.Playing;
            }

            // Check if there are 2 or more players
            if (clients.Values.Where(x => x.Authorized).Count() >= 2)
            {
                gameState.active = true;
                gameState.dealerButtonPos = 0;

                gameState.allTableCards = new PlayingCard[]
                {
                    CardDeck.DrawOne(),
                    CardDeck.DrawOne(),
                    CardDeck.DrawOne(),
                    CardDeck.DrawOne(),
                    CardDeck.DrawOne()
                };

                gameState.shownTableCards = new PlayingCard[]
                {
                    PlayingCard.Back,
                    PlayingCard.Back,
                    PlayingCard.Back,
                    PlayingCard.Back,
                    PlayingCard.Back
                };

                Console.WriteLine("NEW GAME STARTED");
            }
        }

        internal static void DisconnectClient(Socket clientSocket)
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

            // TODO: Dont exit on 0 players
#if DEBUG
            if (clients.Count() == 0)
            {
                Environment.Exit(0);
            }
#endif
        }

        internal static void BroadcastGameState()
        {
            if (gameState != null)
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
        }

        [Obsolete]
        private static void SendGameState(Socket clientSocket)
        {
            var payload = BinarySerializer.Serialize(gameState);
            var packet = BinarySerializer.Serialize(new ControlPacket(OpCode.GameStateUpdate, payload));

            NonBlockingSend(clientSocket, packet);
        }

        internal static void NonBlockingSend(Socket socket, byte[] data)
        {
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, OnSend, socket);
        }

        internal static void OnSend(IAsyncResult ar)
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