using BitSerializer;
using PokerParty.Common;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static PokerParty.Client.Window;
using static PokerParty.Common.ControlPacket;

namespace PokerParty.Client
{
    public static class NetClient
    {
        public static GameState gameState;
        public static bool IsConnected => (controlSocket != null && controlSocket.Connected);

        private static Socket controlSocket;
        private static byte[] recvBuff = new byte[16 * 1024];

        // NET STATE
        private static bool isAutorized;

        public static void Init()
        {
            controlSocket = new Socket(SocketType.Stream, ProtocolType.IP);
            controlSocket.ReceiveTimeout = 10000;
            controlSocket.SendTimeout = 10000;
        }

        public static void Connect()
        {
            controlSocket.BeginConnect(new IPEndPoint(IPAddress.Loopback, 55555), OnConnect, null);
        }

        private static void OnConnect(IAsyncResult ar)
        {
            try
            {
                controlSocket.EndConnect(ar);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine("Cannot connect");
            }

            if (controlSocket.Connected)
            {
                Debug.WriteLine("Connected.");

                SendLoginRequest();
            }
        }

        private static async void SendLoginRequest()
        {
            var packet = new ControlPacket(OpCode.LoginRequest, Encoding.UTF8.GetBytes(Username));
            await controlSocket.SendAsync(BinarySerializer.Serialize(packet), SocketFlags.None);

            controlSocket.BeginReceive(recvBuff, 0, recvBuff.Length, SocketFlags.None, OnControlReceive, null);
        }

        private static void OnControlReceive(IAsyncResult ar)
        {
            int len = 0;

            try
            {
                controlSocket.EndReceive(ar);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine("Disconnected from server.");
                return;
            }

            var resPacket = BinarySerializer.Deserialize<ControlPacket>(recvBuff);

            if (resPacket.Code == OpCode.LoginResponse)
            {
                if (resPacket.Status == OpStatus.Success)
                {
                    isAutorized = true;
                    Debug.WriteLine($"Nickname \"{Username}\" registered.");
                }
                else if (resPacket.Status == OpStatus.Failure)
                {
                    Debug.WriteLine($"Nickname register error: {resPacket.GetError()}.");
                }
            }
            else if (resPacket.Code == OpCode.GameStateUpdate)
            {
                gameState = BinarySerializer.Deserialize<GameState>(resPacket.Payload);
                gameStateUpdatePending = true;
            }

            controlSocket.BeginReceive(recvBuff, 0, recvBuff.Length, SocketFlags.None, OnControlReceive, null);
        }

    }
}
