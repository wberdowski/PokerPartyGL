using BitSerializer;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PokerParty.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using static PokerParty.Common.Chips;
using static PokerParty.Common.ControlPacket;

namespace PokerParty.Client
{
    public class Window : GameWindow
    {
        private const float TableHeight = 0.730712f;
        private const float ChipHeight = 0.004f;

        public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static TimeSpan DeltaTime { get; private set; }
        public static Camera Camera { get; set; }

        private Random rand = new Random();
        private float pitch;
        private float yaw = -90;
        Vector2? lastPos = null;
        private float sensitivity = 0.1f;
        public bool WireframeEnabled { get; private set; }
        public string Username { get; private set; }

        private Vector3 skyColor = new Vector3(0.5f, 0.9f, 1f);
        private List<GameObject> gameObjects = new List<GameObject>();
        private List<UIObject> uiObjects = new List<UIObject>();
        private InstanceCollection cardCollection;
        private float speed = 1f;

        private Socket controlSocket;
        private byte[] recvBuff = new byte[16 * 1024];

        // NET STATE
        private bool isAutorized;
        private GameState gameState;

        private volatile bool gameStateUpdatePending;
        private FontObject playersListObj;
        private InstanceCollection chipCollection;
        private Texture3D chipTexture;

        public Window(int width, int height, string title) : base(
            new GameWindowSettings()
            {
            },
            new NativeWindowSettings()
            {
                Title = title,
                Size = new Vector2i(width, height),
                NumberOfSamples = 4
            })
        {
            MonitorInfo info;
            if (Monitors.TryGetMonitorInfo(CurrentMonitor, out info) || Monitors.TryGetMonitorInfo(0, out info))
            {
                Location = new Vector2i((int)(info.HorizontalResolution / 2f - Size.X / 2f), (int)(info.VerticalResolution / 2f - Size.Y / 2f));
            }
            CursorVisible = false;
            CursorGrabbed = true;
            VSync = VSyncMode.On;

            GL.ClearColor(skyColor.X, skyColor.Y, skyColor.Z, 1f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Run();
        }

        //
        //  LOAD
        //

        protected override void OnLoad()
        {
            Directory.CreateDirectory("world/data");


            Materials.Add("standard", new Material(new Shader("shaders/standard/vert.glsl", "shaders/standard/frag.glsl")));
            Materials.Add("ui", new Material(new Shader("shaders/ui/vert.glsl", "shaders/ui/frag.glsl")));
            Materials.Add("card", new Material(new Shader("shaders/card/vert.glsl", "shaders/card/frag.glsl")));

            controlSocket = new Socket(SocketType.Stream, ProtocolType.IP);
            controlSocket.ReceiveTimeout = 10000;
            controlSocket.SendTimeout = 10000;
            controlSocket.BeginConnect(new IPEndPoint(IPAddress.Loopback, 55555), OnConnect, null);

            Camera = new Camera(
                new Vector3(0, 1, 1),
                Vector3.Zero,
                new Vector3(0, 1, 0),
                0.01f,
                1000f,
                80f
            );

            Debug.WriteLine("Loading assets...");
            var sw = Stopwatch.StartNew();
            CardDeckLoader.Load();
            sw.Stop();
            Debug.WriteLine("Card deck loaded in " + sw.ElapsedMilliseconds + " ms");

            Materials["standard"].Use();

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

            Materials["card"].Use();

            var chipMesh = new Mesh();
            chipMesh.LoadFromObj("models/chip/chip.obj");

            chipTexture = new Texture3D(256, 256, 5);
            chipTexture.LoadAndAdd(0, "models/chip/textures/black.png");
            chipTexture.LoadAndAdd(1, "models/chip/textures/red.png");
            chipTexture.LoadAndAdd(2, "models/chip/textures/green.png");
            chipTexture.LoadAndAdd(3, "models/chip/textures/blue.png");
            chipTexture.LoadAndAdd(4, "models/chip/textures/white.png");
            chipTexture.GenerateMipmaps();

            {
                chipCollection = new InstanceCollection(new Vector3(0, TableHeight + 0.002f, 0.4f));
                chipCollection.Layer = RenderLayer.Instanced;
                chipCollection.Material = Materials["card"];
                chipCollection.Mesh = chipMesh;
                chipCollection.LoadToBuffer();
            }

            // CARDS

            var cardMesh = new Mesh();
            cardMesh.LoadFromObj("models/card/card.obj");

            {
                cardCollection = new InstanceCollection(new Vector3(0, TableHeight, 0));
                cardCollection.Layer = RenderLayer.Instanced;
                cardCollection.Material = Materials["card"];
                cardCollection.Mesh = cardMesh;
                cardCollection.LoadToBuffer();
            }

            Debug.WriteLine("Assets loaded");

            base.OnLoad();
        }

        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                controlSocket.EndConnect(ar);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine("Cannot connect");
            }

            if (controlSocket.Connected)
            {
                Debug.WriteLine("Connected.");

                SendLoginRequest();
            }
        }

        private async void SendLoginRequest()
        {
            Username = "User" + rand.Next(1111, 9999);
            var packet = new ControlPacket(OpCode.LoginRequest, Encoding.UTF8.GetBytes(Username));
            await controlSocket.SendAsync(BinarySerializer.Serialize(packet), SocketFlags.None);

            controlSocket.BeginReceive(recvBuff, 0, recvBuff.Length, SocketFlags.None, OnControlReceive, null);
        }

        private void OnControlReceive(IAsyncResult ar)
        {
            int len = 0;

            try
            {
                controlSocket.EndReceive(ar);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine("Disconnected from server.");
                return;
            }

            var resPacket = BinarySerializer.Deserialize<ControlPacket>(recvBuff);

            if (resPacket.Code == OpCode.LoginResponse)
            {
                if (resPacket.Status == OpStatus.Success)
                {
                    isAutorized = true;
                    Debug.WriteLine($"Nickname \"{Username}\" registered.");
                }
                else if (resPacket.Status == OpStatus.Failure)
                {
                    Debug.WriteLine($"Nickname register error: {resPacket.GetError()}.");
                }
            }
            else if (resPacket.Code == OpCode.GameStateUpdate)
            {
                gameState = BinarySerializer.Deserialize<GameState>(resPacket.Payload);
                gameStateUpdatePending = true;
            }

            controlSocket.BeginReceive(recvBuff, 0, recvBuff.Length, SocketFlags.None, OnControlReceive, null);
        }

        private void UpdateGameState()
        {
            // Update player list
            playersListObj.Generate($"Players ({gameState.players.Length}):\n{string.Join('\n', gameState.players.Select(x => $"{x.Nickname} [{x.Online}]"))}");
            playersListObj.DeleteBuffer();
            playersListObj.LoadToBuffer();

            // Update cards on the table

            InstanceData[] cardInstances = new InstanceData[gameState.cardsOnTheTable.Length];

            for (int i = 0; i < cardInstances.Length; i++)
            {
                var card = gameState.cardsOnTheTable[i];
                cardInstances[i] = new InstanceData(Matrix4.CreateTranslation(new Vector3(i * 0.1f, 0, 0)), PlayingCard.Back.index);
            }

            cardCollection.Instances = cardInstances;
            cardCollection.UpdateInstanceDataBuffer();

            // Update chips

            var chips = gameState.players[0].Chips;
            var chipInstances = new List<InstanceData>();

            for (int j = 0; j < chips.Amounts.Length; j++)
            {
                for (int i = 0; i < chips[(ChipColor)j]; i++)
                {
                    chipInstances.Add(new InstanceData(
                        Matrix4.CreateFromAxisAngle(new Vector3(0, 1, 0), rand.NextSingle() * 2 * (float)Math.PI) *
                        Matrix4.CreateTranslation(
                            (rand.NextSingle() - 0.5f) / 300f + j * 0.05f,
                            (ChipHeight / 2f) + i * ChipHeight,
                            (rand.NextSingle() - 0.5f) / 300f
                        ),
                        j
                    ));
                }
            }

            chipCollection.Instances = chipInstances.ToArray();
            chipCollection.UpdateInstanceDataBuffer();
        }

        //
        //  UPDATE
        //

        private void UpdateUILayout()
        {
            foreach (var obj in uiObjects)
            {
                obj.UpdateModelMatrix();
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (gameStateUpdatePending)
            {
                gameStateUpdatePending = false;
                UpdateGameState();
            }

            if (pitch > 89.0f)
            {
                pitch = 89.0f;
            }
            else if (pitch < -89.0f)
            {
                pitch = -89.0f;
            }

            Camera.Front.X = (float)Math.Cos(MathHelper.DegreesToRadians(pitch)) * (float)Math.Cos(MathHelper.DegreesToRadians(yaw));
            Camera.Front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(pitch));
            Camera.Front.Z = (float)Math.Cos(MathHelper.DegreesToRadians(pitch)) * (float)Math.Sin(MathHelper.DegreesToRadians(yaw));
            Camera.Front = Vector3.Normalize(Camera.Front);

            Camera.UpdateMatrix();

            // Movement
            if (KeyboardState.IsKeyDown(Keys.W))
            {
                Camera.Position += new Vector3(Camera.Front.X, 0, Camera.Front.Z).Normalized() * speed * (float)e.Time; //Forward 
            }

            if (KeyboardState.IsKeyDown(Keys.S))
            {
                Camera.Position -= new Vector3(Camera.Front.X, 0, Camera.Front.Z).Normalized() * speed * (float)e.Time; //Backwards
            }

            if (KeyboardState.IsKeyDown(Keys.A))
            {
                Camera.Position -= Vector3.Normalize(Vector3.Cross(Camera.Front, Camera.Up)) * speed * (float)e.Time; //Left
            }

            if (KeyboardState.IsKeyDown(Keys.D))
            {
                Camera.Position += Vector3.Normalize(Vector3.Cross(Camera.Front, Camera.Up)) * speed * (float)e.Time; //Right
            }

            if (KeyboardState.IsKeyDown(Keys.Space))
            {
                Camera.Position += Camera.Up * speed * (float)e.Time; //Up 
            }

            if (KeyboardState.IsKeyDown(Keys.LeftControl))
            {
                Camera.Position -= Camera.Up * speed * (float)e.Time; //Down
            }

            if (KeyboardState.IsKeyReleased(Keys.G))
            {
                WireframeEnabled = !WireframeEnabled;

                if (WireframeEnabled)
                {
                    Debug.WriteLine("Wireframe view ENABLED");
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    GL.Disable(EnableCap.CullFace);
                }
                else
                {
                    Debug.WriteLine("Wireframe view DISABLED");
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    GL.Enable(EnableCap.CullFace);
                }
            }

            if (KeyboardState.IsKeyReleased(Keys.F11))
            {
                if (WindowState != WindowState.Maximized)
                {
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    WindowState = WindowState.Normal;
                }
            }

            if (KeyboardState.IsKeyReleased(Keys.Escape))
            {
                Close();
            }

            GL.Flush();

            base.OnUpdateFrame(e);
        }

        //
        //  RENDER
        //

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // CARDS
            cardCollection.Material.Shader.Use();
            cardCollection.Material.Shader.SetMatrix4("view", Camera.View);
            cardCollection.Material.Shader.SetMatrix4("projection", Camera.Projection);
            cardCollection.Material.Shader.SetMatrix4("model", cardCollection.ModelMatrix);

            CardDeckLoader.Texture.Use();
            cardCollection.Draw();

            // CHIPS
            chipCollection.Material.Shader.Use();
            chipCollection.Material.Shader.SetMatrix4("view", Camera.View);
            chipCollection.Material.Shader.SetMatrix4("projection", Camera.Projection);
            chipCollection.Material.Shader.SetMatrix4("model", chipCollection.ModelMatrix);

            chipTexture.Use();
            chipCollection.Draw();

            foreach (var obj in gameObjects.Where(x => x.Layer == RenderLayer.Standard))
            {
                obj.Material.Shader.Use();
                obj.Material.Shader.SetMatrix4("view", Camera.View);
                obj.Material.Shader.SetMatrix4("projection", Camera.Projection);
                obj.Material.Shader.SetMatrix4("model", obj.ModelMatrix);

                if (obj.Albedo != null)
                {
                    obj.Albedo.Use();
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }
                obj.Draw();
            }

            // UI
            foreach (var obj in uiObjects)
            {
                obj.Material.Shader.Use();
                obj.Material.Shader.SetMatrix4("view", Camera.View);
                obj.Material.Shader.SetMatrix4("projection", Camera.ProjectionUI);
                obj.Material.Shader.SetMatrix4("model", obj.ModelMatrix * Matrix4.CreateTranslation(-Camera.Bounds.Size.X / 2f, Camera.Bounds.Size.Y / 2f, 0));
                obj.Material.Shader.SetVec3("size", new Vector3(((PanelMesh)obj.Mesh).Size.X, ((PanelMesh)obj.Mesh).Size.Y, 1));
                obj.Material.Shader.SetVec3("texSize", new Vector3(32, 32, 0));
                obj.Material.Shader.SetInt("border", obj.Border);

                if (obj.Albedo != null)
                {
                    obj.Albedo.Use();
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }

                obj.Draw();
            }


            GL.Flush();

            Context.SwapBuffers();

            base.OnRenderFrame(args);

        }

        protected override void OnResize(ResizeEventArgs e)
        {
            Camera.Bounds = new Box2i(0, 0, e.Width, e.Height);
            GL.Viewport(0, 0, e.Width, e.Height);

            UpdateUILayout();

            base.OnResize(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (lastPos == null)
            {
                lastPos = new Vector2(e.X, e.Y);
            }

            float deltaX = e.X - (float)lastPos?.X;
            float deltaY = e.Y - (float)lastPos?.Y;
            lastPos = new Vector2(e.X, e.Y);

            yaw = (yaw + deltaX * sensitivity) % 360;
            pitch -= deltaY * sensitivity;

            base.OnMouseMove(e);
        }
    }
}