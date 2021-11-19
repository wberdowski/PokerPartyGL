using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace PokerParty.Client
{
    public class CardObject : GameObject
    {
        public PlayingCard CardType { get; set; }
        public int InstanceVBO;
        public float[] InstanceData;

        public CardObject(Vector3 position) : base(position)
        {

        }

        public void LoadInstanceDataBuffer()
        {
            GL.BindVertexArray(VAO);

            InstanceVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, InstanceVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, InstanceData.Length * sizeof(float), InstanceData, BufferUsageHint.StaticDraw);

            var offsetLoc = Shader.GetAttribLocation("aOffset");
            GL.EnableVertexAttribArray(offsetLoc);
            GL.VertexAttribPointer(offsetLoc, 3, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.VertexAttribDivisor(offsetLoc, 1);

            var texIdLoc = Shader.GetAttribLocation("aTexId");
            GL.EnableVertexAttribArray(texIdLoc);
            GL.VertexAttribPointer(texIdLoc, 1, VertexAttribPointerType.Float, false, 4 * sizeof(float), 3 * sizeof(float));
            GL.VertexAttribDivisor(texIdLoc, 1);

            GL.BindVertexArray(0);
        }

        public override void Draw()
        {
            if (Mesh == null)
            {
                throw new Exception("Mesh cannot be null.");
            }

            GL.BindVertexArray(VAO);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, Mesh.Vertices.Length / 8, InstanceData.Length / 3);
            GL.BindVertexArray(0);
        }
    }
}
