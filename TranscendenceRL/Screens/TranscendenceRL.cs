using System;
using SadConsole;
using Console = SadConsole.Console;
using TranscendenceRL;
using TranscendenceRL.Screens;
using SadConsole.Renderers;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using SadConsole.Input;
using Common;
using SadRogue.Primitives;
using System.Linq;
using Newtonsoft.Json;
using ASECII;

namespace TranscendenceRL {

	class TranscendenceRL {
		public static int TICKS_PER_SECOND = 60;
		public static int Width = 150, Height = 90;

		static void Main(string[] args) {
			// Setup the engine and create the main window.
			SadConsole.Game.Create(Width, Height, "RogueFrontierContent/IBMCGA.font");
            // Hook the start event so we can add consoles to the system.
            SadConsole.Game.Instance.OnStart = Init;
#if DEBUG
			// Start the game.
			SadConsole.Game.Instance.Run();
			SadConsole.Game.Instance.Dispose();
#else
			try {
				// Start the game.
				SadConsole.Game.Instance.Run();
			} catch (Exception e) {
				throw;
			} finally {
				SadConsole.Game.Instance.Dispose();
			}
#endif
		}

		private static void Init() {
			if(false) {

				GameHost.Instance.Screen = new BackdropConsole(Width, Height, new Backdrop(), () => new Common.XY(0.5, 0.5));
				return;
			}

			World w = new World();
			w.types.Load("RogueFrontierContent/Main.xml");

#if !DEBUG
            ShowTitle();
#else
            var img = new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText("RogueFrontierContent/RogueFrontierPoster.cg")));

            SplashScreen splash = null;
            splash = new SplashScreen(() => {
                var p = new PauseTransition(1, splash, ShowVision) { IsFocused = true };
                GameHost.Instance.Screen = p;
            }) { IsFocused = true };
            GameHost.Instance.Screen = splash;
            void ShowVision() {
                var fade = new FadeOut(splash, () => {
                    var display = new DisplayImage(Width, Height, img);
                    GameHost.Instance.Screen = new FadeIn(new PauseTransition(2, display, () => { GameHost.Instance.Screen = new FadeOut(display, ShowTitle, 1) { IsFocused = true }; })) { IsFocused = true };
                }, 1) { IsFocused = true };
                GameHost.Instance.Screen = fade;
            }
#endif
            void ShowTitle() {
                var title = new TitleSlideOpening(new TitleScreen(Width, Height, w)) { IsFocused = true };
                GameHost.Instance.Screen = title;
            }
            //GameHost.Instance.Screen = new TitleDraw();
        }

        class SplashScreen : Console {
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
                @"             /^\          ",
                @"        ___ / | \ ___     ",
                @"       /___/__|__\___\    ",
                @"    ___      | |      ___   ",
                @"   /__ /\    | |    /\ __\  ",
                @"     //  \   | |   /  \\    ",
                @"    //  /^\  | |  /^\  \\   ",
                @"   //    |   | |   |    \\  ",
                @"  // ^   |   | |   |   ^ \\ ",
                @" //  |   |   | |   |   |  \\",
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
            public override bool ProcessKeyboard(Keyboard info) {
                if (info.IsKeyPressed(Keys.Enter)) {
                    next();
                }
                return base.ProcessKeyboard(info);
            }
            public override bool ProcessMouse(MouseScreenObjectState state) {
                return base.ProcessMouse(state);
            }
        }


        class DisplayImage : Console {
            public ColorImage image;
            public DisplayImage(int width, int height, ColorImage image) : base(width, height) {
                this.image = image;
            }
            public override void Render(TimeSpan delta) {
                //var adj = (new Point(Width, Height) - dimensions.Size) / 2 - dimensions.Position;
                var adj = new Point() - new Point(5, 5);
                foreach (((int x, int y) p, ColoredGlyph t) in image.Sprite) {
                    var pos = (Point)p + adj;

                    this.SetCellAppearance(pos.X, pos.Y, t);
                }

                base.Render(delta);
            }
        }
    }
}
