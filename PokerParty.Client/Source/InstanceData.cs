using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace PokerParty.Client
{
    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceData
    {
        public Matrix4 mat;
        public float texId;

        public InstanceData(Matrix4 mat, float texId)
        {
            this.mat = mat;
            this.texId = texId;
        }
    }
}
