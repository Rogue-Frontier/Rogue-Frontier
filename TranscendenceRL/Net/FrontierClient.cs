
using Common;
using System;
using System.Net.Sockets;
using Debug = System.Diagnostics.Debug;
using TcpClient = NetCoreServer.TcpClient;
using ArchConsole;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using TranscendenceRL.Net;

namespace TranscendenceRL {
    class FrontierClient : TcpClient {
        private ScreenClient game;
        private MemoryStream received;
        private ClientCommands command;
        private int length;
        public FrontierClient(string address, int port, ScreenClient game) : base(address, port) {
            this.game = game;
        }
        protected override void OnConnected() { }
        protected override void OnDisconnected() { }
        protected override void OnReceived(byte[] buffer, long offset, long size) {
            var s = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            var m = Regex.Match(s, "([A-Z]+)([0-9]+)");
            if (m.Success) {
                received = new MemoryStream();
                command = Enum.Parse<ClientCommands>(m.Groups[1].Captures[0].Value);
                length = int.Parse(m.Groups[2].Captures[0].Value);
            } else {
                received.Write(buffer, (int)offset, (int)size);
                if (received.Length >= length) {
                    //var str = Space.Unzip(received);
                    var str = Encoding.UTF8.GetString(received.ToArray());
                    var d = SaveGame.Deserialize(str);

                    switch (command) {
                        case ClientCommands.WORLD:
                            game.World = (World)d;
                            game.InitPlayer();
                            break;
                        case ClientCommands.CAMERA:
                            game.camera = (XY)d;
                            break;
                    }

                    received.Close();
                }
            }
        }
        protected override void OnError(SocketError error) =>
            Debug.WriteLine($"Chat TCP client caught an error with code {error}");
    }

    public class ScreenClient : Console {
        public Dictionary<(int, int), ColoredGlyph> tiles = new Dictionary<(int, int), ColoredGlyph>();
        public MouseWatch mouse = new MouseWatch();

        public TitleScreen prev;
        public World World;
        public XY camera;

        public PlayerMain playerMain;
        public AIShip removed;
        private FrontierClient client;
        public ScreenClient(int width, int height, TitleScreen prev) : base(width, height) {
            this.prev = prev;
            this.World = prev.World;
            this.camera = prev.camera;

            this.playerMain = null;
            this.removed = null;
            
            this.client = new FrontierClient("127.0.0.1", 1111, this);
            this.client.ConnectAsync();

            UseKeyboard = true;
        }
        public void InitPlayer() {
            var s = World.entities.all.OfType<AIShip>().FirstOrDefault();
            World.RemoveEntity(s);
            if (s != null) {
                var playerShip = new PlayerShip(new Player(prev.settings), s.ship);
                World.AddEntity(playerShip);
                playerMain = new PlayerMain(Width, Height, new Profile(), playerShip);
            }
        }
        public override void Update(TimeSpan timeSpan) {
            if (playerMain != null) {
                playerMain.IsFocused = true;
                playerMain.Update(timeSpan);
                IsFocused = true;
                base.Update(timeSpan);
                return;
            }
            World.UpdateAdded();
            World.UpdateActive();
            World.UpdateRemoved();
            tiles.Clear();
            World.PlaceTiles(tiles);
        }
        public override void Render(TimeSpan drawTime) {
            if (playerMain != null) {
                playerMain.Render(drawTime);
                return;
            }

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

            string s = SaveGame.Serialize(info);
            var k = SaveGame.Deserialize(s);

            if (info.IsKeyPressed(Keys.Escape)) {
                if (playerMain != null) {
                    if (playerMain.sceneContainer.Children.Any()) {
                        return playerMain.ProcessKeyboard(info);
                    }
                    playerMain.playerShip.Detach();
                    World.RemoveEntity(playerMain.playerShip);

                    World.AddEntity(removed);
                    World.AddEffect(new Heading(removed));
                } else {
                    client.Disconnect();
                    prev.pov = null;
                    prev.camera = camera;
                    SadConsole.Game.Instance.Screen = prev;
                    prev.IsFocused = true;
                }
            } else if (playerMain != null) {
                return playerMain.ProcessKeyboard(info);
            }
            return base.ProcessKeyboard(info);
        }
    }
}
