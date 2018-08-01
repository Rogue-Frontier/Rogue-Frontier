using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	
	class GameConsole : SadConsole.Console {
		public Space<Entity> entities { get; private set; }		//	3D entity grid used for collision detection
		public ArraySpace<Voxel> voxels { get; private set; }	//	3D voxel grid used for collision detection
		public Point3 camera { get; private set; }				//	Point3 representing the location of the center of the screen
		public Player player { get; private set; }              //	Player object that controls the game
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
			entities.Place(player);
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
		}
		private int HalfWidth { get => Width / 2; }
		private int HalfHeight { get => Height / 2; }
		public override void Draw(TimeSpan delta) {
			Clear();
			for(int x = -HalfWidth; x < HalfWidth; x++) {
				for(int y = -HalfHeight; y < HalfHeight; y++) {
					Point3 location = camera + new Point3(x, y, 0);
					ColoredString s = new ColoredString(" ", new Cell(Color.Transparent, Color.Transparent));
					if (entities.InBounds(location) && entities.Get(location).Count > 0) {
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
			base.Draw(delta);
		}
		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
			Debug.Print(info.IsKeyDown(Keys.RightShift), "shift");
			Debug.Print(info.IsKeyPressed(Keys.Up), "up_pressed");

			if (info.IsKeyPressed(Keys.Up)) {
				if (info.IsKeyDown(Keys.RightShift)) {
					if (player.OnGround()) ;
					player.Actions.Add(new Impulse(player, new Point3(0, 0, 2)));
				} else {
					player.Actions.Add(new WalkAction(player, new Point3(0, -1)));
				}
			} else if(info.IsKeyPressed(Keys.Down)) {
				if(info.IsKeyDown(Keys.RightShift)) {
					if(player.OnGround())
						player.Actions.Add(new Impulse(player, new Point3(0, 0, -2)));
				} else {
					player.Actions.Add(new WalkAction(player, new Point3(0, 1)));
				}
			} else if(info.IsKeyPressed(Keys.Left)) {
				player.Actions.Add(new WalkAction(player, new Point3(-1, 0)));
			} else if (info.IsKeyPressed(Keys.Right)) {
				player.Actions.Add(new WalkAction(player, new Point3(1, 0)));
			}

			if (info.IsKeyPressed(Keys.OemPeriod)) {
				Debug.Print("waiting");
				player.Actions.Add(new WaitAction(30));
			}
			return base.ProcessKeyboard(info);
		}
	}
}
