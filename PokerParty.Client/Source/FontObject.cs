using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace PokerParty.Client
{
    public class FontObject : GameObject
    {
        public string Text { get; private set; }
        public Vector2i Size { get; private set; }
        public Bitmap Image { get; private set; }
        public UILayoutAnchor Anchor { get; set; }

        public FontObject(string text, Font font, Brush brush) : base()
        {
            Generate(text, font, brush);
            Layer = RenderLayer.UI;
            Anchor = UILayoutAnchor.TopLeft;
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
                offset = new Vector3(Game.Camera.Bounds.Size.X, 0, 0);
            }
            else if (Anchor == UILayoutAnchor.BottomLeft)
            {
                offset = new Vector3(0, -Game.Camera.Bounds.Size.Y, 0);
            }
            else if (Anchor == UILayoutAnchor.BottomRight)
            {
                offset = new Vector3(Game.Camera.Bounds.Size.X, -Game.Camera.Bounds.Size.Y, 0);
            }

            ModelMatrix = Matrix4.CreateScale(_scale) * Matrix4.CreateFromQuaternion(_rotation) * Matrix4.CreateTranslation(_position + offset);
        }

        public void Generate(string text, Font font, Brush brush)
        {
            Text = text;

            SizeF sizef = Graphics.FromHwnd(IntPtr.Zero).MeasureString(Text, font);
            Size = new Vector2i((int)Math.Ceiling(sizef.Width), (int)Math.Ceiling(sizef.Height));

            Image = new Bitmap(Size.X, Size.Y);
            using (var g = Graphics.FromImage(Image))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.DrawString(Text, font, brush, 0, 0);
            }

            Mesh = new Mesh();
            Mesh.Vertices = new float[]{
                0, -Size.Y, -1f, 0, 1,
                Size.X , -Size.Y, -1f, 1, 1,
                Size.X , 0, -1f, 1, 0,
                0 , 0, -1f, 0, 0,
            };

            Albedo = Texture.FromImage(Image);
        }

        public override void Draw()
        {
            if (Mesh == null)
            {
                throw new Exception("Mesh cannot be null.");
            }

            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, Mesh.Vertices.Length / 5);
        }

        public override void LoadToBuffer(Shader shader)
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

            // Attributes
            int vertexLocation = shader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            int texCoordLocation = shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        }

    }
}