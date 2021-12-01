namespace PokerParty.Common
{
    public class PlayerData
    {
        public string Nickname;
        public bool Online;
        public PlayerState State;
        // TODO: Dont broadcast others cards
        public PlayingCard[] Cards = new PlayingCard[2];
        public int Bet;
        public int Balance;

        public PlayerData()
        {
        }

        public PlayerData(string nickname)
        {
            Nickname = nickname;
            Online = true;
        }
    }
}
