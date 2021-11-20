using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;

namespace PokerParty.Client
{
    public class Texture3D
    {
        public int Handle { get; }
        public int Capacity { get; private set; }

        public Texture3D(int width, int height, int capacity)
        {
            Capacity = capacity;

            Handle = GL.GenTexture();
            Use();

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            //float maxTextureMaxAnisotropy = GL.GetFloat((GetPName)0x84FF);
            //GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)0x84FE, 0);

            float maxTextureMaxAnisotropy = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
            GL.TexParameter(TextureTarget.Texture2DArray, (TextureParameterName)All.TextureMaxAnisotropy, maxTextureMaxAnisotropy);
            //GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropyExt, maxTextureMaxAnisotropy);

            GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.Rgba, width, height, capacity, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

        }

        public void LoadAndAdd(int index, string filepath)
        {
            if (index < Capacity)
            {
                //Load the image
                Image<Rgba32> image = Image.Load<Rgba32>(filepath);

                //ImageSharp loads from the top-left pixel, whereas OpenGL loads from the bottom-left, causing the texture to be flipped vertically.
                //This will correct that, making the texture display properly.
                image.Mutate(x => x.Flip(FlipMode.Vertical));

                //Convert ImageSharp's format into a byte array, so we can use it with OpenGL.
                var pixels = new List<byte>(4 * image.Width * image.Height);

                for (int y = 0; y < image.Height; y++)
                {
                    var row = image.GetPixelRowSpan(y);

                    for (int x = 0; x < image.Width; x++)
                    {
                        pixels.Add(row[x].R);
                        pixels.Add(row[x].G);
                        pixels.Add(row[x].B);
                        pixels.Add(row[x].A);
                    }
                }

                Use();
                GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, index, image.Width, image.Height, 1, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.ToArray());
            } else
            {
                throw new Exception("Tried to exceed object capacity.");
            }
        }

        public void GenerateMipmaps()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
        }

        public void Use()
        {
            GL.BindTexture(TextureTarget.Texture2DArray, Handle);
        }
    }
}
