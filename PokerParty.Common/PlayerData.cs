﻿namespace PokerParty.Common
{
    public class PlayerData
    {
        public string Nickname;
        public bool Online;
        public PlayerState State;
        public Chips Chips;
        public PlayingCard[] Cards = new PlayingCard[2];
        public int Bid;
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
