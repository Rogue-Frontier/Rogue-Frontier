using ArchConsole;
using Common;
using NetCoreServer;
using SadConsole;
using SadConsole.Input;
using static SadConsole.Input.Keys;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TranscendenceRL;
using Console = SadConsole.Console;
using Debug = System.Diagnostics.Debug;

namespace TranscendenceRL {
    class GameSession : TcpSession {
        ServerScreen game;
        public GameSession(TcpServer server, ServerScreen game) : base(server) {
            this.game = game;
        }
        protected override void OnConnected() {
            while(game.busy) {
                Thread.Yield();
            }
            game.requests++;
            var s = Common.Space.Zip(SaveGame.Serialize(game.World));
            game.requests--;
            Send(s);
        }
        protected override void OnDisconnected() {}
        protected override void OnReceived(byte[] buffer, long offset, long size) {}
        protected override void OnError(SocketError error) =>
            Debug.WriteLine($"Chat TCP session caught an error with code {error}");
    }
    class GameServer : TcpServer {
        public ServerScreen game;
        public GameServer(IPAddress address, int port, ServerScreen game) : base(address, port) {
            this.game = game;
        }
        protected override TcpSession CreateSession() => new GameSession(this, game);
        protected override void OnError(SocketError error) =>
            Debug.WriteLine($"Chat TCP server caught an error with code {error}");
    }


    public class ServerScreen : Console {
        TitleScreen prev;
        GameServer server;
        public World World;
        public SpaceObject pov;
        public int povTimer;
        public List<Message> povDesc;
        XY screenCenter;
        public XY camera;
        public Dictionary<(int, int), ColoredGlyph> tiles = new Dictionary<(int, int), ColoredGlyph>();
        MouseWatch mouse = new MouseWatch();

        public int requests;
        public bool busy;
        public ServerScreen(int width, int height, TitleScreen prev) : base(width, height) {
            this.prev = prev;
            this.World = prev.World;


            UseKeyboard = true;
            screenCenter = new XY(Width / 2, Height / 2);
            camera = new XY(0, 0);
            server = new GameServer(IPAddress.Any, 1111, this);
            server.Start();
        }
        public override void Update(TimeSpan timeSpan) {

            if (requests > 0) {
                return;
            }
            busy = true;
            World.UpdateAdded();
            World.UpdateActive();
            World.UpdateRemoved();
            busy = false;
            tiles.Clear();
            World.PlaceTiles(tiles);

            if (pov == null) {
                return;
            }
            //Smoothly move the camera to where it should be
            if ((camera - pov.position).magnitude < pov.velocity.magnitude / 15 + 1) {
                camera = pov.position;
            } else {
                var step = (pov.position - camera) / 15;
                if (step.magnitude < 1) {
                    step = step.normal;
                }
                camera += step;
            }
        }
        public override void Render(TimeSpan drawTime) {
            this.Clear();

            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = this.GetGlyph(x, y);

                    var offset = new XY(x, Height - y) - new XY(Width / 2, Height / 2);
                    var location = camera + offset;
                    if (g == 0 || g == ' ' || this.GetForeground(x, y).A == 0) {


                        if (tiles.TryGetValue(location.roundDown, out var tile)) {
                            if (tile.Background == Color.Transparent) {
                                tile.Background = World.backdrop.GetBackground(location, camera);
                            }
                            this.SetCellAppearance(x, y, tile);
                        } else {
                            this.SetCellAppearance(x, y, World.backdrop.GetTile(location, camera));
                        }
                    } else {
                        this.SetBackground(x, y, World.backdrop.GetBackground(location, camera));
                    }

                }
            }
            base.Render(drawTime);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            mouse.nowPos = new Point(mouse.nowPos.X, Height - mouse.nowPos.Y);
            if (mouse.left == ClickState.Held) {
                camera += new XY(mouse.prevPos - mouse.nowPos);
            }

            return base.ProcessMouse(state);

        }
        public override bool ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(Keys.K)) {
                if (pov.active) {
                    pov.Destroy(pov);
                }
            }


            if (info.IsKeyPressed(Escape)) {
                server.Stop();
                prev.pov = null;
                prev.camera = camera;
                SadConsole.Game.Instance.Screen = prev;
                prev.IsFocused = true;
            }
            return base.ProcessKeyboard(info);
        }
    }
}
