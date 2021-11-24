using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Diagnostics;

namespace PokerParty.Client
{
    public class UIObject : GameObject
    {
        public UILayoutAnchor Anchor { get; set; }
        public int Border { get; internal set; }

        public UIObject() : base()
        {

        }

        public override void UpdateModelMatrix()
        {
            Vector3 offset = default(Vector3);

            if (Anchor == UILayoutAnchor.TopLeft)
            {
                offset = Vector3.Zero;
            }
            else if (Anchor == UILayoutAnchor.TopRight)
            {
                offset = new Vector3(Window.Camera.Bounds.Size.X, 0, 0);
            }
            else if (Anchor == UILayoutAnchor.BottomLeft)
            {
                offset = new Vector3(0, -Window.Camera.Bounds.Size.Y, 0);
            }
            else if (Anchor == UILayoutAnchor.BottomRight)
            {
                offset = new Vector3(Window.Camera.Bounds.Size.X, -Window.Camera.Bounds.Size.Y, 0);
            }

            ModelMatrix = Matrix4.CreateScale(_scale) * Matrix4.CreateFromQuaternion(_rotation) * Matrix4.CreateTranslation(_position + offset);
        }

        public override void Draw()
        {
            if (Mesh == null)
            {
                throw new Exception("Mesh cannot be null.");
            }

            Material.Shader.Use();
            Material.Shader.SetMatrix4("view", Window.Camera.View);
            Material.Shader.SetMatrix4("projection", Window.Camera.ProjectionUI);
            Material.Shader.SetMatrix4("model", ModelMatrix * Matrix4.CreateTranslation(-Window.Camera.Bounds.Size.X / 2f, Window.Camera.Bounds.Size.Y / 2f, 0));
            Material.Shader.SetVec3("size", new Vector3(((PanelMesh)Mesh).Size.X, ((PanelMesh)Mesh).Size.Y, 1));
            Material.Shader.SetVec3("texSize", new Vector3(32, 32, 0));
            Material.Shader.SetInt("border", Border);

            if (TextureType == Texture.TextureType.Texture2D)
            {
                Albedo?.Use();
            }
            else
            {
                Albedo3D?.Use();
            }

            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, Mesh.Vertices.Length / 5);
        }

        public override void LoadToBuffer()
        {
            if (Mesh == null)
            {
                throw new Exception("Mesh cannot be null.");
            }

            if (Material == null)
            {
                throw new Exception("Material cannot be null.");
            }

            Material.Use();

            // VAO
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // VBO
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, Mesh.Vertices.Length * sizeof(float), Mesh.Vertices, BufferUsageHint.StaticDraw);

            Debug.WriteLine($"Load model: {Mesh.Vertices.Length * sizeof(float):n0} B");

            // Attributes
            int vertexLocation = Material.Shader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            int texCoordLocation = Material.Shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        }

        internal void DeleteBuffer()
        {
            GL.DeleteBuffer(VBO);
        }
    }
}
