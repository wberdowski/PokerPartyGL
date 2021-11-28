using BitSerializer;
using PokerParty.Common;
using System.Net.Sockets;
using System.Text;
using static PokerParty.Common.ControlPacket;
using static PokerParty.Server.Program;

namespace PokerParty.Server
{
    internal static class Commands
    {
        public static Dictionary<OpCode, CommandDelegate> Registered = new Dictionary<OpCode, CommandDelegate>();
        public delegate void CommandDelegate(Socket clientSocket, ControlPacket packet);

        public static void RegisterAll()
        {
            Register(OpCode.LoginRequest, OnLoginRequest);
            Register(OpCode.RaiseRequest, OnRaiseRequest);
            Register(OpCode.FoldRequest, OnFoldRequest);
            Register(OpCode.CheckRequest, OnCheckRequest);
        }

        #region Commands

        private static void OnLoginRequest(Socket clientSocket, ControlPacket packet)
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
                if (!EnsureClientData(clientSocket, packet, out var data)) return;

                // Successfully registered
                data.Authorized = true;
                data.PlayerData = new PlayerData(username);
                data.PlayerData.Balance = 10000;
                GenChips(data.PlayerData);

                data.PlayerData.Cards[0] = new PlayingCard(PlayingCard.CardColor.Spades, PlayingCard.CardValue.Ace);
                data.PlayerData.Cards[1] = new PlayingCard(PlayingCard.CardColor.Hearts, PlayingCard.CardValue.Queen);
                data.PlayerData.State = PlayerState.Playing;

                GenGameState();

                var resPacket = new ControlPacket(OpCode.LoginResponse, OpStatus.Success);
                NonBlockingSend(clientSocket, BinarySerializer.Serialize(resPacket));
                BroadcastGameState();
            }
        }

        private static void OnRaiseRequest(Socket clientSocket, ControlPacket packet)
        {
            if (!EnsureGameActive(clientSocket, packet)) return;
            if (!EnsureClientData(clientSocket, packet, out var data)) return;
            if (!EnsurePlayerTurn(clientSocket, packet, data.PlayerData)) return;
            if (!EnsureNotFolded(clientSocket, packet, data.PlayerData)) return;

            int amount = BitConverter.ToInt32(packet.Payload);
            // TODO: Check if player has money
            data.PlayerData.Bid += amount;
            data.PlayerData.Balance -= amount;
            gameState.pot += amount;
            GenChips(data.PlayerData);
            gameState.AdvanceTurn();
            BroadcastGameState();
        }

        private static void OnFoldRequest(Socket clientSocket, ControlPacket packet)
        {
            if (!EnsureGameActive(clientSocket, packet)) return;
            if (!EnsureClientData(clientSocket, packet, out var data)) return;
            if (!EnsurePlayerTurn(clientSocket, packet, data.PlayerData)) return;
            if (!EnsureNotFolded(clientSocket, packet, data.PlayerData)) return;

            data.PlayerData.State = PlayerState.Folded;
            gameState.AdvanceTurn();
            BroadcastGameState();
        }

        private static void OnCheckRequest(Socket clientSocket, ControlPacket packet)
        {
            if (!EnsureGameActive(clientSocket, packet)) return;
            if (!EnsureClientData(clientSocket, packet, out var data)) return;
            if (!EnsurePlayerTurn(clientSocket, packet, data.PlayerData)) return;
            if (!EnsureNotFolded(clientSocket, packet, data.PlayerData)) return;

            gameState.AdvanceTurn();
            BroadcastGameState();
        }

        #endregion

        #region Validation

        private static bool EnsureGameActive(Socket clientSocket, ControlPacket packet)
        {
            if (gameState == null || !gameState.active)
            {
                // Game not started
                var resPacket = new ControlPacket(packet.Code, OpStatus.Failure, ErrorCode.GameNotStarted);
                NonBlockingSend(clientSocket, BinarySerializer.Serialize(resPacket));
                return false;
            }

            return true;
        }

        private static bool EnsurePlayerTurn(Socket clientSocket, ControlPacket packet, PlayerData data)
        {
            if (!gameState.IsPlayerTurn(data))
            {
                // Not players turn
                var resPacket = new ControlPacket(packet.Code, OpStatus.Failure, ErrorCode.NotPlayersTurn);
                NonBlockingSend(clientSocket, BinarySerializer.Serialize(resPacket));
                return false;
            }

            return true;
        }

        private static bool EnsureNotFolded(Socket clientSocket, ControlPacket packet, PlayerData data)
        {
            if (data.State == PlayerState.Folded)
            {
                // Player already folded
                var resPacket = new ControlPacket(packet.Code, OpStatus.Failure, ErrorCode.PlayerAlreadyFolded);
                NonBlockingSend(clientSocket, BinarySerializer.Serialize(resPacket));
                return false;
            }

            return true;
        }


        private static bool EnsureClientData(Socket clientSocket, ControlPacket packet, out ClientData data)
        {
            if (!clients.TryGetValue(clientSocket, out data))
            {
                // Player not registered
                var resPacket = new ControlPacket(packet.Code, OpStatus.Failure, ErrorCode.Unknown);
                NonBlockingSend(clientSocket, BinarySerializer.Serialize(resPacket));
                return false;
            }

            return true;
        }

        #endregion

        public static void Register(OpCode code, CommandDelegate command)
        {
            Registered.Add(code, command);
        }

        public static void Unregister(OpCode code)
        {
            Registered.Remove(code);
        }
    }
}
