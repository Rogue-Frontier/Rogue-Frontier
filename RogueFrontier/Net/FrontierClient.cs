
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
using System.Threading;
using System.Threading.Tasks;

namespace RogueFrontier {
    class FrontierClient : TcpClient {
        private ScreenClient game;
        private MemoryStream received;
        private int length;
        public FrontierClient(string address, int port, ScreenClient game) : base(address, port) {
            this.game = game;
        }
        protected override void OnConnected() { }
        protected override void OnDisconnected() { }
        protected override void OnReceived(byte[] buffer, long offset, long size) {
            var s = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            var m = Regex.Match(s, "^(?<length>[0-9]+)");
            if (m.Success) {
                received = new MemoryStream();
                var l = m.Groups["length"].Value;
                length = int.Parse(l);
                if (s.Length > l.Length) {
                    var b = Encoding.UTF8.GetBytes(l);
                    OnReceived(buffer, offset + b.LongLength, size - b.LongLength);
                }
            } else {
                received.Write(buffer, (int)offset, (int)size);
                CheckReceived();
            }
        }
        public void CheckReceived() {
            if (received.Length >= length) {
                var b = new byte[length];
                received.Position = 0;
                received.Read(b, 0, length);

                //var str = Space.Unzip(received);
                //var command = SaveGame.Deserialize(Encoding.UTF8.GetString(received.ToArray()));

                var r = Space.Unzip(b);
                var command = SaveGame.Deserialize(r);
                switch (command) {
                    case TellClient.SetWorld c:
                        var sys = c.sys;
                        game.World = sys;

                        AIShip ai = null;
                        var playerId = game.playerMain?.playerShip?.Id;
                        if (playerId.HasValue && game.entityLookup.TryGetValue(playerId.Value, out var en)) {
                            ai = en as AIShip;
                        }
                        ai ??= sys.entities.all.OfType<AIShip>().FirstOrDefault();
                        if (ai != null) {
                            game.removed = ai;
                            sys.RemoveEntity(ai);
                            var playerShip = new PlayerShip(new Player(game.prev.settings), ai.ship);
                            sys.AddEntity(playerShip);
                            game.SetPlayerMain(new PlayerMain(game.Width, game.Height,
                                new Profile(), playerShip));
                            TellServer(new TellServer.AssumePlayerShip() { shipId = ai.Id });
                        }
                        break;
                    case TellClient.SetCamera c:
                        game.camera = c.pos;
                        break;
                    case TellClient.SyncSharedState c:
                        foreach (var state in c.entities) {
                            if (game.entityLookup.TryGetValue(state.Id, out Entity e)) {
                                e.SetSharedState(state);
                            }
                        }
                        break;
                }

                received.Close();
            }
        }
        public void TellServer(TellServer c) {
            var s = Space.Zip(SaveGame.Serialize(c));
            SendAsync($"{s.Length}");
            SendAsync(s);
        }
        protected override void OnError(SocketError error) =>
            Debug.WriteLine($"Chat TCP client caught an error with code {error}");
    }
    public interface TellClient {
        public struct SetWorld : TellClient {
            public System sys;
        }
        public struct SetCamera : TellClient {
            public XY pos;
        }
        public struct SyncSharedState : TellClient {
            public List<Entity> entities;
        }
    }

    public class ScreenClient : Console {
        public TitleScreen prev;
        public System World;
        public XY camera;

        public Console PauseMenu;
        public PlayerMain playerMain;
        public AIShip removed;
        private FrontierClient client;

        public MouseWatch mouse = new();
        public Dictionary<(int, int), ColoredGlyph> tiles = new();
        public Dictionary<int, Entity> entityLookup = new();
        public Dictionary<PlayerShip, PlayerInput> playerControls = new();
        public ScreenClient(int width, int height, TitleScreen prev) : base(width, height) {
            this.prev = prev;
            this.World = prev.World;
            this.camera = prev.camera;


            this.playerMain = null;
            this.removed = null;
            this.client = new FrontierClient("127.0.0.1", 1111, this);
            
            UseKeyboard = true;

            UpdateUI();
        }
        public void SetPlayerMain(PlayerMain playerMain) {
            this.playerMain = playerMain;

            var fs = FontSize * 3;
            this.PauseMenu = new Console(Width, Height);
            this.PauseMenu.Children.Add(new LabelButton("Leave Player", LeavePlayer) { Position = new Point(2, 2), FontSize = fs });
            this.PauseMenu.Children.Add(new LabelButton("Disconnect", Disconnect) { Position = new Point(2, 4), FontSize = fs });

            this.PauseMenu.IsVisible = false;
        }
        public void UpdateUI() {
            var fs = FontSize * 2;
            Children.Clear();
            LabelButton clientButton = null;

            void UpdateText() {
                clientButton.text = client.IsConnected ? "Connected" :
                    client.IsConnecting ? "Connecting..." : "Connect";
            }
            void ConnectClient() {
                if (client.IsConnected) {
                    Task.Run(() => {
                        client.DisconnectAsync();
                        UpdateText();
                    });
                } else if (client.IsConnecting) {
                    Task.Run(() => {
                        client.DisconnectAsync();
                        UpdateText();
                    });
                } else {
                    Task.Run(() => {
                        client.ConnectAsync();
                        UpdateText();
                    });
                }
                UpdateText();
            }

            clientButton = new LabelButton("Connect",
                () => ConnectClient()) { Position = new Point(1, 1), FontSize = fs };
            Children.Add(clientButton);

            UpdateText();
        }
        public override void Update(TimeSpan timeSpan) {
            if (playerMain != null) {

                if (PauseMenu.IsVisible) {
                    PauseMenu.IsFocused = true;
                    PauseMenu.Update(timeSpan);

                    playerControls.UpdatePlayerControls();
                    playerMain.Update(timeSpan);
                    entityLookup.UpdateEntityLookup(World);

                    IsFocused = true;
                    base.Update(timeSpan);
                } else {
                    playerControls.UpdatePlayerControls();
                    playerMain.IsFocused = true;
                    playerMain.Update(timeSpan);
                    entityLookup.UpdateEntityLookup(World);

                    IsFocused = true;
                    base.Update(timeSpan);
                }

                return;
            } else {
                playerControls.UpdatePlayerControls();

                World.UpdateAdded();
                World.UpdateActive();
                World.UpdateRemoved();

                entityLookup.UpdateEntityLookup(World);

                tiles.Clear();
                World.PlaceTiles(tiles);
            }
        }
        public override void Render(TimeSpan drawTime) {
            if (playerMain != null) {
                playerMain.Render(drawTime);
                if(PauseMenu.IsVisible) {
                    PauseMenu.Clear();
                    for(int y = 0; y < PauseMenu.Height; y++) {
                        for(int x = 0; x < PauseMenu.Width; x++) {
                            PauseMenu.SetBackground(x, y, Color.Black.SetAlpha(128));
                        }
                    }
                    PauseMenu.Render(drawTime);
                }
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
             if(playerMain != null) {
                if (PauseMenu.IsVisible) {
                    PauseMenu.ProcessMouseTree(state.Mouse);
                } else {
                    playerMain.ProcessMouseTree(state.Mouse);
                }
                return true;
            } else {
                mouse.Update(state, IsMouseOver);
                mouse.nowPos = new Point(mouse.nowPos.X, Height - mouse.nowPos.Y);
                if (mouse.left == ClickState.Held) {
                    camera += new XY(mouse.prevPos - mouse.nowPos);
                }

                return base.ProcessMouse(state);
            }

        }
        public void LeavePlayer() {
            if (playerMain != null) {
                playerMain.playerShip.Detach();
                World.RemoveEntity(playerMain.playerShip);
                playerMain = null;
            }
            if (removed != null) {
                World.AddEntity(removed);
                World.AddEffect(new Heading(removed));
            }
            client.TellServer(new TellServer.LeavePlayerShip());
            UpdateUI();
        }
        public void Disconnect() {
            LeavePlayer();
            client.Disconnect();
            UpdateUI();
        }
        public override bool ProcessKeyboard(Keyboard info) {

            if (info.IsKeyPressed(Keys.Escape)) {
                if (playerMain != null) {
                    if (playerMain.sceneContainer.Children.Any()) {
                        return playerMain.ProcessKeyboard(info);
                    }
                    PauseMenu.IsVisible = !PauseMenu.IsVisible;
                } else {
                    client.Disconnect();
                    prev.pov = null;
                    prev.camera = camera;
                    Game.Instance.Screen = prev;
                    prev.IsFocused = true;
                }
            } else if (playerMain != null) {
                var result = playerMain.ProcessKeyboard(info);
                if (playerMain.playerControls.input != null) {
                    client.TellServer(new TellServer.ControlPlayerShip() { input = playerMain.playerControls.input });
                }
                return result;
            }
            return base.ProcessKeyboard(info);
        }
    }
}
