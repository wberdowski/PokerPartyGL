﻿using BitSerializer;
using System.Windows.Forms;
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
using static PokerParty.Common.ControlPacket;
using static PokerParty.Common.PlayingCard;

namespace PokerParty.Client
{
    public class Window : GameWindow
    {
        public static TimeSpan DeltaTime { get; private set; }
        public static Camera Camera { get; set; }

        private Random rand = new Random();
        private Shader stdShader;
        private Shader uiShader;
        private Shader cardShader;
        private float pitch;
        private float yaw = -90;
        Vector2? lastPos = null;
        private float sensitivity = 0.1f;
        public bool WireframeEnabled { get; private set; }
        public string Username { get; private set; }

        private Vector3 skyColor = new Vector3(0.5f, 0.9f, 1f);
        private List<GameObject> gameObjects = new List<GameObject>();
        private CardCollectionObject cardCollection;
        private float speed = 1;

        private Socket controlSocket;
        private byte[] recvBuff = new byte[16 * 1024];

        // NET STATE
        private bool isAutorized;
        private GameState gameState;

        private volatile bool gameStateUpdatePending;
        private FontObject playersListObj;

        public Window(int width, int height, string title) : base(
            new GameWindowSettings()
            {
            },
            new NativeWindowSettings()
            {
                Title = title,
                Size = new Vector2i(width, height),
                NumberOfSamples = 0
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

            stdShader = new Shader("shaders/standard2/vert.glsl", "shaders/standard/frag.glsl");
            uiShader = new Shader("shaders/ui/vert.glsl", "shaders/ui/frag.glsl");
            cardShader = new Shader("shaders/card/vert.glsl", "shaders/card/frag.glsl");

            controlSocket = new Socket(SocketType.Rdm, ProtocolType.IP);
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

            Console.WriteLine("Loading assets...");
            var sw = Stopwatch.StartNew();
            CardDeckLoader.Load();
            sw.Stop();
            Console.WriteLine("Card deck loaded in " + sw.ElapsedMilliseconds + " ms");

            stdShader.Use();

            {
                var obj = new GameObject(new Vector3(0, 0, 0));
                obj.Shader = stdShader;
                obj.Albedo = Texture.FromFile("models/floor/textures/wood.png");
                obj.Mesh = new Mesh();
                obj.Mesh.LoadFromObj("models/floor/floor.obj");
                obj.LoadToBuffer();
                gameObjects.Add(obj);
            }

            {
                var obj = new GameObject(new Vector3(0, 0, 0));
                obj.Shader = stdShader;
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
                obj.Shader = stdShader;
                obj.Mesh = chairMesh;
                obj.Albedo = chairTex;
                obj.LoadToBuffer();
                gameObjects.Add(obj);
            }

            // FONTS
            {
                var fontObj = new FontObject($"PokerParty v{Assembly.GetExecutingAssembly().GetName().Version}", new Font("Segoe UI", 12f), new SolidBrush(Color.FromArgb(100, Color.White)));
                fontObj.Shader = uiShader;
                fontObj.Layer = RenderLayer.UI;
                fontObj.Anchor = UILayoutAnchor.BottomRight;
                fontObj.Position = new Vector3(-(fontObj.Size.X + 10), fontObj.Size.Y + 10, 0);
                fontObj.LoadToBuffer();
                gameObjects.Add(fontObj);
            }

            {
                var fontObj = new FontObject("Fold [F]\nRise [R]\nCheck [C]", new Font("Segoe UI", 14f, FontStyle.Bold), new SolidBrush(Color.White));
                fontObj.Shader = uiShader;
                fontObj.Layer = RenderLayer.UI;
                fontObj.Anchor = UILayoutAnchor.BottomLeft;
                fontObj.Position = new Vector3(10, fontObj.Size.Y + 10, 0);
                fontObj.LoadToBuffer();
                gameObjects.Add(fontObj);
            }

            {
                playersListObj = new FontObject("Players (0):", new Font("Segoe UI", 12f, FontStyle.Bold), new SolidBrush(Color.White));
                playersListObj.Shader = uiShader;
                playersListObj.Layer = RenderLayer.UI;
                playersListObj.Anchor = UILayoutAnchor.TopLeft;
                playersListObj.Position = new Vector3(10, -10, 0);
                playersListObj.LoadToBuffer();
                gameObjects.Add(playersListObj);
            }

            // CARDS

            var cardMesh = new Mesh();
            cardMesh.LoadFromObj("models/card/card.obj");

            {
                cardCollection = new CardCollectionObject(new Vector3(0, 0.74f, 0));
                cardCollection.CardType = new PlayingCard(PlayingCard.CardColor.Spades, PlayingCard.CardValue.Ace);
                cardCollection.Layer = RenderLayer.Card;
                cardCollection.Shader = cardShader;
                cardCollection.Mesh = cardMesh;
                cardCollection.LoadToBuffer();
            }

            Console.WriteLine("Assets loaded");

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
                Console.WriteLine("Cannot connect");
            }

            if (controlSocket.Connected)
            {
                Console.WriteLine("Connected.");

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
            var len = controlSocket.EndReceive(ar);

            var resPacket = BinarySerializer.Deserialize<ControlPacket>(recvBuff);

            if (resPacket.Code == OpCode.LoginResponse)
            {
                if (resPacket.Status == OpStatus.Success)
                {
                    isAutorized = true;
                    Console.WriteLine($"Nickname \"{Username}\" registered.");
                }
                else if (resPacket.Status == OpStatus.Failure)
                {
                    Console.WriteLine($"Nickname register error: {resPacket.GetError()}.");
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

            CardInstanceData[] c = new CardInstanceData[gameState.cardsOnTheTable.Length];

            for (int i = 0; i < c.Length; i++)
            {
                var card = gameState.cardsOnTheTable[i];
                c[i] = new CardInstanceData(Matrix4.CreateTranslation(new Vector3(i * 0.1f, 0, 0)), card.index);
            }

            cardCollection.Instances = c;
            cardCollection.UpdateInstanceDataBuffer();
        }

        //
        //  UPDATE
        //

        private void UpdateUILayout()
        {
            foreach (var obj in gameObjects.Where(x => x.Layer == RenderLayer.UI).Cast<FontObject>())
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

            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W))
            {
                Camera.Position += new Vector3(Camera.Front.X, 0, Camera.Front.Z).Normalized() * speed * (float)e.Time; //Forward 
            }

            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S))
            {
                Camera.Position -= new Vector3(Camera.Front.X, 0, Camera.Front.Z).Normalized() * speed * (float)e.Time; //Backwards
            }

            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.A))
            {
                Camera.Position -= Vector3.Normalize(Vector3.Cross(Camera.Front, Camera.Up)) * speed * (float)e.Time; //Left
            }

            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D))
            {
                Camera.Position += Vector3.Normalize(Vector3.Cross(Camera.Front, Camera.Up)) * speed * (float)e.Time; //Right
            }

            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space))
            {
                Camera.Position += Camera.Up * speed * (float)e.Time; //Up 
            }

            if (KeyboardState.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftControl))
            {
                Camera.Position -= Camera.Up * speed * (float)e.Time; //Down
            }

            if (KeyboardState.IsKeyReleased(OpenTK.Windowing.GraphicsLibraryFramework.Keys.G))
            {
                WireframeEnabled = !WireframeEnabled;

                if (WireframeEnabled)
                {
                    Console.WriteLine("Wireframe view ENABLED");
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    GL.Disable(EnableCap.CullFace);
                }
                else
                {
                    Console.WriteLine("Wireframe view DISABLED");
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    GL.Enable(EnableCap.CullFace);
                }
            }

            if (KeyboardState.IsKeyReleased(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F11))
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

            if (KeyboardState.IsKeyReleased(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
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

            cardCollection.Shader.Use();
            cardCollection.Shader.SetMatrix4("view", Camera.View);
            cardCollection.Shader.SetMatrix4("projection", Camera.Projection);
            cardCollection.Shader.SetMatrix4("model", cardCollection.ModelMatrix);

            GL.BindTexture(TextureTarget.Texture2DArray, CardDeckLoader.Texture.Handle);
            cardCollection.Draw();

            foreach (var obj in gameObjects.Where(x => x.Layer == RenderLayer.Standard))
            {
                obj.Shader.Use();
                obj.Shader.SetMatrix4("view", Camera.View);
                obj.Shader.SetMatrix4("projection", Camera.Projection);
                obj.Shader.SetMatrix4("model", obj.ModelMatrix);

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

            foreach (var obj in gameObjects.Where(x => x.Layer == RenderLayer.UI))
            {
                obj.Shader.Use();
                obj.Shader.SetMatrix4("view", Camera.View);
                obj.Shader.SetMatrix4("projection", Camera.ProjectionUI);
                obj.Shader.SetMatrix4("model", obj.ModelMatrix * Matrix4.CreateTranslation(-Camera.Bounds.Size.X / 2f, Camera.Bounds.Size.Y / 2f, 0));

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