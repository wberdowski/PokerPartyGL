using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Diagnostics;
using static PokerParty.Client.Texture;

namespace PokerParty.Client
{
    public class GameObject : IDisposable
    {
        public RenderLayer Layer { get; set; } = RenderLayer.Standard;
        public Mesh Mesh { get; set; }
        public Vector3 Position { get { return _position; } set { _position = value; UpdateModelMatrix(); } }
        public Quaternion Rotation { get { return _rotation; } set { _rotation = value; UpdateModelMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateModelMatrix(); } }
        public Matrix4 ModelMatrix { get; protected set; } = Matrix4.Identity;
        public Matrix4 InstanceMatrix { get; set; } = Matrix4.Identity;
        public Material Material { get; set; }
        public int VAO { get; set; }
        public int VBO { get; set; }

        public Texture Albedo { get; set; }
        public Texture3D Albedo3D { get; set; }
        public TextureType TextureType { get; set; }

        internal Vector3 _position = Vector3.Zero;
        internal Quaternion _rotation = Quaternion.Identity;
        internal Vector3 _scale = Vector3.One;

        public GameObject()
        {
        }

        public GameObject(Vector3 position)
        {
            Position = position;
        }

        public GameObject(Vector3 position, Quaternion rotation) : this(position)
        {
            Rotation = rotation;
        }

        public virtual void Draw()
        {
            if (Mesh == null)
            {
                throw new Exception("Mesh cannot be null.");
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
            GL.DrawArrays(PrimitiveType.Triangles, 0, Mesh.Vertices.Length / 8);
        }

        public virtual void UpdateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(_scale) * Matrix4.CreateFromQuaternion(_rotation) * Matrix4.CreateTranslation(_position) * InstanceMatrix;
        }

        public virtual void LoadToBuffer()
        {
            if (Mesh == null)
            {
                throw new Exception("Mesh cannot be null.");
            }

            if (Material == null)
            {
                throw new Exception("Material cannot be null.");
            }

            Material.Shader.Use();

            // VAO
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // VBO
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, Mesh.Vertices.Length * sizeof(float), Mesh.Vertices, BufferUsageHint.StaticDraw);

            Debug.WriteLine($"Load model: {Mesh.Vertices.Length * sizeof(float):n0} B");

            //// EBO
            //EBO = GL.GenBuffer();
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            //GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.StaticDraw);

            // Attributes
            int vertexLocation = Material.Shader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            int texCoordLocation = Material.Shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            int normalLocation = Material.Shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 5 * sizeof(float));
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VAO);
            GL.DeleteBuffer(VBO);
        }
    }
}
