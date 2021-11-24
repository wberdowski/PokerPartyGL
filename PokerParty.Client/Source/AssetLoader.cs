﻿using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using static PokerParty.Client.Window;

namespace PokerParty.Client
{
    public static class AssetLoader
    {
        public static void Load()
        {
            Debug.WriteLine("Loading assets...");
            var sw = Stopwatch.StartNew();
            CardDeckLoader.Load();
            sw.Stop();
            Debug.WriteLine("Card deck loaded in " + sw.ElapsedMilliseconds + " ms");

            {
                var obj = new GameObject(new Vector3(0, 0, 0));
                obj.Material = Materials["standard"];
                obj.Albedo = Texture.FromFile("models/floor/textures/wood.png");
                obj.Mesh = new Mesh();
                obj.Mesh.LoadFromObj("models/floor/floor.obj");
                obj.LoadToBuffer();
                gameObjects.Add(obj);
            }

            {
                var obj = new GameObject(new Vector3(0, 0, 0));
                obj.Material = Materials["standard"];
                obj.Albedo = Texture.FromFile("models/table/textures/table.png");
                obj.Mesh = new Mesh();
                obj.Mesh.LoadFromObj("models/table/table.obj");
                obj.LoadToBuffer();
                gameObjects.Add(obj);
            }

            var chairMesh = new Mesh();
            chairMesh.LoadFromObj("models/chair/chair.obj");
            var chairTex = Texture.FromFile("models/chair/textures/wood.jpg");

            {
                var obj = new GameObject(new Vector3(0, 0, 0.8f));
                obj.Material = Materials["standard"];
                obj.Mesh = chairMesh;
                obj.Albedo = chairTex;
                obj.LoadToBuffer();
                gameObjects.Add(obj);
            }

            // FONTS
            {
                var fontObj = new FontObject($"PokerParty v{Assembly.GetExecutingAssembly().GetName().Version}", new Font("Segoe UI", 12f), new SolidBrush(Color.FromArgb(100, Color.White)));
                fontObj.Material = Materials["ui"];
                fontObj.Layer = RenderLayer.UI;
                fontObj.Anchor = UILayoutAnchor.BottomRight;
                fontObj.Position = new Vector3(-(fontObj.Size.X + 10), fontObj.Size.Y + 10, 0);
                fontObj.LoadToBuffer();
                uiObjects.Add(fontObj);
            }

            {
                var fontObj = new FontObject("Fold [F]\nRise [R]\nCheck [C]", new Font("Segoe UI", 14f, FontStyle.Bold), new SolidBrush(Color.White));
                fontObj.Material = Materials["ui"];
                fontObj.Layer = RenderLayer.UI;
                fontObj.Anchor = UILayoutAnchor.BottomLeft;
                fontObj.Position = new Vector3(20, fontObj.Size.Y + 20, 0);
                fontObj.LoadToBuffer();
                uiObjects.Add(fontObj);
            }

            {
                playersListObj = new FontObject("Players (0):", new Font("Segoe UI", 12f, FontStyle.Bold), new SolidBrush(Color.White));
                playersListObj.Material = Materials["ui"];
                playersListObj.Layer = RenderLayer.UI;
                playersListObj.Anchor = UILayoutAnchor.TopLeft;
                playersListObj.Position = new Vector3(10, -10, 0);
                playersListObj.LoadToBuffer();
                uiObjects.Add(playersListObj);
            }

            // PANEL

            {
                var panelObj = new UIObject();
                panelObj.Mesh = new PanelMesh(new Vector2(200, 240));
                panelObj.Border = 6;
                panelObj.Albedo = Texture.FromFile("models/panel/textures/panel.png", TextureMinFilter.Nearest);
                panelObj.Material = Materials["ui"];
                panelObj.Layer = RenderLayer.UI;
                panelObj.Anchor = UILayoutAnchor.TopLeft;
                panelObj.Position = new Vector3(10, -10, -1);
                panelObj.LoadToBuffer();
                uiObjects.Add(panelObj);
            }

            {
                var panelObj = new UIObject();
                panelObj.Mesh = new PanelMesh(new Vector2(200, 100));
                panelObj.Border = 6;
                panelObj.Albedo = Texture.FromFile("models/panel/textures/panel.png", TextureMinFilter.Nearest);
                panelObj.Material = Materials["ui"];
                panelObj.Layer = RenderLayer.UI;
                panelObj.Anchor = UILayoutAnchor.BottomLeft;
                panelObj.Position = new Vector3(10, 110, -1);
                panelObj.LoadToBuffer();
                uiObjects.Add(panelObj);
            }

            // Message
            {
                var fontObj = new FontObject($"Message", new Font("Segoe UI", 12f), new SolidBrush(Color.White));
                fontObj.Material = Materials["ui"];
                fontObj.Layer = RenderLayer.UI;
                fontObj.Anchor = UILayoutAnchor.TopRight;
                fontObj.Position = new Vector3(-306, -14, 0);
                fontObj.LoadToBuffer();
                uiObjects.Add(fontObj);
            }

            {
                var panelObj = new UIObject();
                panelObj.Mesh = new PanelMesh(new Vector2(300, 32));
                panelObj.Border = 6;
                panelObj.Albedo = Texture.FromFile("models/panel/textures/panel.png", TextureMinFilter.Nearest);
                panelObj.Material = Materials["ui"];
                panelObj.Layer = RenderLayer.UI;
                panelObj.Anchor = UILayoutAnchor.TopRight;
                panelObj.Position = new Vector3(-310, -10, -1);
                panelObj.LoadToBuffer();
                uiObjects.Add(panelObj);
            }

            // Sort transparency
            uiObjects.Sort((a, b) =>
            {
                if (a.Position.Z == b.Position.Z)
                {
                    return 0;
                }

                if (a.Position.Z > b.Position.Z)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            });

            // CHIPS

            var chipMesh = new Mesh();
            chipMesh.LoadFromObj("models/chip/chip.obj");

            var chipTexture = new Texture3D(256, 256, 5);
            chipTexture.LoadAndAdd(0, "models/chip/textures/black.png");
            chipTexture.LoadAndAdd(1, "models/chip/textures/red.png");
            chipTexture.LoadAndAdd(2, "models/chip/textures/green.png");
            chipTexture.LoadAndAdd(3, "models/chip/textures/blue.png");
            chipTexture.LoadAndAdd(4, "models/chip/textures/white.png");
            chipTexture.GenerateMipmaps();

            {
                chipCollection = new InstanceCollection(new Vector3(0, TableHeight + 0.002f, 0));
                chipCollection.Layer = RenderLayer.Instanced;
                chipCollection.Material = Materials["card"];
                chipCollection.Albedo3D = chipTexture;
                chipCollection.Mesh = chipMesh;
                chipCollection.LoadToBuffer();
            }

            // CARDS

            var cardMesh = new Mesh();
            cardMesh.LoadFromObj("models/card/card.obj");

            {
                cardCollection = new InstanceCollection(new Vector3(0, TableHeight, 0));
                cardCollection.Layer = RenderLayer.Instanced;
                cardCollection.Albedo3D = CardDeckLoader.Texture;
                cardCollection.Material = Materials["card"];
                cardCollection.Mesh = cardMesh;
                cardCollection.LoadToBuffer();
            }

            Debug.WriteLine("Assets loaded");
        }
    }
}
