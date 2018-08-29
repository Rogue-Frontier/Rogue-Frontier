using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IslandHopper.Constants;

namespace IslandHopper {
	interface Entity {
		Point3 Position { get; set; }
		Point3 Velocity { get; set; }
		bool IsActive();					//	When this is inactive, we remove it
		void UpdateRealtime();				//	For step-independent effects
		void UpdateStep();					//	The number of steps per one in-game second is defined in Constants as STEPS_PER_SECOND
		ColoredString GetSymbolCenter();
		ColoredString GetName();
	}
	static class SGravity {
		public static bool OnGround(this IGravity g) => (g.World.voxels[g.Position] is Floor || g.World.voxels[g.Position.PlusZ(-1)] is Grass);
		public static void UpdateGravity(this IGravity g) {
			//	Fall or hit the ground
			if (g.Velocity.z < 0 && g.OnGround()) {
				g.Velocity.z = 0;
			} else {
				System.Console.WriteLine("fall");
				g.Velocity += new Point3(0, 0, -9.8 / STEPS_PER_SECOND);
			}
		}
		public static void UpdateMotion(this IGravity g) {
			Point3 normal = g.Velocity.Normal();
			Point3 dest = g.Position;
			for (Point3 p = g.Position + normal; (g.Position - p).Magnitude() < g.Velocity.Magnitude(); p += normal) {
				if (g.World.voxels[p] is Air) {
					dest = p;
				} else {
					break;
				}
			}
			g.Position = dest;
		}
	}
	interface IGravity {
		World World { get; set; }
		Point3 Position { get; set; }
		Point3 Velocity { get; set; }
	}
	class Player : Entity, IGravity {
		public Point3 Velocity { get; set; }
		public Point3 Position { get; set; }
		public World World { get; set; }
		public HashSet<EntityAction> Actions { get; private set; }
		public HashSet<Item> inventory { get; private set; }

		public Player(World World, Point3 Position) {
			this.World = World;
			this.Position = Position;
			this.Velocity = new Point3(0, 0, 0);
			Actions = new HashSet<EntityAction>();
			inventory = new HashSet<Item>();
		}
		public bool AllowUpdate() => Actions.Count > 0;
		public bool IsActive() => true;
		public void UpdateRealtime() {

		}
		public void UpdateStep() {
			this.UpdateGravity();
			this.UpdateMotion();
			Actions.ToList().ForEach(a => a.Update());
			Actions.RemoveWhere(a => a.Done());
			foreach(var i in inventory) {
				i.Position = Position;
				i.Velocity = Velocity;
			}
		}

		public readonly ColoredString symbol = new ColoredString("@", Color.White, Color.Transparent);
		public ColoredString GetSymbolCenter() => symbol;
		public ColoredString GetName() => new ColoredString("Player", Color.White, Color.Black);
	}
	class Parachute : Entity {
		public Entity user { get; private set; }
		public bool Active { get; private set; }
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }
		public Parachute(Entity user) {
			this.user = user;
			this.Position = user.Position + new Point3(0, 0, 1);
			this.Velocity = user.Velocity;
		}

		public bool IsActive() => Active;

		public void UpdateRealtime() {}

		public void UpdateStep() {
			Position = user.Position + new Point3(0, 0, 1);
			Velocity = user.Velocity;

			Point3 forward = user.Position - Position;
			Point3 backward = -forward;
			double speed = forward * user.Velocity;
			if(speed > 3.8/30) {
				double deceleration = speed * 0.1;

				user.Velocity += backward * deceleration;
			}
		}
		public readonly ColoredString symbol = new ColoredString("*", Color.White, Color.Transparent);
		public ColoredString GetSymbolCenter() => symbol;
		public ColoredString GetName() => new ColoredString("Parachute", Color.White, Color.Black);
	}
	interface Item : Entity {
		Gun Gun { get; set; }
	}
	interface Gun {

	}
	class Gun1 : Item, IGravity {
		public Gun Gun {get; set;}
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }
		public World World { get; set; }

		public Gun1(World World, Point3 Position) {
			this.World = World;
			this.Position = Position;
			this.Velocity = new Point3();
		}

		public bool IsActive() => true;

		public void UpdateRealtime() { }

		public void UpdateStep() {
			this.UpdateGravity();
			this.UpdateMotion();
		}


		public ColoredString GetName() => new ColoredString("Gun", new Cell(Color.Gray, Color.Transparent));
		public ColoredString GetSymbolCenter() => new ColoredString("r", new Cell(Color.Gray, Color.Transparent));
	}

	/*
	class Human : Entity {
		public Point3 Velocity { get; set; }
		public Point3 Position { get; set; }
		public GameConsole World { get; private set; }
		public HashSet<PlayerAction> Actions;
		public Human(GameConsole World, Point3 Position) {
			this.World = World;
			this.Position = Position;
			this.Velocity = new Point3(0, 0, 0);
			Actions = new HashSet<PlayerAction>();
		}
		public bool IsActive() => true;
		public bool OnGround() => (World.voxels[Position] is Floor || World.voxels[Position.PlusZ(-1)] is Grass);
		public void UpdateRealtime() {

		}
		public void UpdateStep() {
			//	Fall or hit the ground
			if(Velocity.z < 0 && OnGround()) {
				Velocity.z = 0;
			} else {
				System.Console.WriteLine("fall");
				Velocity += new Point3(0, 0, -9.8 / 30);
			}
			Point3 normal = Velocity.Normal();
			Point3 dest = Position;
			for(Point3 p = Position + normal; (Position - p).Magnitude() < Velocity.Magnitude(); p += normal) {
				if(World.voxels[p] is Air) {
					dest = p;
				} else {
					break;
				}
			}
			Position = dest;
			Actions.ToList().ForEach(a => a.Update());
			Actions.RemoveWhere(a => a.Done());
		}

		public static readonly ColoredString symbol = new ColoredString("U", Color.White, Color.Transparent);
		public virtual ColoredString GetSymbolCenter() => symbol;
	}
	class Player : Human {

		public Player(GameConsole World, Point3 Position) : base(World, Position) { }
		public bool AllowUpdate() => Actions.Count > 0;
		public static ColoredString symbol_player = new ColoredString("@", Color.White, Color.Transparent);
		public override ColoredString GetSymbolCenter() => symbol_player;
	}
	*/
}
