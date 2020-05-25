using Common;
using IslandHopper.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Xml.Linq;
using static IslandHopper.Constants;


namespace IslandHopper {
	static class Themes {
		public static WindowTheme Main = new SadConsole.Themes.WindowTheme() {
			ModalTint = Color.Transparent,
			FillStyle = new Cell(Color.White, Color.Black),
		};
		public static WindowTheme Sub = new WindowTheme() {
			ModalTint = Color.Transparent,
			FillStyle = new Cell(Color.Transparent, Color.Transparent)
		};
	}
	class GameConsole : ControlsConsole {
        Island World;
        DateTime lastUpdate;
        int ticks;
        public GameConsole(int Width, int Height) : base(Width, Height) {
            Theme = new WindowTheme {
                ModalTint = Color.Transparent,
                FillStyle = new Cell(Color.White, Color.Black),
            };
            IsFocused = true;
            UseKeyboard = true;
			UseMouse = true;
			this.DebugInfo($"Width: {Width}", $"Height: {Height}");

            //TO DO: Create a multi-tiled plane structure to introduce the player
            int size = 500;
            int height = 30;
            World = new Island() {
                karma = new Random(0),
                entities = new SetDict<Entity, (int, int, int)>(e => e.Position),
                effects = new SetDict<Effect, (int, int, int)>(e => e.Position),
                voxels = new ArraySpace<Voxel>(size, size, height, new Air()),
                camera = new XYZ(0, 0, 0),
                types = new TypeCollection(XElement.Parse(Properties.Resources.Items))
			};
			World.player = new Player(World, new XYZ(28.5, 29.5, 1));
			//World.AddEntity(new Player(World, new Point3(85, 85, 20)));

			for (int x = 0; x < World.voxels.Width; x++) {
				for(int y = 0; y < World.voxels.Height; y++) {
					World.voxels[new XYZ(x, y, 0)] = new Grass();
				}
			}

			for(int i = 0; i < 1; i++) {

				World.entities.Place(World.types.Lookup<ItemType>("itHotRod").GetItem(World, new XYZ(28.5, 29.5, 1)));
            }
			
			World.entities.Place(World.player);
            World.entities.Place(new Enemy(World, new XYZ(35, 35, 1)));
		}
        public override void Update(TimeSpan delta) {
            base.Update(delta);
            ticks++;
            World.entities.UpdateSpace();       //	Update all entity positions on the grid
            foreach (var e in World.entities.all) {
                e.UpdateRealtime(delta);
            }

            World.effects.UpdateSpace();
            foreach (var e in World.effects.all) {
                e.UpdateRealtime(delta);
            }
            var now = DateTime.Now;
            if (World.player.AllowUpdate() && (now - lastUpdate).TotalSeconds > 1 / 60f) {
                lastUpdate = now;
                this.DebugInfo("Global Update");
                foreach (var e in new List<Entity>(World.entities.all)) {
                    e.DebugInfo("UpdateStep() by world");
                    e.UpdateStep();
                }
                foreach (var e in new List<Effect>(World.effects.all)) {
                    e.DebugInfo("UpdateStep() by world");
                    e.UpdateStep();
                }
                World.camera = World.player.Position;
            } else {
                //System.Console.WriteLine("not updating");
            }

            var Removed = new List<Entity>();
            World.entities.all.RemoveWhere(e => {
                bool result = !e.Active;
                if (result) {
                    Removed.Add(e);
                }
                return result;
            });
            Removed.ForEach(e => e.OnRemoved());

            World.effects.all.RemoveWhere(e => !e.Active);

        }
        private int HalfWidth { get => Width / 2; }
		private int HalfHeight { get => Height / 2; }
		public override void Draw(TimeSpan delta) {
            this.DebugInfo($"Draw({delta})");
            Clear();

            int HalfViewWidth = 90;
            int HalfViewHeight = 30;
			for(int x = -HalfViewWidth; x < HalfViewWidth; x++) {
				for(int y = -HalfViewHeight; y < HalfViewHeight; y++) {
					XYZ location = World.camera + new XYZ(x, y, 0);
					
					Print(x + HalfWidth, Height - (y + HalfHeight), GetGlyph(location));
				}
			}

            Print(1, 1, "" + World.player.Position.z, Color.White);
            Print(1, 2, "" + World.camera.z, Color.White);
            var printY = (Height - 4);
            for (int i = 0; i < 30 && i < World.player.HistoryRecent.Count(); i++) {
                var entry = World.player.HistoryRecent[World.player.HistoryRecent.Count() - 1 - i];
                printY--;
                Print(1, printY, entry.ScreenTime > 1 ? entry.Desc : entry.Desc.WithOpacity((byte)(255 * entry.ScreenTime)));
            }

            printY = (Height - 3);
            foreach (var action in World.player.Actions) {
                switch (action) {
                    case WalkAction w:
                        Print(1, printY, "Walking", Color.Cyan, Color.Black);
                        break;
                    case Jump j:
                        if (j.z > 0) {
                            Print(1, printY, "Jumping", Color.Cyan, Color.Black);
                        } else {
                            Print(1, printY, "Running", Color.Cyan, Color.Black);
                        }
                        break;
                    case WaitAction wait:
                        Print(1, printY, "Waiting", Color.Cyan, Color.Black);
                        break;
                    case ShootAction fire:
                        var gun = fire.item.Gun;
                        var delay = gun.FireTimeLeft + gun.ReloadTimeLeft;
                        Print(1, printY, new ColoredString(gun.ReloadTimeLeft > 0 ? "Reloading " : gun.FireTimeLeft > 0 ? "Firing " : "Aiming ", Color.Cyan, Color.Black)
                            + fire.item.Name + new ColoredString(" ")
                            + new ColoredString(new string('>', delay)));
                        break;
                }
                printY++;
            }

            int printX;
            (printX, printY) = (Width - 16, Height - 2);
            Print(printX, printY, new ColoredString("Body HP:  ") + new ColoredString(World.player.health.bodyHP.ToString(), Color.Red, Color.Black));
            printY++;
            Print(printX, printY, new ColoredString("Blood HP: ") + new ColoredString(World.player.health.bloodHP.ToString(), Color.Red, Color.Black));


            int PreviewWidth = 10;
            int PreviewHeight = 10;

            int previewX = Width - PreviewWidth / 2;
            int previewY = PreviewHeight / 2;
            //Draw a border, up/down arrow, and z difference
            foreach (var watching in World.player.Watch) {
                for (int x = -PreviewWidth / 2; x < PreviewWidth / 2; x++) {
                    for (int y = -PreviewHeight / 2; y < PreviewHeight / 2; y++) {
                        XYZ location = watching.Position + new XYZ(x, y, 0);
                        Print(x + previewX, -y + previewY, GetGlyph(location));
                    }
                }
                previewY += PreviewHeight;
            }
            base.Draw(delta);
		}
		public ColoredGlyph GetGlyph(XYZ location) {
			var c = new ColoredGlyph(' ', Color.Transparent, Color.Transparent);
			if (World.voxels.InBounds(location)) {
                var effects = VisibleOnly(World.effects[location].Select(e => e.SymbolCenter)).ToList();
                var items = VisibleOnly(World.entities[location].Where(e => e is Item).Select(e => e.SymbolCenter)).ToList();
                var entities = World.entities[location].Where(e => !(e is Item)).Select(e => e.SymbolCenter).ToList();
                IEnumerable<ColoredGlyph> VisibleOnly(IEnumerable<ColoredGlyph> e) {
                    return e.Where(cg => (cg.Glyph != ' ' && cg.Foreground.A != 0) || cg.Background.A != 0);
                }
                if(effects.Any()) {
                    int effectIndex = (ticks / 60) % effects.Count();
                    c = effects[effectIndex];
                } else if(items.Any() && (!entities.Any() || ticks%60 >= 30)) {
                    int itemIndex = (ticks / 120) % items.Count();
                    c = items[itemIndex];
                } else if (entities.Any()) {
                    if (entities.Count() > 1) {
                        int entityIndex = (ticks / 120) % entities.Count();
                        int subticks = ticks % 120;
                        switch (subticks) {
                            case 0:
                            case 2:
                            case 3:
                                c = new ColoredGlyph('|', Color.White, Color.Black);
                                break;
                            case 4:
                            case 5:
                            case 6:
                                c = new ColoredGlyph('\\', Color.White, Color.Black);
                                break;
                            case 7:
                            case 8:
                            case 9:
                                c = new ColoredGlyph('-', Color.White, Color.Black);
                                break;
                            case 10:
                            case 11:
                            case 12:
                                c = new ColoredGlyph('/', Color.White, Color.Black);
                                break;
                            default:
                                c = entities[entityIndex];
                                break;
                        }
                    } else {
                        c = entities.First();
                    }
                } else if (!(World.voxels[location] is Air)) {
					c = World.voxels[location].CharCenter;
				} else {
					location = location + new XYZ(0, 0, -1);
					if (World.voxels.InBounds(location)) {
						c = World.voxels[location].CharAbove;
					}
				}
			}
			return c;
		}

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
            this.DebugInfo("ProcessKeyboard");
            var player = World.player;
            if (info.IsKeyDown(Keys.Up) || info.IsKeyDown(Keys.Down) || info.IsKeyDown(Keys.Right) || info.IsKeyDown(Keys.Left)) {
                XYZ direction = new XYZ();
                if (info.IsKeyDown(Keys.Up)) {
                    direction += new XYZ(0, 1);
                }
                if (info.IsKeyDown(Keys.Down)) {
                    direction += new XYZ(0, -1);
                }
                if (info.IsKeyDown(Keys.Right)) {
                    direction += new XYZ(1, 0);
                }
                if (info.IsKeyDown(Keys.Left)) {
                    direction += new XYZ(-1, 0);
                }
                if (direction.Magnitude > 0) {
                    direction = direction.Normal;
                    if (info.IsKeyDown(Keys.RightControl)) {
                        //Run
                        if (World.player.OnGround() && !World.player.Actions.Any(a => a is Jump)) {
                            double runAccel = 1;
                            int runCooldown = 10;
                            int runTime = 10;
                            World.player.Actions.Add(new Jump(World.player, direction * runAccel, runCooldown, runTime));
                        }
                    } else {
                        if (World.player.OnGround() && !World.player.Actions.Any(a => a is WalkAction))
                            World.player.Actions.Add(new WalkAction(World.player, direction));
                    }
                }

            }

            //Other actions besides walking/running
            if (info.IsKeyDown(Keys.OemOpenBrackets)) {
                //Go up stairs
            } else if (info.IsKeyDown(Keys.OemCloseBrackets)) {
                //Go down stairs
            } else if (info.IsKeyDown(Keys.J)) {
                //Jump up

                //Running is also a jump action but without z > 0, so we can jump while running
                if (player.OnGround() && !player.Actions.Any(a => a is Jump j && j.z > 0))
                    player.Actions.Add(new Jump(player, new XYZ(0, 0, 5)));
            } else if (info.IsKeyPressed(Keys.C)) {
                //


            } else if (info.IsKeyPressed(Keys.D)) {
                new ListMenu<IItem>(Width, Height, "Select inventory items to drop. Press ESC to finish.", player.Inventory.Select(Item => new ListItem(Item)), item => {
                    //Just drop the item for now
                    player.Inventory.Remove(item);
                    World.entities.Place(item);

                    World.player.Witness(new InfoEvent(new ColoredString("You drop: ") + item.Name.WithBackground(Color.Black)));
                    return true;
                }).Show(true);
            } else if (info.IsKeyPressed(Keys.U)) {
                //World.AddEntity(new ExplosionSource(World, World.player.Position, 10));
                //Use menu
                new ListMenu<IItem>(Width, Height, "Select items to use. Press ESC to finish.", World.player.Inventory.Select(Item => new ListItem(Item)), item => {

                    if (item.Grenade != null && !item.Grenade.Armed) {
                        item.Grenade.Arm();
                        player.Witness(new InfoEvent(new ColoredString("You arm: ") + item.Name.WithBackground(Color.Black)));
                    }
                    return false;
                }).Show(true);
            } else if (info.IsKeyPressed(Keys.G)) {
                new ListMenu<IItem>(Width, Height, "Select items to get. Press ESC to finish.", World.entities[World.player.Position].OfType<IItem>().Select(Item => new ListItem(Item)), item => {
                    //Just take the item for now
                    World.player.Inventory.Add(item);
                    World.entities.Remove(item);

                    World.player.Witness(new InfoEvent(new ColoredString("You get: ") + item.Name.WithBackground(Color.Black)));
                    return true;
                }).Show(true);
            } else if (info.IsKeyPressed(Keys.I)) {
                new ListMenu<IItem>(Width, Height, "Select inventory items to examine. Press ESC to finish.", World.player.Inventory.Select(Item => new ListItem(Item)), item => {
                    //	Later, we might have a chance of identifying the item upon selecting it in the inventory

                    //World.player.Witness(new SelfEvent(new ColoredString("You examine: ") + item.Name.WithBackground(Color.Black)));
                    return false;
                }).Show(true);
            } else if (info.IsKeyPressed(Keys.L)) {
                new LookMenu(Width, Height, World).Show(true);
            } else if (info.IsKeyPressed(Keys.S)) {
                //TODO: Ask the player if they want to cancel their current ShootAction

                new ShootMenu(Width, Height, World, World.player).Show(true);
            } else if (info.IsKeyPressed(Keys.T)) {
                new ThrowMenu(Width, Height, World, World.player).Show(true);
            } else if (info.IsKeyDown(Keys.OemPeriod) && info.IsKeyDown(Keys.RightControl)) {
                if (!World.player.Actions.Any(a => a is WaitAction)) {
                    World.player.Actions.Add(new WaitAction(1));
                }
            } else if (info.IsKeyPressed(Keys.OemPeriod)) {
                if (!World.player.Actions.Any(a => a is WaitAction)) {
                    Debug.Print("waiting");
                    World.player.Actions.Add(new WaitAction(Constants.STEPS_PER_SECOND));
                    World.player.Witness(new InfoEvent(new ColoredString("You wait")));
                }
            }
            return base.ProcessKeyboard(info);
        }
    }
	interface GameMenu {
		bool Done { get; }
		void Update(TimeSpan delta);
		void Draw(TimeSpan delta);
		
		void ProcessKeyboard(SadConsole.Input.Keyboard info);
	}
	static class Help {
		public static void Transparent(this SadConsole.Console c) {
			c.DefaultBackground = Color.Transparent;
			c.DefaultForeground = Color.Transparent;
			c.Clear();
		}
	}
}
