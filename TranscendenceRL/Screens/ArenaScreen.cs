using SadConsole.Input;
using System;
using System.Collections.Generic;
using SadRogue.Primitives;
using Common;
using SadConsole;
using Console = SadConsole.Console;
using System.Linq;
using static SadConsole.Input.Keys;
using ArchConsole;
using static TranscendenceRL.PlayerShip;

namespace TranscendenceRL {

    struct ArenaScreenReset : IContainer<PlayerDestroyed> {
        public ArenaScreen arena;
        public ArenaScreenReset(ArenaScreen arena) {
            this.arena = arena;
        }
        public PlayerDestroyed Value {
            get {
                var t = this;
                return (p, s, w) => {
                    t.arena.camera = t.arena.playerMain.camera;
                    t.arena.playerMain = null;
                    t.arena.IsFocused = true;
                };
            }
        }
        public override bool Equals(object obj) => obj is ArenaScreenReset r && r.arena == arena;
    }
    class ArenaScreen : Console {
        TitleScreen prev;
        Settings settings;
        World World;
        public XY camera;
        public Dictionary<(int, int), ColoredGlyph> tiles;
        XY screenCenter;
        MouseWatch mouse;

        public SpaceObject pov;
        SpaceObject nearest;

        public PlayerMain playerMain;

        public ArenaScreen(TitleScreen prev, Settings settings, World World) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.settings = settings;
            this.World = World;
            this.camera = new XY(0.1, 0.1);
            this.tiles = new Dictionary<(int, int), ColoredGlyph>();
            this.screenCenter = new XY(Width / 2, Height / 2);
            this.mouse = new MouseWatch();

            UseKeyboard = true;
            FocusOnMouseClick = true;

            InitControls();
            void InitControls() {
                var sovereign = Sovereign.Gladiator;

                {

                    var x = 8;
                    var y = 7;
                    var label = new Label("Sovereign") { Position = new Point(x, y++) };
                    var field = new TextField(24) { Position = new Point(x, y++) };
                    ButtonList buttons = new ButtonList(this, new Point(x, y++));
                    field.TextChanged += _ => UpdateSovereignListing();

                    Children.Add(label);
                    Children.Add(field);
                    UpdateSovereignLabel();
                    UpdateSovereignListing();
                    void UpdateSovereignListing() {
                        var text = field.text;
                        buttons.Clear();
                        var sovereignDict = World.types.sovereign;
                        foreach (var type in sovereignDict.Keys.OrderBy(k => k).Where(k => k.Contains(text))) {
                            buttons.Add(type, (Action)(() => {
                                sovereign = sovereignDict[type];
                                UpdateSovereignLabel();
                            }));
                        }
                    }
                    void UpdateSovereignLabel() {
                        label.text = new ColoredString($"Sovereign: {sovereign.codename}");
                    }
                }
                {
                    var x = 36;
                    var y = 8;
                    var field = new TextField(24) { Position = new Point(x, y++) };
                    ButtonList buttons = new ButtonList(this, new Point(x, y++));
                    field.TextChanged += _ => UpdateShipListing();
                    Children.Add(field);
                    UpdateShipListing();

                    void UpdateShipListing() {
                        var text = field.text;
                        buttons.Clear();
                        var shipClassDict = World.types.shipClass;
                        foreach (var type in shipClassDict.Keys.OrderBy(k => k).Where(k => k.Contains(text))) {
                            buttons.Add(type, () => {
                                var ship = new AIShip(new BaseShip(World, shipClassDict[type], sovereign, camera), new AttackAllOrder());
                                World.AddEntity(ship);
                                World.AddEffect(new Heading(ship));
                            });
                        }
                    }
                }
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

                        base.Update(timeSpan);
            World.UpdateAdded();

            tiles.Clear();
            World.UpdateActive(tiles);
            World.UpdateRemoved();

            if(pov?.Active == false) {
                pov = null;
            }

            if(pov != null) {
                if(pov.Active) {
                    UpdateNearest();

                    //Smoothly move the camera to where it should be
                    if ((camera - pov.Position).Magnitude < pov.Velocity.Magnitude / 15 + 1) {
                        camera = pov.Position;
                    } else {
                        var step = (pov.Position - camera) / 15;
                        if (step.Magnitude < 1) {
                            step = step.Normal;
                        }
                        camera += step;
                    }
                } else {
                    pov = null;
                    UpdateNearest();
                }
            } else {
                UpdateNearest();
            }
            
            void UpdateNearest() {
                XY worldPos = new XY(mouse.nowPos) - screenCenter + camera;
                nearest = World.entities.all.OfType<SpaceObject>().OrderBy(e => (e.Position - worldPos).Magnitude).FirstOrDefault();
            }



            Heading.Crosshair(World, nearest.Position);
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

                    var offset = new XY(x, Height - y) - screenCenter;
                    var location = camera + offset;
                    if (g == 0 || g == ' ' || this.GetForeground(x, y).A == 0) {


                        if (tiles.TryGetValue(location.RoundDown, out var tile)) {
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
        public override bool ProcessKeyboard(Keyboard info) {

            if (info.IsKeyPressed(Escape)) {
                if (playerMain != null) {
                    playerMain.playerShip.Detach();
                    World.RemoveEntity(playerMain.playerShip);
                    var aiShip = new AIShip(playerMain.playerShip.Ship, new AttackAllOrder());
                    World.AddEntity(aiShip);

                    camera = playerMain.camera;
                    pov = aiShip;

                    playerMain = null;
                    this.IsFocused = true;
                } else {
                    prev.pov = null;
                    prev.camera = camera;
                    SadConsole.Game.Instance.Screen = prev;
                    prev.IsFocused = true;
                }
            } else if (playerMain != null) {
                return playerMain.ProcessKeyboard(info);
            }
            

            if (info.IsKeyPressed(Keys.A)) {
                if (nearest is AIShip a) {
                    World.RemoveEntity(a);
                    var playerShip = new PlayerShip(new Player() { Settings = settings }, a.Ship);

                    playerMain = new PlayerMain(Width, Height, World, playerShip) { IsFocused = true, camera = camera };
                    playerShip.OnDestroyed += new ArenaScreenReset(this);
                    World.AddEntity(playerShip);

                    pov = playerShip;
                }
            }
            if (info.IsKeyPressed(Keys.F)) {
                if (pov == nearest) {
                    pov = null;
                } else {
                    pov = nearest;
                }
            }
            foreach (var pressed in info.KeysDown) {
                var delta = 1 / 3f;
                switch (pressed.Key) {
                    case Keys.Up:
                        camera += new XY(0, delta);
                        break;
                    case Keys.Down:
                        camera += new XY(0, -delta);
                        break;
                    case Keys.Right:
                        camera += new XY(delta, 0);
                        break;
                    case Keys.Left:
                        camera += new XY(-delta, 0);
                        break;
                }
            }

            return base.ProcessKeyboard(info);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            if (playerMain != null) {
                return playerMain.ProcessMouse(state);
            }

            mouse.Update(state, IsMouseOver);
            mouse.nowPos = new Point(mouse.nowPos.X, Height - mouse.nowPos.Y);
            if(mouse.left == ClickState.Held) {
                camera += new XY(mouse.prevPos - mouse.nowPos);
            }

            return base.ProcessMouse(state);
        }
    }
}
