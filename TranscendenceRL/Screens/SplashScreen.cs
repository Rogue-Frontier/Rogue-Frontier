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
    class SplashScreen : Console {
        Console next;
        World World;
        public Dictionary<(int, int), ColoredGlyph> tiles;
        XY screenCenter;
        double time;
        public SplashScreen(Console next) : base(next.Width/2, next.Height/2) {
            this.next = next;
            FontSize = next.FontSize * 2;
            Random r = new Random();
            this.World = new World();
            tiles = new Dictionary<(int, int), ColoredGlyph>();
            screenCenter = new XY(Width / 2, Height / 2);
            var lines = new string[] {
                @"          /^\          ",
                @"         / | \         ",
                @"        /__|__\        ",
                @"  ___      |     ___   ",
                @"     /\    |    /\     ",
                @"    /  \   |   /  \    ",
                @"   /  /^\  |  /^\  \   ",
                @"  /    |   |   |    \  ",
                @" / ^   |   |   |   ^ \ ",
                @"/  |   |   |   |   |  \",
                @"-----------------------",
                @" Triagony  Productions ",
            };
            for(int y = 0; y < lines.Length; y++) {
                var s = lines[y];
                var pos = new XY(-s.Length, -lines.Length + y * 2);
                var margin = new AIShip(new BaseShip(World, ShipClass.empty, new Sovereign(), pos) { rotationDegrees = 90 }, null);
                for (int x = 0; x < s.Length; x++) {
                    var c = s[x];
                    if (c == ' ')
                        continue;
                    var shipClass = new ShipClass() {
                        thrust = 2,
                        maxSpeed = 25,
                        rotationAccel = 8,
                        rotationDecel = 12,
                        rotationMaxSpeed = 10,
                        tile = new ColoredGlyph(Color.LightCyan, Color.Transparent, c),
                        devices = new DeviceList(),
                        damageDesc = ShipClass.empty.damageDesc
                    };
                    XY p = null;
                    switch (r.Next(0, 4)) {
                        case 0:
                            p = new XY(-Width, r.Next(-Height, Height));
                            break;
                        case 1:
                            p = new XY(Width, r.Next(-Height, Height));
                            break;
                        case 2:
                            p = new XY(r.Next(-Width, Width), Height);
                            break;
                        case 3:
                            p = new XY(r.Next(-Width, Width), -Height);
                            break;
                    }
                    var ship = new AIShip(new BaseShip(World, shipClass, new Sovereign(), p), new ApproachOrder(margin, new XY(0, -2 - (x * 2))));
                    World.AddEntity(ship);
                    //World.AddEffect(new Heading(ship));
                }
            }
        }
        public override void Update(TimeSpan timeSpan) {
            tiles.Clear();
            World.UpdateAdded();
            World.UpdateActive();
            World.UpdateActive();
            World.UpdateActive();
            World.UpdateActive(tiles);
            World.UpdateRemoved();
            base.Update(timeSpan);


            if(time < 10) {
                time += timeSpan.TotalSeconds;
            } else {
                Next();
            }
        }
        public void Next() {
            SadConsole.Game.Instance.Screen = next;
            next.IsFocused = true;
        }
        public override void Render(TimeSpan drawTime) {
            this.Clear();

            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = this.GetGlyph(x, y);

                    var location = new XY(x + 0.1, y + 0.1) - screenCenter;
                    
                    if (tiles.TryGetValue(location.RoundDown, out var tile)) {
                        this.SetCellAppearance(x, y, tile);
                    }
                }
            }
            base.Render(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(Keys.Enter)) {
                Next();
            }
            return base.ProcessKeyboard(info);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
    }
}
