namespace PokerParty.Common
{
    public struct ControlPacket
    {
        public OpCode Code { get; set; }
        public OpStatus Status { get; set; }
        public byte[] Payload { get; set; }

        public ControlPacket(OpCode code, OpStatus status)
        {
            Code = code;
            Status = status;
            Payload = new byte[0];
        }


        public ControlPacket(OpCode code, byte[] payload)
        {
            Code = code;
            Status = OpStatus.Unknown;
            Payload = payload;
        }

        public ControlPacket(OpCode code, OpStatus status, byte[] payload)
        {
            Code = code;
            Status = status;
            Payload = payload;
        }


        public ControlPacket(OpCode code, OpStatus status, ErrorCode error)
        {
            Code = code;
            Status = status;
            Payload = new byte[] { (byte)error };
        }

        public ErrorCode GetError()
        {
            return (ErrorCode)Payload[0];
        }

        public override string ToString()
        {
            return $"{Code}, {Status}, {BitConverter.ToString(Payload)}";
        }

        public enum OpCode : byte
        {
            Unknown,
            LoginRequest,
            LoginResponse,
            GameStateUpdate,
        }

        public enum OpStatus : byte
        {
            Unknown,
            Success,
            Failure
        }

        public enum ErrorCode : byte
        {
            Unknown,
            PlayerNicknameTaken,
            PlayerNicknameIsInvalid,
        }
    }
}
