namespace PokerParty.Client
{
    public struct PlayingCard
    {
        public CardColor Color { get; set; }
        public CardValue Value { get; set; }

        public PlayingCard(CardColor color, CardValue value)
        {
            Color = color;
            Value = value;
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
            Num2 = 2,
            Num3 = 3,
            Num4 = 4,
            Num5 = 5,
            Num6 = 6,
            Num7 = 7,
            Num8 = 8,
            Num9 = 9,
            Num10 = 10,
            Jack = 11,
            Queen = 12,
            King = 13,
            Ace = 14,
        }
    }
}
