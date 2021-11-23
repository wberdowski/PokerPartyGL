namespace PokerParty.Common
{
    public struct Chips
    {
        public int this[ChipColor color]
        {
            get => Amounts[(int)color];
            set => Amounts[(int)color] = value;
        }

        public int[] Amounts = new int[typeof(ChipColor).GetEnumValues().Length];

        public Chips()
        {
        }

        public enum ChipColor
        {
            Black,
            Red,
            Green,
            Blue,
            White
        }
    }
}
