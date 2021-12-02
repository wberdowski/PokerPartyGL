using PokerParty.Common;

namespace PokerParty.Common
{
    internal static class CardDeck
    {
        public static List<PlayingCard> Deck { get; private set; }
        private static Random rand = new Random();

        public static PlayingCard DrawOne()
        {
            if (Deck == null || Deck.Count == 0)
            {
                throw new Exception("Deck is empty or null.");
            }

            var card = Deck[0];
            Deck.RemoveAt(0);
            return card;
        }

        public static void ResetAndShuffle()
        {
            Reset();
            Shuffle();
        }

        public static void Reset()
        {
            // Init
            Deck = new List<PlayingCard>(52);
            var colors = Enum.GetNames(typeof(PlayingCard.CardColor));
            var colorsV = Enum.GetValues(typeof(PlayingCard.CardColor));
            var values = Enum.GetNames(typeof(PlayingCard.CardValue));
            var valuesV = Enum.GetValues(typeof(PlayingCard.CardValue));

            for (int i = 0; i < colors.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    Deck.Add(new PlayingCard((PlayingCard.CardColor)colorsV.GetValue(i), (PlayingCard.CardValue)valuesV.GetValue(j)));
                }
            }
        }

        public static void Shuffle()
        {
            int n = Deck.Count;
            while (n > 1)
            {
                int k = rand.Next(n--);
                var temp = Deck[n];
                Deck[n] = Deck[k];
                Deck[k] = temp;
            }
        }
    }
}
