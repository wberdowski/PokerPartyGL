namespace PokerParty.Client
{
    public struct PlayingCard
    {
        public CardColor Color { get; }
        public CardValue Value { get; }
        public byte Index { get; }

        public PlayingCard(CardColor color, CardValue value)
        {
            Color = color;
            Value = value;
            Index = (byte)((byte)color * 13 + (byte)value);
        }

        public static PlayingCard GetByIndex(byte index)
        {
            return new PlayingCard((CardColor)(index / 13), (CardValue)(index % 13));
        }

        public static byte GetIndexByColorValue(CardColor color, CardValue value)
        {
            return (byte)((byte)color * 13 + (byte)value);
        }

        public enum CardColor : byte
        {
            Clubs,
            Diamonds,
            Hearts,
            Spades
        }

        public enum CardValue : byte
        {
            Num2,
            Num3,
            Num4,
            Num5,
            Num6,
            Num7,
            Num8,
            Num9,
            Num10,
            Jack,
            Queen,
            King,
            Ace,
        }
    }
}
