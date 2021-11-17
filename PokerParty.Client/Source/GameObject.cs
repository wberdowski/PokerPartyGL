using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace PokerParty.Client
{
    public class GameObject : IDisposable
    {
        public Mesh Mesh { get; set; }
        public Vector3 Position { get { return _position; } set { _position = value; UpdateModelMatrix(); } }
        public Quaternion Rotation { get { return _rotation; } set { _rotation = value; UpdateModelMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateModelMatrix(); } }
        public Matrix4 ModelMatrix { get; set; } = Matrix4.Identity;
        public int VAO { get; set; }
        public int VBO { get; set; }

        public Texture Albedo { get; set; }

        private Vector3 _position = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;

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

        public void Draw()
        {
            if (Mesh == null)
            {
                throw new Exception("Mesh cannot be null.");
            }

            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, Mesh.Vertices.Length / 8);
        }

        public void UpdateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(_scale) * Matrix4.CreateFromQuaternion(_rotation) * Matrix4.CreateTranslation(_position);
        }

        public void LoadToBuffer(Shader shader)
        {
            if (Mesh == null)
            {
                throw new Exception("Mesh cannot be null.");
            }

            shader.Use();

            // VAO
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // VBO
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, Mesh.Vertices.Length * sizeof(float), Mesh.Vertices, BufferUsageHint.StaticDraw);

            Console.WriteLine($"Load model: {Mesh.Vertices.Length * sizeof(float):n0} B");

            //// EBO
            //EBO = GL.GenBuffer();
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            //GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.StaticDraw);

            // Attributes
            int vertexLocation = shader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            int texCoordLocation = shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            int normalLocation = shader.GetAttribLocation("aNormal");
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
