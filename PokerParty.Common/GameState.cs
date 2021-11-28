namespace PokerParty.Common
{
    public class GameState
    {
        public bool active;
        public PlayerData[] players;
        public PlayingCard[] cardsOnTheTable;
        public int dealerButtonPos;
        public int turn;
        public int pot;

        public void AdvanceTurn()
        {
            turn = (turn + 1) % players.Length;
            Console.WriteLine("ADVANCE TURN");
        }

        public bool IsPlayerTurn(PlayerData data)
        {
            return (turn == Array.IndexOf(players, data));
        }
    }
}
