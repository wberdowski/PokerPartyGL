using ObjLoader.Loader.Loaders;
using System;
using System.Collections.Generic;
using System.IO;

namespace PokerParty.Client
{
    public class Mesh : IDisposable
    {
        public float[] Vertices { get; set; }
        public bool IsDisposed { get; private set; }

        public void LoadFromObj(string filepath)
        {
            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();
            using (var fileStream = new FileStream(filepath, FileMode.Open))
            {
                var result = objLoader.Load(fileStream);
                var vertices = new List<float>();

                foreach (var face in result.Groups[0].Faces)
                {
                    if (face.Count == 3)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var v = face[i];

                            vertices.AddRange(new float[]
                            {
                                result.Vertices[v.VertexIndex - 1].X,
                                result.Vertices[v.VertexIndex - 1].Y,
                                result.Vertices[v.VertexIndex - 1].Z,
                                result.Textures[v.TextureIndex - 1].X,
                                result.Textures[v.TextureIndex - 1].Y,
                                result.Normals[v.NormalIndex - 1].X,
                                result.Normals[v.NormalIndex - 1].Y,
                                result.Normals[v.NormalIndex - 1].Z,
                            });
                        }
                    }
                    else
                    {
                        throw new Exception("Unsupported face vertex count.");
                    }
                }

                Vertices = vertices.ToArray();
            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Vertices = null;
            }
        }
    }
}
