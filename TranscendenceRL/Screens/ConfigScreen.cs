using ASECII;
using Common;
using SadConsole;
using SadConsole.Input;
using SadConsole.Renderers;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Console = SadConsole.Console;

namespace TranscendenceRL.Screens {
    class ConfigScreen : Console {
        TitleScreen prev;
        Settings settings;
        World World;
        public XY camera;
        public Dictionary<(int, int), ColoredGlyph> tiles;
        XY screenCenter;
        MouseWatch mouse;

        BackdropConsole back;
        ControlKeys? currentSet;
        Dictionary<ControlKeys, LabelButton> buttons;

        public AIShip pov;
        public int povTimer;

        public ConfigScreen(TitleScreen prev, Settings settings, World World) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.settings = settings;
            this.World = World;
            camera = new XY(0.1, 0.1);
            tiles = new Dictionary<(int, int), ColoredGlyph>();
            screenCenter = new XY(Width / 2, Height / 2);
            mouse = new MouseWatch();

            UseKeyboard = true;
            FocusOnMouseClick = true;

            back = new BackdropConsole(prev.Width, prev.Height, World.backdrop, () => camera);
            currentSet = null;
            buttons = new Dictionary<ControlKeys, LabelButton>();
            var controls = settings.controls;

            int x = 8;
            int y = 8;
            foreach(var control in settings.controls.Keys) {
                var c = control;
                string label = GetLabel(c);
                LabelButton b = null;
                b = new LabelButton(label, () => {
                    ResetLabel();
                    currentSet = c;
                    b.text = $"{control.ToString(),-16} [Press Key]";
                }) { Position = new Point(x, y++) };
                
                buttons[control] = b;
                Children.Add(b);
            }
        }
        string GetLabel(ControlKeys control) => $"{control.ToString(),-16} {settings.controls[control].ToString()}";
        public void ResetLabel() {
            if (currentSet.HasValue) {
                buttons[currentSet.Value].text = GetLabel(currentSet.Value);
            }
        }
        public override void Update(TimeSpan timeSpan) {
            
            base.Update(timeSpan);
            back.Update(timeSpan);

            World.UpdateAdded();

            tiles.Clear();
            World.UpdateActive(tiles);
            World.UpdateRemoved();

            if (World.entities.all.OfType<IShip>().Count() < 5) {
                var shipClasses = World.types.shipClass.Values;
                var shipClass = shipClasses.ElementAt(World.karma.Next(shipClasses.Count));
                var angle = World.karma.NextDouble() * Math.PI * 2;
                var distance = World.karma.Next(10, 20);
                var center = World.entities.all.FirstOrDefault()?.Position ?? new XY(0, 0);
                var ship = new BaseShip(World, shipClass, Sovereign.Gladiator, center + XY.Polar(angle, distance));
                var enemy = new AIShip(ship, new AttackAllOrder());
                World.AddEntity(enemy);
                World.AddEffect(new Heading(enemy));
                //Update now in case we need a POV
                World.UpdatePresent();
            }
            if (pov == null || povTimer < 1) {
                pov = World.entities.all.OfType<AIShip>().First();
                povTimer = 150;
            } else if (!pov.Active) {
                povTimer--;
            }

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
        public override void Render(TimeSpan drawTime) {
            this.Clear();
            back.Render(drawTime);
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = this.GetGlyph(x, y);

                    var offset = new XY(x, Height - y) - screenCenter;
                    var location = camera + offset;
                    if (g == 0 || g == ' ' || this.GetForeground(x, y).A == 0) {
                        if (tiles.TryGetValue(location.RoundDown, out var tile)) {
                            this.SetCellAppearance(x, y, tile);
                        }
                    }
                }
            }
            base.Render(drawTime);
        }

        public override bool ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(Keys.Escape)) {
                if (currentSet.HasValue) {
                    buttons[currentSet.Value].text = GetLabel(currentSet.Value);
                    currentSet = null;
                } else {
                    prev.camera = camera;
                    prev.pov = pov;
                    prev.povTimer = povTimer;
                    SadConsole.Game.Instance.Screen = prev;
                    prev.IsFocused = true;
                }
            } else if(info.KeysPressed.Any()) {
                if(currentSet.HasValue) {
                    settings.controls[currentSet.Value] = info.KeysPressed.First().Key;
                    ResetLabel();
                    currentSet = null;
                }
            }


            return base.ProcessKeyboard(info);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
    }
}
