using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IslandHopper.Constants;

namespace IslandHopper {
	
	class GameConsole : SadConsole.Console {
		public Space<Entity> entities { get; private set; }		//	3D entity grid used for collision detection
		public ArraySpace<Voxel> voxels { get; private set; }	//	3D voxel grid used for collision detection
		public Point3 camera { get; set; }				//	Point3 representing the location of the center of the screen
		public Player player { get; private set; }              //	Player object that controls the game
		public Stack<GameMenu> controller;
		public GameConsole(int Width, int Height) : base(Width, Height) {
			UseKeyboard = true;
			UseMouse = true;
			System.Console.WriteLine("Width: " + Width);
			System.Console.WriteLine("Height: " + Height);
			entities = new Space<Entity>(100, 100, 30, e => e.Position);
			voxels = new ArraySpace<Voxel>(100, 100, 30);
			for(int x = 0; x < voxels.Width; x++) {
				for(int y = 0; y < voxels.Height; y++) {
					for(int z = 0; z < voxels.Depth; z++) {
						voxels[new Point3(x, y, z)] = new Air();
					}
				}
			}

			for(int x = 0; x < voxels.Width; x++) {
				for(int y = 0; y < voxels.Height; y++) {
					voxels[new Point3(x, y, 0)] = new Grass();
				}
			}
			camera = new Point3(0, 0, 0);
			player = new Player(this, new Point3(80, 80, 20));

			for(int i = 5; i < 300; i++) {
				entities.Place(new Gun1(this, new Point3(78, 78, 1 + i/30)));
			}
			
			entities.Place(player);

			controller = new Stack<GameMenu>();
			controller.Push(new PlayerMain(this, player));
		}
		public override void Update(TimeSpan delta) {
			base.Update(delta);

			entities.UpdateSpace();		//	Update all entity positions on the grid
			entities.all.ToList().ForEach(e => e.UpdateRealtime());

			if(player.AllowUpdate()) {
				entities.all.ToList().ForEach(e => {
					e.UpdateStep();
				});
				camera = player.Position;
			} else {
				//System.Console.WriteLine("not updating");
			}

			entities.all.RemoveWhere(e => !e.IsActive());
			controller.Peek().Update(delta);
			while (controller.Peek().Done)
				controller.Pop();
		}
		private int HalfWidth { get => Width / 2; }
		private int HalfHeight { get => Height / 2; }
		public override void Draw(TimeSpan delta) {
			Clear();
			for(int x = -HalfWidth; x < HalfWidth; x++) {
				for(int y = -HalfHeight; y < HalfHeight; y++) {
					Point3 location = camera + new Point3(x, y, 0);
					ColoredString s = new ColoredString(" ", new Cell(Color.Transparent, Color.Transparent));
					if (entities.InBounds(location) && entities.Try(location).Count > 0) {
						s = entities[location].ToList()[0].GetSymbolCenter();
					} else if (voxels.InBounds(location) && !(voxels[location] is Air)) {
						s = voxels[location].GetCharCenter();
					} else {
						location = location + new Point3(0, 0, -1);
						if (voxels.InBounds(location)) {
							s = voxels[location].GetCharAbove();
						}
					}
					Print(x + HalfWidth, y + HalfHeight, s);
				}
			}
			Print(1, 1, "" + player.Position.z, Color.White);
			Print(1, 2, "" + camera.z, Color.White);
			controller.Peek().Draw(delta);
			base.Draw(delta);
		}
		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
			Debug.Print(info.IsKeyDown(Keys.RightShift), "shift");
			Debug.Print(info.IsKeyPressed(Keys.Up), "up_pressed");

			//For now, we don't let the player stack actions (i.e. they must be idle)
			if(!player.AllowUpdate()) {
				controller.Peek().ProcessKeyboard(info);
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
	class PlayerMain : GameMenu {
		GameConsole Console;
		Player player;
		public PlayerMain(GameConsole Console, Player player) {
			this.Console = Console;
			this.player = player;
		}
		public void Update(TimeSpan delta) {

		}
		public void Draw(TimeSpan delta) {
			
		}
		public bool Done => false;
		public void ProcessKeyboard(SadConsole.Input.Keyboard info) {
			if (info.IsKeyPressed(Keys.Up)) {
				if (info.IsKeyDown(Keys.RightShift)) {
					if (player.OnGround()) ;
					player.Actions.Add(new Impulse(player, new Point3(0, 0, 2)));
				} else {
					player.Actions.Add(new WalkAction(player, new Point3(0, -1)));
				}
			} else if (info.IsKeyPressed(Keys.Down)) {
				if (info.IsKeyDown(Keys.RightShift)) {
					if (player.OnGround())
						player.Actions.Add(new Impulse(player, new Point3(0, 0, -2)));
				} else {
					player.Actions.Add(new WalkAction(player, new Point3(0, 1)));
				}
			} else if (info.IsKeyPressed(Keys.Left)) {
				player.Actions.Add(new WalkAction(player, new Point3(-1, 0)));
			} else if (info.IsKeyPressed(Keys.Right)) {
				player.Actions.Add(new WalkAction(player, new Point3(1, 0)));
			} else if (info.IsKeyPressed(Keys.D)) {
				Console.controller.Push(new ListMenu<Item>(Console, "Select inventory items to drop. Press ESC to finish.", player.inventory.OfType<Item>().Select(Item => new ListItem(Item)), item => {
					//Just drop the item for now
					player.inventory.Remove(item);
					Console.entities.Place(item);
					return true;
				}));
			} else if (info.IsKeyPressed(Keys.G)) {
				Console.controller.Push(new ListMenu<Item>(Console, "Select items to get. Press ESC to finish.", Console.entities[player.Position].OfType<Item>().Select(Item => new ListItem(Item)), item => {
					//Just take the item for now
					player.inventory.Add(item);
					Console.entities.Remove(item);
					return true;
				}));
			} else if (info.IsKeyPressed(Keys.I)) {
				Console.controller.Push(new ListMenu<Item>(Console, "Select inventory items to examine. Press ESC to finish.", player.inventory.OfType<Item>().Select(Item => new ListItem(Item)), item => {
					//	Later, we might have a chance of identifying the item upon selecting it in the inventory
					return false;
				}));
			} else if(info.IsKeyPressed(Keys.L)) {
				Console.controller.Push(new LookMenu(Console, player));
			} else if (info.IsKeyPressed(Keys.OemPeriod)) {
				Debug.Print("waiting");
				player.Actions.Add(new WaitAction(STEPS_PER_SECOND));
			}
		}
	}
	interface ListChoice<T> {
		T Value { get; }
		ColoredString GetSymbolCenter();
		ColoredString GetName();
	}
	class ListItem : ListChoice<Item> {
		public Item Value { get; }
		public ListItem(Item Value) {
			this.Value = Value;
		}
		public ColoredString GetSymbolCenter() => Value.GetSymbolCenter();
		public ColoredString GetName() => Value.GetName();
	}
	class ListEntity : ListChoice<Entity> {
		public Entity Value { get; }
		public ListEntity(Entity Value) {
			this.Value = Value;
		}
		public ColoredString GetSymbolCenter() => Value.GetSymbolCenter();
		public ColoredString GetName() => Value.GetName();
	}
	class ListMenu<T> : GameMenu {
		SadConsole.Console Console;
		string hint;
		HashSet<ListChoice<T>> Choices;
		Func<T, bool> select;		//Fires when we select an item. If true, then we remove the item from the selections
		public bool Done { get; private set; }
		int startIndex;
		public static ListMenu<Item> itemSelector(SadConsole.Console Console, string hint, IEnumerable<Item> Items, Func<Item, bool> select) {
			return new ListMenu<Item>(Console, hint, Items.Select(item => new ListItem(item)), select);
		}
		public ListMenu(SadConsole.Console Console, string hint, IEnumerable<ListChoice<T>> Choices, Func<T, bool> select) {
			this.Console = Console;
			this.hint = hint;
			this.Choices = new HashSet<ListChoice<T>>(Choices);
			this.select = select;
			Done = false;
			startIndex = 0;
		}
		public void Update(TimeSpan delta) {

		}
		public void Draw(TimeSpan delta) {
			int x = 5;
			int y = 5;
			Console.Print(x, y, hint, Color.White, Color.Black);
			y++;
			if (Choices.Count > 0) {
				string UP = ((char)24).ToString();
				string LEFT = ((char)27).ToString();
				Console.Print(x, y, "    ", background: Color.Black);
				if (CanScrollUp) {
					Console.Print(x, y, UP, Color.White, Color.Black);
					if (CanPageUp)
						Console.Print(x + 2, y, LEFT, Color.White, Color.Black);
					Console.Print(x + 4, y, startIndex.ToString(), Color.White, Color.Black);
				} else {
					Console.Print(x, y, "-", Color.White, Color.Black);
				}
				y++;

				List<ListChoice<T>> list = Choices.ToList();
				for (int i = startIndex; i < startIndex + 26; i++) {
					if(i < Choices.Count) {
						char binding = (char)('a' + (i - startIndex));
						Console.Print(x, y, "" + binding, Color.LimeGreen, Color.Transparent);
						Console.Print(x + 1, y, " ", Color.Black, Color.Black);
						Console.Print(x + 2, y, list[i].GetSymbolCenter());
						Console.Print(x + 3, y, " ", Color.Black, Color.Black);
						Console.Print(x + 4, y, list[i].GetName());
					} else {
						Console.Print(x, y, ".", Color.Gray, Color.Black);
					}
					y++;
				}

				string DOWN = ((char)25).ToString();
				string RIGHT = ((char)26).ToString();
				Console.Print(x, y, "    ", background: Color.Black);
				if (CanScrollDown) {
					Console.Print(x, y, DOWN, Color.White, Color.Black);
					if (CanPageDown)
						Console.Print(x + 2, y, RIGHT, Color.White, Color.Black);
					Console.Print(x + 4, y, ((Choices.Count - 26) - startIndex).ToString(), Color.White, Color.Black);
				} else {
					Console.Print(x, y, "-", Color.White, Color.Black);
				}
				
				y++;
			} else {
				Console.Print(x, y, "There is nothing here.", Color.Red, Color.Black);
			}
		}
		private bool CanScrollUp => startIndex > 0;
		private bool CanPageUp => startIndex - 25 > 0;
		private bool CanScrollDown => startIndex + 26 < Choices.Count;
		private bool CanPageDown => startIndex + 26 + 25 < Choices.Count;
		public void ProcessKeyboard(SadConsole.Input.Keyboard info) {
			if (info.IsKeyPressed(Keys.Escape)) {
				Done = true;
			} else {
				ListControls(info);
			}
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
	class LookMenu : GameMenu {
		GameConsole console;
		Player player;
		string hint;
		Func<Entity, bool> select;

		public bool Done { get; private set; }
		Timer cursorBlink;
		bool cursorVisible;

		ListMenu<Entity> examineMenu;

		readonly ColoredString cursor = new ColoredString("?", Color.Yellow, Color.Black);
		public LookMenu(GameConsole console, Player player) {
			this.console = console;
			this.player = player;
			this.hint = "Select an entity to examine";
			this.select = e => false;

			Done = false;
			cursorBlink = new Timer(0.4, () => {
				cursorVisible = !cursorVisible;
			});
			UpdateExamine();
		}
		public LookMenu(GameConsole console, Player player, string hint, Func<Entity, bool> select) {
			this.console = console;
			this.player = player;
			this.hint = hint;
			this.select = select;
			Done = false;
			cursorBlink = new Timer(0.4, () => {
				cursorVisible = !cursorVisible;
			});
			UpdateExamine();
		}

		public void Draw(TimeSpan delta) {
			if(cursorVisible) {
				console.Print(console.Width / 2, console.Height / 2, cursor);
			}
			examineMenu.Draw(delta);
		}

		public void ProcessKeyboard(SadConsole.Input.Keyboard info) {
			const int delta = 1;	//Distance moved by camera
			if(info.IsKeyDown(Keys.RightControl)) {
				examineMenu.ListControls(info);
			} else if (info.IsKeyPressed(Keys.Up)) {
				if (info.IsKeyDown(Keys.RightShift)) {
					console.camera += new Point3(0, 0, delta);
				} else {
					console.camera += new Point3(0, -delta);
				}
				UpdateExamine();
			} else if (info.IsKeyPressed(Keys.Down)) {
				if (info.IsKeyDown(Keys.RightShift)) {
					console.camera += new Point3(0, 0, -delta);
				} else {
					console.camera += new Point3(0, delta);
				}
				UpdateExamine();
			} else if (info.IsKeyPressed(Keys.Left)) {
				console.camera += new Point3(-delta, 0);
				UpdateExamine();
			} else if (info.IsKeyPressed(Keys.Right)) {
				console.camera += new Point3(delta, 0);
				UpdateExamine();
			} else if (info.IsKeyPressed(Keys.Escape)) {
				console.camera = player.Position;
				Done = true;
			}
		}

		public void Update(TimeSpan delta) {
			examineMenu.Update(delta);
			cursorBlink.Update(delta.TotalSeconds);
		}

		public void UpdateExamine() => examineMenu = new ListMenu<Entity>(console, hint, console.entities[console.camera].Select(e => new ListEntity(e)), select);
	}
}
