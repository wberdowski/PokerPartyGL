using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace PokerParty.Client
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CardInstanceData
    {
        public Matrix4 mat;
        public float texId;

        public CardInstanceData(Matrix4 mat, float texId)
        {
            this.mat = mat;
            this.texId = texId;
        }
    }
}
