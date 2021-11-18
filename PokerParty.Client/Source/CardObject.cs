using OpenTK.Mathematics;

namespace PokerParty.Client
{
    public class CardObject : GameObject
    {
        public PlayingCard CardType { get; set; }

        public CardObject(Vector3 position) : base(position)
        {
        }
    }
}
