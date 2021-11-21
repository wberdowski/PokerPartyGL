namespace PokerParty.Common
{
    public class PlayerData
    {
        public string Nickname { get; }
        public bool Online { get; set; }

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
