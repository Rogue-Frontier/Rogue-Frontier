using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
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
	class GameConsole : Window {
		World World;
		public GameConsole(int Width, int Height) : base(Width, Height) {
			Theme = Themes.Main;
			UseKeyboard = true;
			UseMouse = true;
			this.Info($"Width: {Width}", $"Height: {Height}");
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

			for(int i = 0; i < 1; i++) {
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
					ColoredGlyph c = new ColoredString(" ", new Cell(Color.Transparent, Color.Transparent))[0];
					if (World.entities.InBounds(location) && World.entities.Try(location).Count > 0) {
						c = World.entities[location].ToList()[0].SymbolCenter;
					} else if (World.voxels.InBounds(location) && !(World.voxels[location] is Air)) {
						c = World.voxels[location].CharCenter;
					} else {
						location = location + new Point3(0, 0, -1);
						if (World.voxels.InBounds(location)) {
							c = World.voxels[location].CharAbove;
						}
					}
					Print(x + HalfWidth, y + HalfHeight, c.ToColoredString());
				}
			}
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
			c.DefaultBackground = Color.Transparent;
			c.DefaultForeground = Color.Transparent;
			c.Clear();
		}
	}
	class PlayerMain : Window {
		World World;
		public PlayerMain(int Width, int Height, World world) : base(Width, Height) {
			Theme = Themes.Sub;
			UseKeyboard = true;
			UseMouse = true;
			this.World = world;
			this.Transparent();
		}
		public override void Update(TimeSpan delta) {
			base.Update(delta);

			World.entities.UpdateSpace();       //	Update all entity positions on the grid
			foreach(var e in World.entities.all) {
				e.UpdateRealtime();
			}

			if (World.player.AllowUpdate()) {
				this.Info("Global Update");
				foreach(var e in World.entities.all) {
					e.Info("UpdateStep() by world");
					e.UpdateStep();
				}
				World.camera = World.player.Position;
			} else {
				//System.Console.WriteLine("not updating");
			}

			var Removed = new List<Entity>();
			World.entities.all.RemoveWhere(e => {
				bool result = !e.Active;
				if(result) {
					Removed.Add(e);
				}
				return result;
			});
			Removed.ForEach(e => e.OnRemoved());
		}
		public override void Draw(TimeSpan delta) {
			Clear();
			Print(1, 1, "" + World.player.Position.z, Color.White);
			Print(1, 2, "" + World.camera.z, Color.White);

			for(int i = 0; i < 30 && i < World.player.HistoryRecent.Count(); i++) {
				var entry = World.player.HistoryRecent[i];
				Print(1, (Height - 1) - i, entry.ScreenTime > 30 ? entry.Desc : entry.Desc.Brighten(-255 + 255 * entry.ScreenTime / 30));
			}
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
				new ListMenu<IItem>(Width, Height, "Select inventory items to drop. Press ESC to finish.", World.player.Inventory.Select(Item => new ListItem(Item)), item => {
					//Just drop the item for now
					World.player.Inventory.Remove(item);
					World.entities.Place(item);

					World.player.Witness(new SelfEvent(new ColoredString("You drop: ") + item.Name.WithBackground(Color.Black)));
					return true;
				}).Show(true);
			} else if (info.IsKeyPressed(Keys.G)) {
				new ListMenu<IItem>(Width, Height, "Select items to get. Press ESC to finish.", World.entities[World.player.Position].OfType<IItem>().Select(Item => new ListItem(Item)), item => {
					//Just take the item for now
					World.player.Inventory.Add(item);
					World.entities.Remove(item);

					World.player.Witness(new SelfEvent(new ColoredString("You get: ") + item.Name.WithBackground(Color.Black)));
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
			} else if (info.IsKeyPressed(Keys.T)) {
				new ThrowMenu(Width, Height, World, World.player).Show(true);
			} else if (info.IsKeyPressed(Keys.OemPeriod)) {
				Debug.Print("waiting");
				World.player.Actions.Add(new WaitAction(STEPS_PER_SECOND));
				World.player.Witness(new SelfEvent(new ColoredString("You wait")));
			}
			return base.ProcessKeyboard(info);
		}
	}
	interface ListChoice<T> {
		T Value { get; }
		ColoredGlyph GetSymbolCenter();
		ColoredString GetName();
	}
	class ListItem : ListChoice<IItem> {
		public IItem Value { get; }
		public ListItem(IItem Value) {
			this.Value = Value;
		}
		public ColoredGlyph GetSymbolCenter() => Value.SymbolCenter;
		public ColoredString GetName() => Value.Name;
	}
	class ListEntity : ListChoice<Entity> {
		public Entity Value { get; }
		public ListEntity(Entity Value) {
			this.Value = Value;
		}
		public ColoredGlyph GetSymbolCenter() => Value.SymbolCenter;
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
			Theme = Themes.Sub;
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
			this.Clear();
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
						Print(x + 2, y, list[i].GetSymbolCenter().ToColoredString());
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
					startIndex = Math.Max(0, Choices.Count - 26);
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
	class ThrowMenu : Window {
		World w;
		Player p;
		ListMenu<IItem> itemSelector;
		LookMenu targetSelector;
		public ThrowMenu(int width, int height, World w, Player p) : base(width, height) {
			Theme = Themes.Sub;

			this.w = w;
			this.p = p;

			this.Transparent();
			Theme.ModalTint = Color.Transparent;
		}
		public override void Update(TimeSpan time) {
			base.Update(time);
			if(itemSelector == null) {
				UpdateItemSelector();
			}
			if (targetSelector != null) {
				targetSelector.Update(time);
			} else {
				itemSelector.Update(time);
			}
		}
		public override void Draw(TimeSpan drawTime) {
			this.Clear();
			base.Draw(drawTime);
			if(targetSelector != null) {
				targetSelector.Draw(drawTime);
			} else {
				itemSelector.Draw(drawTime);
			}
		}
		public void UpdateItemSelector() {
			Hide();
			itemSelector = new ListMenu<IItem>(Width, Height, "Select item to throw. ESC to cancel.", p.Inventory.Select(Item => new ListItem(Item)), item => {
				itemSelector.Hide();
				targetSelector = new LookMenu(Width, Height, w, "Select target to throw item at. ESC to cancel.", target => {
					targetSelector.Hide();

					ThrowItem(target, item);
					return false;
				});
				targetSelector.Show(true);
				return false;
			});
			itemSelector.Show(true);
		}
		public void ThrowItem(Entity target, IItem item) {
			if(Helper.CalcAim2(target.Position - p.Position, 30, out Point3 lower, out Point3 _)) {
				item.Velocity = lower / STEPS_PER_SECOND;
				//Remove the item from the player's inventory and create a thrown item in the world
				p.Inventory.Remove(item);
				w.AddEntity(new ThrownItem(p, item));
				p.Witness(new SelfEvent(new ColoredString("You throw: ") + item.Name.WithBackground(Color.Black)));
			}
		}
		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
			if(info.IsKeyDown(Keys.Escape)) {
				if (targetSelector != null) {
					targetSelector.Hide();
					targetSelector = null;
					UpdateItemSelector();
				} else {
					itemSelector.Hide();
				}
				return true;
			} else {
				if(targetSelector != null) {
					return targetSelector.ProcessKeyboard(info);
				} else {
					return itemSelector.ProcessKeyboard(info);
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
			Theme = Themes.Sub;

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

			this.Transparent();
			Theme.ModalTint = Color.Transparent;
			UpdateExamine();
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
		public override void Draw(TimeSpan delta) {
			examineMenu?.Draw(delta);
			if (cursorVisible) {
				Print(Width / 2, Height / 2, cursor);
			}
			base.Draw(delta);
		}
		public override void Update(TimeSpan delta) {
			examineMenu?.Update(delta);
			cursorBlink.Update(delta.TotalSeconds);
			base.Update(delta);
		}
		public override void Hide() {
			base.Hide();
			examineMenu?.Hide();
		}
		public void UpdateExamine() {
			examineMenu?.Hide();
			var ent = world.entities[world.camera];
			if (ent != null) {
				examineMenu = new ListMenu<Entity>(Width, Height, hint, ent.Select(e => new ListEntity(e)), select) {
					IsVisible = true
				};
			}
		}
	}
}
