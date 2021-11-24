using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PokerParty.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static PokerParty.Client.NetClient;
using static PokerParty.Common.Chips;

namespace PokerParty.Client
{
    public class Window : GameWindow
    {
        public const float TableHeight = 0.730712f;
        public const float ChipHeight = 0.004f;
        public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public static TimeSpan DeltaTime { get; private set; }
        public static Camera Camera { get; set; }
        public static volatile bool gameStateUpdatePending;
        public static FontObject playersListObj;
        public static InstanceCollection chipCollection;
        public static List<GameObject> gameObjects = new List<GameObject>();
        public static List<UIObject> uiObjects = new List<UIObject>();
        public static InstanceCollection cardCollection;
        public static string Username { get; set; }

        private Random rand = new Random();
        private bool wireframeEnabled;
        private float pitch = 0;
        private float yaw = -90;
        private Vector2? lastPos = null;
        private float sensitivity = 0.1f;
        private Vector3 skyColor = new Vector3(0.5f, 0.9f, 1f);
        private float speed = 1f;
        private Matrix4[] seats = new Matrix4[]
        {
            // Bottom row
            Matrix4.CreateTranslation(0,0,0.4f),
            Matrix4.CreateTranslation(-0.6f,0,0.4f),
            Matrix4.CreateTranslation(0.6f,0,0.4f),

            // Top row
            Matrix4.CreateRotationY((float)Math.PI) * Matrix4.CreateTranslation(0,0,-0.4f),
            Matrix4.CreateRotationY((float)Math.PI) * Matrix4.CreateTranslation(-0.6f,0,-0.4f),
            Matrix4.CreateRotationY((float)Math.PI) * Matrix4.CreateTranslation(0.6f,0,-0.4f),

            // Left-bottom
            Matrix4.CreateRotationY((float)Math.PI * 2 * (-55f/360f)) * Matrix4.CreateTranslation(-1.05629f,0,0.230594f),
            // Left-top
            Matrix4.CreateRotationY((float)Math.PI * 2 * (55f/360f) + (float)Math.PI) * Matrix4.CreateTranslation(-1.05629f,0,-0.230594f),

            // Right-bottom
            Matrix4.CreateRotationY((float)Math.PI * 2 * (55f/360f)) * Matrix4.CreateTranslation(1.05629f,0,0.230594f),
            // Right-top
            Matrix4.CreateRotationY((float)Math.PI * 2 * (-55f/360f) + (float)Math.PI) * Matrix4.CreateTranslation(1.05629f,0,-0.230594f),
        };

        public Window(int width, int height, string title) : base(
            new GameWindowSettings()
            {
            },
            new NativeWindowSettings()
            {
                Title = title,
                Size = new Vector2i(width, height),
                NumberOfSamples = 2
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

            Camera = new Camera(
                new Vector3(0, 1, 1),
                Vector3.Zero,
                new Vector3(0, 1, 0),
                0.01f,
                1000f,
                80f
            );

            Username = "User" + rand.Next(1111, 9999);
            Title += " - " + Username;

            AssetLoader.Load();

            NetClient.Init();
            NetClient.Connect();

            base.OnLoad();
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
                cardInstances[i] = new InstanceData(Matrix4.CreateTranslation(new Vector3(i * 0.1f - 0.2f, 0, 0)), card.index);
            }

            cardCollection.Instances = cardInstances;
            cardCollection.UpdateInstanceDataBuffer();

            // Update chips

            var chipInstances = new List<InstanceData>();
            for (int p = 0; p < gameState.players.Length; p++)
            {
                var chips = gameState.players[p].Chips;

                for (int j = 0; j < chips.Amounts.Length; j++)
                {
                    for (int i = 0; i < chips[(ChipColor)j]; i++)
                    {
                        chipInstances.Add(new InstanceData(
                            Matrix4.CreateFromAxisAngle(new Vector3(0, 1, 0), rand.NextSingle() * 2 * (float)Math.PI) *
                            Matrix4.CreateTranslation(
                                (rand.NextSingle() - 0.5f) / 300f + j * 0.05f - 0.1f,
                                (ChipHeight / 2f) + i * ChipHeight,
                                (rand.NextSingle() - 0.5f) / 300f
                            ) * seats[p],
                            j
                        ));
                    }
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
                wireframeEnabled = !wireframeEnabled;

                if (wireframeEnabled)
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
            cardCollection.Draw();

            // CHIPS
            chipCollection.Draw();

            foreach (var obj in gameObjects)
            {
                obj.Draw();
            }

            // UI
            foreach (var obj in uiObjects)
            {
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