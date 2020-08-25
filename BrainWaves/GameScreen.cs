
using Common;
using SadConsole;
using System.Collections.Generic;
using SadRogue.Primitives;
using System;
using Console = SadConsole.Console;
using SadConsole.Input;

namespace BrainWaves {
    public class GameScreen : Console {

		World World;
		Player Player;
		WorldScreen WorldScreen;
		MessageScreen MessageScreen;
		public GameScreen(int Width, int Height) : base(Width, Height) {
			World = new World();
			Random rnd = new Random();


			HashSet<Rectangle> rooms = new HashSet<Rectangle>();
			HashSet<(int, int)> built = new HashSet<(int, int)>();
			var mainRoom = new Rectangle(0, 0, rnd.Next(12, 16), rnd.Next(12, 16));
			BuildWalls(mainRoom);
			rooms.Add(mainRoom);
			BuildRoom(mainRoom);

			void BuildRoom(Rectangle r) {
				(int x, int y) p;
				(int x, int y) c1;

				int width = Math.Abs(r.Width);
				int height = Math.Abs(r.Height);
				if(rnd.Next(2) == 0) {
					//Along Up/down
					p.x = r.MinExtentX + rnd.Next(1, width - 2);

					if (rnd.Next(2) == 0) {
						p.y = r.MinExtentY;
						c1 = (p.x, p.y - 1);
					} else {
						p.y = r.MinExtentY + height - 1;
						c1 = (p.x, p.y + 1);
					}
                } else {
					p.y = r.MinExtentY + rnd.Next(1, height - 2);
					if(rnd.Next(2) == 0) {
						p.x = r.MinExtentX;
						c1 = (p.x - 1, p.y);
					} else {
						p.x = r.MinExtentX + width - 1;
						c1 = (p.x + 1, p.y);
					}
				}
				if(built.Add(c1)) {
					var c2 = c1;

					width = -1;
					
					bool b = built.Add((c2.x + 1, c2.y));
					if(b) {
						while (b && (width < 5 || rnd.Next(0, 5) > 0)) {
							width++;
							c2.x++;
							b = built.Add((c2.x + 1, c2.y));
						}
					} else {
						b = built.Add((c2.x - 1, c2.y));
						if (b) {
							while (b && (width < 5 || rnd.Next(0, 5) > 0)) {
								width++;
								c2.x--;
								b = built.Add((c2.x - 1, c2.y));
							}
						}
					}

					height = -1;
					b = built.Add((c2.x, c2.y - 1));
					if (b) {
						while (b && (height < 5 || rnd.Next(0, 5) > 0)) {
							height++;
							c2.y--;
							b = built.Add((c2.x, c2.y - 1));
						}
					} else {
						b = built.Add((c2.x, c2.y + 1));
						if (b) {
							while (b && (height < 5 || rnd.Next(0, 5) > 0)) {
								height++;
								c2.y++;
								b = built.Add((c2.x, c2.y + 1));
							}
						}
					}

					Rectangle next = new Rectangle(c1, c2);
					next = next.WithPosition(next.Position - new Point(1, 1)).WithSize(next.Size + new Point(2, 2));
					rooms.Add(next);
					BuildWalls(next);
					BuildRoom(next);

					World.voxels.Set(p.x, p.y, null);
				}
            }

			void BuildWalls(Rectangle r) {
				foreach(var p in r.Positions()) {
					built.Add(p);
                }
				foreach(var p in r.PerimeterPositions()) {
					World.voxels[p] = new Wall();
                }
            }

			Player = new Player(World) { Position = new XY(0, 0) };
			WorldScreen = new WorldScreen(Width, Height - 10, World);
			MessageScreen = new MessageScreen(Width, 10, Player) { Position = new Point(0, Height - 10) };

			Children.Add(WorldScreen);
			Children.Add(MessageScreen);
        }

        public override bool ProcessKeyboard(Keyboard keyboard) {
			foreach(var k in keyboard.KeysPressed) {
				var p = Player.Position;
				switch(k.Key) {
					case Keys.Up:
						p += new XY(0, 1);
						break;
					case Keys.Down:
						p += new XY(0, -1);
						break;
					case Keys.Right:
						p += new XY(1, 0);
						break;
					case Keys.Left:
						p += new XY(-1, 0);
						break;
                }
            }
            return base.ProcessKeyboard(keyboard);
        }
    }
    public class WorldScreen : Console {
		World World;
		XY camera;
		Dictionary<(int, int), ColoredGlyph> tiles;
		public WorldScreen(int Width, int Height, World World) : base(Width, Height) {
			this.World = World;
			camera = new XY();
			tiles = new Dictionary<(int, int), ColoredGlyph>();
        }
        public override void Render(TimeSpan delta) {
			DrawWorld();
            base.Render(delta);
        }
        public void DrawWorld() {
			int ViewWidth = Width;
			int ViewHeight = Height;
			int HalfViewWidth = ViewWidth / 2;
			int HalfViewHeight = ViewHeight / 2;

			for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
				for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
					XY location = camera + new XY(x, y);

					var xScreen = x + HalfViewWidth;
					var yScreen = HalfViewHeight - y;
					if (tiles.TryGetValue(location.RoundDown, out var tile)) {
						this.SetCellAppearance(xScreen, yScreen, tile);
					} else {
						var v = World.voxels.Get(location.xi, location.yi);
						if(v == null) {
							this.SetCellAppearance(xScreen, yScreen, new ColoredGlyph(Color.White, Color.Black, '.'));
						} else {
							this.SetCellAppearance(xScreen, yScreen, new ColoredGlyph(Color.White, Color.Black, 'x'));
						}

					}
				}
			}
		}
	}
	class MessageScreen : Console {
		Player Player;
		public MessageScreen(int Width, int Height, Player Player) : base(Width, Height) {
			this.Player = Player;
        }
        public override void Render(TimeSpan delta) {
			int y = 0;
			foreach(var (s, count) in Player.messages.dict) {
				this.Print(0, y++, $"{s,-32} (x{count})", Color.Black, Color.White);
            }
            base.Render(delta);
        }
    }
}
