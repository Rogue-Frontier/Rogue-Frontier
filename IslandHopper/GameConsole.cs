﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Input;
using SadConsole.Surfaces;
using SadConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IslandHopper.Constants;

namespace IslandHopper {
	
	class World {
		public Random karma;
		public Space<Entity> entities { get; set; }     //	3D entity grid used for collision detection
		public ArraySpace<Voxel> voxels { get; set; }   //	3D voxel grid used for collision detection
		public Point3 camera { get; set; }              //	Point3 representing the location of the center of the screen
		public Player player { get; set; }              //	Player object that controls the game
	}
	static class Debugging {
		public static void Info(this object o, params string[] message) {
			System.Console.Write(o.GetType().Name + ":");
			WriteLine(message);
		}
		public static void WriteLine(params string[] s) {
			s.ToList().ForEach(str => System.Console.Write(str));
			System.Console.WriteLine();
		}
	}

	class GameConsole : Window {
		World World;
		public GameConsole(int Width, int Height) : base(Width, Height) {
			UseKeyboard = true;
			UseMouse = true;
			Theme.ModalTint = Color.Transparent;
			System.Console.WriteLine("Width: " + Width);
			System.Console.WriteLine("Height: " + Height);
			World = new World() {
				karma = new Random(0),
				entities = new Space<Entity>(100, 100, 30, e => e.Position),
				voxels = new ArraySpace<Voxel>(100, 100, 30, new Air()),
				camera = new Point3(0, 0, 0)
			};
			World.player = new Player(World, new Point3(80, 80, 20));

			for (int x = 0; x < World.voxels.Width; x++) {
				for(int y = 0; y < World.voxels.Height; y++) {
					World.voxels[new Point3(x, y, 0)] = new Grass();
				}
			}

			for(int i = 5; i < 300; i++) {
				World.entities.Place(new Gun1(World, new Point3(78, 78, 1 + i/30)));
			}
			
			World.entities.Place(World.player);
		}
		public override void Show(bool modal) {
			base.Show(modal);
			new PlayerMain(Width, Height, World) {
				IsFocused = true
			}.Show(true);
		}
		public override void Update(TimeSpan delta) {
		}
		private int HalfWidth { get => Width / 2; }
		private int HalfHeight { get => Height / 2; }
		public override void Draw(TimeSpan delta) {
			Clear();
			for(int x = -HalfWidth; x < HalfWidth; x++) {
				for(int y = -HalfHeight; y < HalfHeight; y++) {
					Point3 location = World.camera + new Point3(x, y, 0);
					ColoredString s = new ColoredString(" ", new Cell(Color.Transparent, Color.Transparent));
					if (World.entities.InBounds(location) && World.entities.Try(location).Count > 0) {
						s = World.entities[location].ToList()[0].SymbolCenter;
					} else if (World.voxels.InBounds(location) && !(World.voxels[location] is Air)) {
						s = World.voxels[location].GetCharCenter();
					} else {
						location = location + new Point3(0, 0, -1);
						if (World.voxels.InBounds(location)) {
							s = World.voxels[location].GetCharAbove();
						}
					}
					Print(x + HalfWidth, y + HalfHeight, s);
				}
			}
			Print(1, 1, "" + World.player.Position.z, Color.White);
			Print(1, 2, "" + World.camera.z, Color.White);
			base.Draw(delta);
		}
		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
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
			c.TextSurface.DefaultBackground = Color.Transparent;
			c.TextSurface.DefaultForeground = Color.Transparent;
			c.Clear();
		}
	}
	class PlayerMain : Window {
		World World;
		public PlayerMain(int Width, int Height, World world) : base(Width, Height) {
			UseKeyboard = true;
			UseMouse = true;
			this.World = world;
			this.Transparent();
		}
		public override void Update(TimeSpan delta) {
			base.Update(delta);

			World.entities.UpdateSpace();       //	Update all entity positions on the grid
			World.entities.all.ToList().ForEach(e => e.UpdateRealtime());

			if (World.player.AllowUpdate()) {
				World.entities.all.ToList().ForEach(e => {
					e.UpdateStep();
				});
				World.camera = World.player.Position;
			} else {
				//System.Console.WriteLine("not updating");
			}

			World.entities.all.RemoveWhere(e => !e.Active);
		}
		public override void Draw(TimeSpan delta) {
			base.Draw(delta);
		}
		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {

			if (info.IsKeyPressed(Keys.Up)) {
				if (info.IsKeyDown(Keys.RightShift)) {
					if (World.player.OnGround()) ;
					World.player.Actions.Add(new Impulse(World.player, new Point3(0, 0, 2)));
				} else {
					World.player.Actions.Add(new WalkAction(World.player, new Point3(0, -1)));
				}
			} else if (info.IsKeyPressed(Keys.Down)) {
				if (info.IsKeyDown(Keys.RightShift)) {
					if (World.player.OnGround())
						World.player.Actions.Add(new Impulse(World.player, new Point3(0, 0, -2)));
				} else {
					World.player.Actions.Add(new WalkAction(World.player, new Point3(0, 1)));
				}
			} else if (info.IsKeyPressed(Keys.Left)) {
				World.player.Actions.Add(new WalkAction(World.player, new Point3(-1, 0)));
			} else if (info.IsKeyPressed(Keys.Right)) {
				World.player.Actions.Add(new WalkAction(World.player, new Point3(1, 0)));
			} else if (info.IsKeyPressed(Keys.D)) {
				new ListMenu<IItem>(Width, Height, "Select inventory items to drop. Press ESC to finish.", World.player.inventory.OfType<IItem>().Select(Item => new ListItem(Item)), item => {
					//Just drop the item for now
					World.player.inventory.Remove(item);
					World.entities.Place(item);
					return true;
				}).Show(true);
			} else if (info.IsKeyPressed(Keys.G)) {
				new ListMenu<IItem>(Width, Height, "Select items to get. Press ESC to finish.", World.entities[World.player.Position].OfType<IItem>().Select(Item => new ListItem(Item)), item => {
					//Just take the item for now
					World.player.inventory.Add(item);
					World.entities.Remove(item);
					return true;
				}).Show(true);
			} else if (info.IsKeyPressed(Keys.I)) {
				new ListMenu<IItem>(Width, Height, "Select inventory items to examine. Press ESC to finish.", World.player.inventory.OfType<IItem>().Select(Item => new ListItem(Item)), item => {
					//	Later, we might have a chance of identifying the item upon selecting it in the inventory
					return false;
				}).Show(true);
			} else if(info.IsKeyPressed(Keys.L)) {
				new LookMenu(Width, Height, World).Show(true);
			} else if (info.IsKeyPressed(Keys.OemPeriod)) {
				Debug.Print("waiting");
				World.player.Actions.Add(new WaitAction(STEPS_PER_SECOND));
			}
			return base.ProcessKeyboard(info);
		}
	}
	interface ListChoice<T> {
		T Value { get; }
		ColoredString GetSymbolCenter();
		ColoredString GetName();
	}
	class ListItem : ListChoice<IItem> {
		public IItem Value { get; }
		public ListItem(IItem Value) {
			this.Value = Value;
		}
		public ColoredString GetSymbolCenter() => Value.SymbolCenter;
		public ColoredString GetName() => Value.Name;
	}
	class ListEntity : ListChoice<Entity> {
		public Entity Value { get; }
		public ListEntity(Entity Value) {
			this.Value = Value;
		}
		public ColoredString GetSymbolCenter() => Value.SymbolCenter;
		public ColoredString GetName() => Value.Name;
	}
	class ListMenu<T> : Window {
		string hint;
		HashSet<ListChoice<T>> Choices;
		Func<T, bool> select;		//Fires when we select an item. If true, then we remove the item from the selections
		int startIndex;
		public static ListMenu<IItem> itemSelector(int Width, int Height, string hint, IEnumerable<IItem> Items, Func<IItem, bool> select) {
			return new ListMenu<IItem>(Width, Height, hint, Items.Select(item => new ListItem(item)), select);
		}
		public ListMenu(int Width, int Height, string hint, IEnumerable<ListChoice<T>> Choices, Func<T, bool> select) : base(Width, Height) {
			this.hint = hint;
			this.Choices = new HashSet<ListChoice<T>>(Choices);
			this.select = select;
			startIndex = 0;

			this.Transparent();
			Theme.ModalTint = Color.Transparent;
		}
		public override void Update(TimeSpan delta) {
			base.Update(delta);
		}
		public override void Draw(TimeSpan delta) {
			int x = 5;
			int y = 5;
			Print(x, y, hint, Color.White, Color.Black);
			y++;
			if (Choices.Count > 0) {
				string UP = ((char)24).ToString();
				string LEFT = ((char)27).ToString();
				Print(x, y, "    ", background: Color.Black);
				if (CanScrollUp) {
					Print(x, y, UP, Color.White, Color.Black);
					if (CanPageUp)
						Print(x + 2, y, LEFT, Color.White, Color.Black);
					Print(x + 4, y, startIndex.ToString(), Color.White, Color.Black);
				} else {
					Print(x, y, "-", Color.White, Color.Black);
				}
				y++;

				List<ListChoice<T>> list = Choices.ToList();
				for (int i = startIndex; i < startIndex + 26; i++) {
					if(i < Choices.Count) {
						char binding = (char)('a' + (i - startIndex));
						Print(x, y, "" + binding, Color.LimeGreen, Color.Transparent);
						Print(x + 1, y, " ", Color.Black, Color.Black);
						Print(x + 2, y, list[i].GetSymbolCenter());
						Print(x + 3, y, " ", Color.Black, Color.Black);
						Print(x + 4, y, list[i].GetName());
					} else {
						Print(x, y, ".", Color.Gray, Color.Black);
					}
					y++;
				}

				string DOWN = ((char)25).ToString();
				string RIGHT = ((char)26).ToString();
				Print(x, y, "    ", background: Color.Black);
				if (CanScrollDown) {
					Print(x, y, DOWN, Color.White, Color.Black);
					if (CanPageDown)
						Print(x + 2, y, RIGHT, Color.White, Color.Black);
					Print(x + 4, y, ((Choices.Count - 26) - startIndex).ToString(), Color.White, Color.Black);
				} else {
					Print(x, y, "-", Color.White, Color.Black);
				}
				
				y++;
			} else {
				Print(x, y, "There is nothing here.", Color.Red, Color.Black);
			}

			base.Draw(delta);
		}
		private bool CanScrollUp => startIndex > 0;
		private bool CanPageUp => startIndex - 25 > 0;
		private bool CanScrollDown => startIndex + 26 < Choices.Count;
		private bool CanPageDown => startIndex + 26 + 25 < Choices.Count;
		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
			if (info.IsKeyPressed(Keys.Escape)) {
				Hide();
			} else {
				ListControls(info);
			}
			return true;
		}
		public void ListControls(SadConsole.Input.Keyboard info) {
			if (info.IsKeyPressed(Keys.Up)) {
				if (CanScrollUp)
					startIndex--;
			} else if (info.IsKeyPressed(Keys.Down)) {
				if (CanScrollDown)
					startIndex++;
			} else if (info.IsKeyPressed(Keys.Left)) {
				if (CanPageUp)
					startIndex -= 26;
				else
					startIndex = 0;
			} else if (info.IsKeyPressed(Keys.Right)) {
				if (CanPageDown)
					startIndex += 26;
				else
					startIndex = Choices.Count - 26;
			} else {
				//If this key represents an item, then we select it
				foreach (var k in info.KeysPressed) {
					var key = k.Key;
					if (Keys.A <= key && key <= Keys.Z) {
						//A represents the first displayed item (i.e. the one at startIndex). Z represents the last displayed item (startIndex + 25)
						int index = (key - Keys.A) + startIndex;
						if (index < Choices.Count) {
							//Select the item
							ListChoice<T> selected = Choices.ToList()[index];
							if (select.Invoke(selected.Value)) {
								Choices.Remove(selected);

								//If we're at the bottom of the menu and we're removing an item here, move the list view up so that we don't have empty slots
								if (Choices.Count > 25 && !CanPageDown) {
									startIndex = Choices.Count - 26;
								}
							}

						}
						break;
					}
				}
			}
		}
	}
	class LookMenu : Window {
		World world;

		string hint;
		Func<Entity, bool> select;

		Timer cursorBlink;
		bool cursorVisible;

		ListMenu<Entity> examineMenu;

		readonly ColoredString cursor = new ColoredString("?", Color.Yellow, Color.Black);
		public LookMenu(int Width, int Height, World world) : base(Width, Height) {

			this.world = world;
			this.hint = "Select an entity to examine";
			this.select = e => false;

			this.Transparent();
			Theme.ModalTint = Color.Transparent;

			cursorBlink = new Timer(0.4, () => {
				cursorVisible = !cursorVisible;
			});
			UpdateExamine();
		}
		public LookMenu(int width, int height, World world, string hint, Func<Entity, bool> select) : base(width, height) {
			this.world = world;
			this.hint = hint;
			this.select = select;
			cursorBlink = new Timer(0.4, () => {
				cursorVisible = !cursorVisible;
			});
			UpdateExamine();
		}

		public override void Draw(TimeSpan delta) {
			this.Clear();
			if(cursorVisible) {
				Print(Width / 2, Height / 2, cursor);
			}
			base.Draw(delta);
		}

		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
			const int delta = 1;	//Distance moved by camera
			if(info.IsKeyDown(Keys.RightControl)) {
				examineMenu.ListControls(info);
			} else if (info.IsKeyPressed(Keys.Up)) {
				if (info.IsKeyDown(Keys.RightShift)) {
					world.camera += new Point3(0, 0, delta);
				} else {
					world.camera += new Point3(0, -delta);
				}
				UpdateExamine();
			} else if (info.IsKeyPressed(Keys.Down)) {
				if (info.IsKeyDown(Keys.RightShift)) {
					world.camera += new Point3(0, 0, -delta);
				} else {
					world.camera += new Point3(0, delta);
				}
				UpdateExamine();
			} else if (info.IsKeyPressed(Keys.Left)) {
				world.camera += new Point3(-delta, 0);
				UpdateExamine();
			} else if (info.IsKeyPressed(Keys.Right)) {
				world.camera += new Point3(delta, 0);
				UpdateExamine();
			} else if (info.IsKeyPressed(Keys.Escape)) {
				world.camera = world.player.Position;
				Hide();
			} else {
				examineMenu.ListControls(info);
			}
			return true;
		}

		public override void Update(TimeSpan delta) {
			examineMenu.Update(delta);
			cursorBlink.Update(delta.TotalSeconds);
			base.Update(delta);
		}

		public void UpdateExamine() {
			examineMenu?.Hide();
			examineMenu = new ListMenu<Entity>(Width, Height, hint, world.entities[world.camera].Select(e => new ListEntity(e)), select) {
				IsVisible = true
			};
			examineMenu.Show();
		}
	}
}
