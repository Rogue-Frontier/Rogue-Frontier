using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using System.IO;
using Common;
using SadConsole;
using Console = SadConsole.Console;
using System.Linq;
using static SadConsole.Input.Keys;
using static UI;
using ASECII;

namespace TranscendenceRL {
    class ArenaScreen : Console {
        World World;
        public XY camera;
        public Dictionary<(int, int), ColoredGlyph> tiles;
        XY screenCenter;
        MouseWatch mouse;

        SpaceObject pov;
        SpaceObject nearest;

        public ArenaScreen(Console prev, World World) : base(prev.Width, prev.Height) {
            UseKeyboard = true;
            this.World = World;
            camera = new XY(0.1, 0.1);
            tiles = new Dictionary<(int, int), ColoredGlyph>();
            screenCenter = new XY(Width / 2, Height / 2);
            mouse = new MouseWatch();

            var sovereign = Sovereign.Gladiator;

            {

                var x = 8;
                var y = 7;
                var label = new Label("Sovereign") { Position = new Point(x, y++) };
                var field = new TextField(24) { Position = new Point(x, y++) };
                ButtonList buttons = new ButtonList(this, new Point(x, y++));
                field.TextChanged += _ => UpdateSovereignListing();

                this.Children.Add(label);
                this.Children.Add(field);
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
                this.Children.Add(field);
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
            FocusOnMouseClick = true;
        }
        public override void Update(TimeSpan timeSpan) {
            World.UpdateAdded();

            tiles.Clear();
            World.UpdateActive(tiles);
            World.UpdateRemoved();

            XY worldPos = new XY(mouse.nowPos) - screenCenter + camera;
            nearest = World.entities.all.OfType<SpaceObject>().OrderBy(e => (e.Position - worldPos).Magnitude).FirstOrDefault();

            if(pov != null) {
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
            }
            

            Heading.Crosshair(World, nearest.Position);

            base.Update(timeSpan);
        }
        public override void Draw(TimeSpan drawTime) {
            this.Clear();

            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = this.GetGlyph(x, y);

                    var offset = new XY(x, y) - screenCenter;
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
            base.Draw(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(Keys.F)) {
                if (pov == nearest) {
                    pov = null;
                } else {
                    pov = nearest;
                }
            }
            foreach(var pressed in info.KeysDown) {
                var delta = 1 / 3f;
                switch(pressed.Key) {
                    case Keys.Up:
                        camera += new XY(0, -delta);
                        break;
                    case Keys.Down:
                        camera += new XY(0, delta);
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
            mouse.Update(state, IsMouseOver);
            if(mouse.left == MouseState.Held) {
                camera += new XY(mouse.prevPos - mouse.nowPos);
            }
            return base.ProcessMouse(state);
        }
    }
}
