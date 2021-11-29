using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
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

            // TEXT
            {
                var fontObj = new TextObject($"PokerParty v{Assembly.GetExecutingAssembly().GetName().Version}", new Font("Segoe UI", 12f), new SolidBrush(Color.FromArgb(100, Color.White)));
                fontObj.Material = Materials["ui"];
                fontObj.Layer = RenderLayer.UI;
                fontObj.Anchor = UILayoutAnchor.TopRight;
                fontObj.Position = new Vector3(-(fontObj.Size.X + 10), -10, 0);
                fontObj.LoadToBuffer();
                uiObjects.Add(fontObj);
            }

            {
                var fontObj = new TextObject("Fold [F]\nRaise [R]\nCheck [C]", new Font("Segoe UI", 14f, FontStyle.Bold), new SolidBrush(Color.White));
                fontObj.Material = Materials["ui"];
                fontObj.Layer = RenderLayer.UI;
                fontObj.Anchor = UILayoutAnchor.BottomLeft;
                fontObj.Position = new Vector3(20, fontObj.Size.Y + 20, 0);
                fontObj.LoadToBuffer();
                uiObjects.Add(fontObj);
            }

            {
                playersListObj = new TextObject("Players (0):", new Font("Segoe UI", 12f, FontStyle.Bold), new SolidBrush(Color.White));
                playersListObj.Material = Materials["ui"];
                playersListObj.Layer = RenderLayer.UI;
                playersListObj.Anchor = UILayoutAnchor.TopLeft;
                playersListObj.Position = new Vector3(20, -20, 0);
                playersListObj.LoadToBuffer();
                uiObjects.Add(playersListObj);
            }

            // PLAYER NAMES
            foreach (var s in seats)
            {
                var fontObj = new GameObject(new Vector3(0, TableHeight + 0.3f, 0.2f), Quaternion.FromEulerAngles((float)Math.PI, 0, (float)Math.PI));
                fontObj.InstanceMatrix = s;
                fontObj.Scale = new Vector3(0.0003f);
                fontObj.Material = Materials["standard"];
                fontObj.Albedo = TextObject.GenerateTexture("N/A", new Font("Segoe UI", 100f, FontStyle.Bold), new SolidBrush(Color.Black), out var size);
                fontObj.Mesh = new PanelMesh3D(new Vector2(size.X, size.Y));
                fontObj.Position = new Vector3(fontObj.Position.X + size.X * fontObj.Scale.X / 2f, fontObj.Position.Y, fontObj.Position.Z);
                fontObj.LoadToBuffer();
                seatLabels.Add(fontObj);
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

            // Center panel
            {
                titleObj = new TextObject("Waiting for players", new Font("Segoe UI", 18f, FontStyle.Bold), new SolidBrush(Color.White));
                titleObj.Material = Materials["ui"];
                titleObj.Layer = RenderLayer.UI;
                titleObj.Anchor = UILayoutAnchor.TopCenter;
                titleObj.Position = new Vector3(0, -12, 0);
                titleObj.LoadToBuffer();
                uiObjects.Add(titleObj);
            }

            {
                var panelObj = new UIObject();
                panelObj.Mesh = new PanelMesh(new Vector2(400, 38));
                panelObj.Size = new Vector2i(400, 48);
                panelObj.Border = 6;
                panelObj.Albedo = Texture.FromFile("models/panel/textures/panel.png", TextureMinFilter.Nearest);
                panelObj.Material = Materials["ui"];
                panelObj.Layer = RenderLayer.UI;
                panelObj.Anchor = UILayoutAnchor.TopCenter;
                panelObj.Position = new Vector3(0, -10, -1);
                panelObj.LoadToBuffer();
                uiObjects.Add(panelObj);
            }

            // Message
            {
                messageObj = new TextObject("Ready", new Font("Segoe UI", 12f), new SolidBrush(Color.White));
                messageObj.Material = Materials["ui"];
                messageObj.Layer = RenderLayer.UI;
                messageObj.Anchor = UILayoutAnchor.BottomRight;
                messageObj.Position = new Vector3(-406, 38, 0);
                messageObj.LoadToBuffer();
                uiObjects.Add(messageObj);
            }

            {
                var panelObj = new UIObject();
                panelObj.Mesh = new PanelMesh(new Vector2(400, 32));
                panelObj.Border = 6;
                panelObj.Albedo = Texture.FromFile("models/panel/textures/panel.png", TextureMinFilter.Nearest);
                panelObj.Material = Materials["ui"];
                panelObj.Layer = RenderLayer.UI;
                panelObj.Anchor = UILayoutAnchor.BottomRight;
                panelObj.Position = new Vector3(-410, 42, -1);
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

            var chipTexture = new Texture3D(256, 256, 5);
            chipTexture.LoadAndAdd(0, "models/chip/textures/black.png");
            chipTexture.LoadAndAdd(1, "models/chip/textures/red.png");
            chipTexture.LoadAndAdd(2, "models/chip/textures/green.png");
            chipTexture.LoadAndAdd(3, "models/chip/textures/blue.png");
            chipTexture.LoadAndAdd(4, "models/chip/textures/white.png");
            chipTexture.GenerateMipmaps();

            {
                chipCollection = new InstanceCollection(new Vector3(0, TableHeight + 0.004f, 0));
                chipCollection.Layer = RenderLayer.Instanced;
                chipCollection.Material = Materials["card"];
                chipCollection.Albedo3D = chipTexture;
                chipCollection.Mesh = new Mesh();
                chipCollection.Mesh.LoadFromObj("models/chip/chip.obj");
                chipCollection.LoadToBuffer();
            }

            // BUTTONS
            var buttonTexture = new Texture3D(512, 512, 3);
            buttonTexture.LoadAndAdd(0, "models/button/textures/dealer.png");
            buttonTexture.LoadAndAdd(1, "models/button/textures/smallblind.png");
            buttonTexture.LoadAndAdd(2, "models/button/textures/bigblind.png");
            buttonTexture.GenerateMipmaps();

            {
                buttonCollection = new InstanceCollection(new Vector3(0, TableHeight + 0.002f, 0));
                buttonCollection.Layer = RenderLayer.Instanced;
                buttonCollection.Material = Materials["card"];
                buttonCollection.Albedo3D = buttonTexture;
                buttonCollection.Mesh = new Mesh();
                buttonCollection.Mesh.LoadFromObj("models/button/button.obj");
                buttonCollection.LoadToBuffer();
            }

            // CARDS
            {
                cardCollection = new InstanceCollection(new Vector3(0, TableHeight, 0));
                cardCollection.Layer = RenderLayer.Instanced;
                cardCollection.Albedo3D = CardDeckLoader.Texture;
                cardCollection.Material = Materials["card"];
                cardCollection.Mesh = new Mesh();
                cardCollection.Mesh.LoadFromObj("models/card/card.obj");
                cardCollection.LoadToBuffer();
            }

            // Player cards
            {
                playerCardCollection = new InstanceCollection(new Vector3(0,-0.009f,-Window.Camera.DepthNear - 0.0001f));
                playerCardCollection.Scale = new Vector3(0.05f);
                playerCardCollection.Layer = RenderLayer.Instanced;
                playerCardCollection.Albedo3D = CardDeckLoader.Texture;
                playerCardCollection.Material = Materials["card"];
                playerCardCollection.Mesh = new Mesh();
                playerCardCollection.Mesh.LoadFromObj("models/card/card.obj");
                playerCardCollection.LoadToBuffer();
            }

            Debug.WriteLine("Assets loaded");
        }
    }
}
