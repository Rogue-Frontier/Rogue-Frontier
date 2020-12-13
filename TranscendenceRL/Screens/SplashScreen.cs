using System;
using SadConsole;
using Console = SadConsole.Console;
using System.Collections.Generic;
using Common;
using SadRogue.Primitives;

namespace TranscendenceRL {
    public class SplashScreen : Console {
        Action next;
        World World;
        public Dictionary<(int, int), ColoredGlyph> tiles;
        XY screenCenter;
        double time;
        public SplashScreen(Action next) : base(TranscendenceRL.Width / 2, TranscendenceRL.Height / 2) {
            this.next = next;
            FontSize = FontSize * 2;
            Random r = new Random(3);
            this.World = new World();
            tiles = new Dictionary<(int, int), ColoredGlyph>();
            screenCenter = new XY(Width / 2, Height / 2);
            var lines = new string[] {
                @"             /^\             ",
                @"        ___ / | \ ___        ",
                @"       /___/__|__\___\       ",
                @"    ___      | |      ___    ",
                @"   /__ /\    | |    /\ __\   ",
                @"     //  \   | |   /  \\     ",
                @"    //  /^\  | |  /^\  \\    ",
                @"   //    |   | |   |    \\   ",
                @"  // ^   |   | |   |   ^ \\  ",
                @" //  |   |   | |   |   |  \\ ",
                @"||-------------------------||",
                @"||  Triagony  Productions  ||",
                @"||-------------------------||"
            };
            for (int y = 0; y < lines.Length; y++) {
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
            World.UpdateActive();
            World.UpdateActive(tiles);
            World.UpdateRemoved();
            base.Update(timeSpan);


            if (time < 10) {
                time += timeSpan.TotalSeconds;
            } else {
                next();
            }
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
    }
}
