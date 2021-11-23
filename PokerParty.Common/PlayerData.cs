namespace PokerParty.Common
{
    public class PlayerData
    {
        public string Nickname;
        public bool Online;
        public Chips Chips;

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
