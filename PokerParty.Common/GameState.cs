namespace PokerParty.Common
{
    public class GameState
    {
        public bool isActive;
        public PlayerData[] players;
        public PlayingCard[] cardsOnTheTable;
        public int dealerButtonPos;
    }
}
