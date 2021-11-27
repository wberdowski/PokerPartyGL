namespace PokerParty.Common
{
    public class GameState
    {
        public bool active;
        public PlayerData[] players;
        public PlayingCard[] cardsOnTheTable;
        public int dealerButtonPos;
    }
}
