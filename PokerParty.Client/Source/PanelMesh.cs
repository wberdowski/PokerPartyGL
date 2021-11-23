using OpenTK.Mathematics;

namespace PokerParty.Client
{
    public class PanelMesh : Mesh
    {
        public Vector2 Size { get; set; }

        public PanelMesh(Vector2 size)
        {
            Size = size;
            Vertices = new float[]{
                0, -Size.Y, -1f, 0, 1,
                Size.X , -Size.Y, -1f, 1, 1,
                Size.X , 0, -1f, 1, 0,
                0 , 0, -1f, 0, 0,
            };
        }
    }
}
