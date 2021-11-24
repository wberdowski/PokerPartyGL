using System;

namespace PokerParty.Client
{
    public class Material
    {
        public Shader Shader { get; set; }

        public Material()
        {
        }

        public Material(Shader shader)
        {
            Shader = shader ?? throw new ArgumentNullException(nameof(shader));
        }

        public void Use()
        {
            Shader?.Use();
        }
    }
}
