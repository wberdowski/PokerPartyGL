using OpenTK.Mathematics;
using System;
using System.Drawing;
using System.Drawing.Text;

namespace PokerParty.Client
{
    public class FontObject : UIObject
    {
        public string Text { get; private set; }
        public Vector2i Size { get; private set; }
        public Bitmap Image { get; private set; }
        public Font Font { get; }
        public Brush Brush { get; }

        public FontObject(string text, Font font, Brush brush) : base()
        {
            Generate(text, font, brush);
            Font = font;
            Brush = brush;
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

        public void Generate(string text)
        {
            Generate(text, Font, Brush);
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

            Mesh = new PanelMesh(Size);

            Albedo = Texture.FromImage(Image);
        }

        public static Texture GenerateTexture(string text, Font font, Brush brush, out Vector2i size)
        {
            SizeF sizef = Graphics.FromHwnd(IntPtr.Zero).MeasureString(text, font);
            size = new Vector2i((int)Math.Ceiling(sizef.Width), (int)Math.Ceiling(sizef.Height));

            var Image = new Bitmap(size.X, size.Y);
            using (var g = Graphics.FromImage(Image))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.DrawString(text, font, brush, 0, 0);
            }

            return Texture.FromImage(Image);
        }
    }
}