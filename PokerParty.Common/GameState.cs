namespace PokerParty.Common
{
    public class GameState
    {
        public bool active;
        public PlayerData[] players;
        public PlayingCard[] shownTableCards;
        [NonSerialized]
        public PlayingCard[] allTableCards;
        public int dealerButtonPos;
        public int turn;
        public int pot;
        public int bet;

        public void AdvanceTurn()
        {
            if (!active)
            {
                throw new Exception("Game has not started.");
            }

            if (turn + 1 >= players.Length)
            {
                dealerButtonPos++;
            }

            turn = (turn + 1) % players.Length;
            Console.WriteLine("ADVANCE TURN");
        }

        public static GameState StartNew(PlayerData[] players)
        {
            if (players.Length < 2)
            {
                throw new Exception("Too few players to start a game.");
            }

            CardDeck.ResetAndShuffle();

            var gameState = new GameState();
            gameState.active = true;
            gameState.dealerButtonPos = players.Length - 1;
            gameState.players = players;

            foreach (var p in gameState.players)
            {
                p.Cards[0] = CardDeck.DrawOne();
                p.Cards[1] = CardDeck.DrawOne();
                p.State = PlayerState.Playing;
            }

            gameState.allTableCards = new PlayingCard[]
            {
                    CardDeck.DrawOne(),
                    CardDeck.DrawOne(),
                    CardDeck.DrawOne(),
                    CardDeck.DrawOne(),
                    CardDeck.DrawOne()
            };

            gameState.shownTableCards = new PlayingCard[]
            {
                    PlayingCard.Back,
                    PlayingCard.Back,
                    PlayingCard.Back,
                    PlayingCard.Back,
                    PlayingCard.Back
            };

            Console.WriteLine("NEW GAME STARTED");

            return gameState;
        }



        public bool IsPlayerTurn(PlayerData data)
        {
            return (turn == Array.IndexOf(players, data));
        }
    }
}
