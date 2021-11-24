using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Runtime.InteropServices;

namespace PokerParty.Client
{
    public class InstanceCollection : GameObject
    {
        public int? InstanceVBO;
        public InstanceData[] Instances;

        public InstanceCollection(Vector3 position) : base(position)
        {
            TextureType = Texture.TextureType.Texture3D;
        }

        public void UpdateInstanceDataBuffer()
        {
            if (InstanceVBO != null)
            {
                GL.DeleteBuffer((int)InstanceVBO);
            }

            GL.BindVertexArray(VAO);

            var structSize = Marshal.SizeOf<InstanceData>();

            InstanceVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, (int)InstanceVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, Instances.Length * structSize, Instances, BufferUsageHint.StaticDraw);

            var aMatLoc = Material.Shader.GetAttribLocation("aMat");

            for (int i = 0; i < 4; i++)
            {
                GL.EnableVertexAttribArray(aMatLoc + i);
                GL.VertexAttribPointer(aMatLoc + i, 4, VertexAttribPointerType.Float, false, structSize, i * 4 * sizeof(float));
                GL.VertexAttribDivisor(aMatLoc + i, 1);
            }

            var texIdLoc = Material.Shader.GetAttribLocation("aTexId");
            GL.EnableVertexAttribArray(texIdLoc);
            GL.VertexAttribPointer(texIdLoc, 1, VertexAttribPointerType.Float, false, structSize, 16 * sizeof(float));
            GL.VertexAttribDivisor(texIdLoc, 1);

            GL.BindVertexArray(0);
        }

        public override void Draw()
        {
            if (Mesh == null)
            {
                throw new Exception("Mesh cannot be null.");
            }

            if (Instances == null)
            {
                return;
            }

            Material.Shader.Use();
            Material.Shader.SetMatrix4("view", Window.Camera.View);
            Material.Shader.SetMatrix4("projection", Window.Camera.Projection);
            Material.Shader.SetMatrix4("model", ModelMatrix);

            if (TextureType == Texture.TextureType.Texture2D)
            {
                Albedo?.Use();
            }
            else
            {
                Albedo3D?.Use();
            }

            GL.BindVertexArray(VAO);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, Mesh.Vertices.Length / 8, Instances.Length);
            GL.BindVertexArray(0);
        }
    }
}
