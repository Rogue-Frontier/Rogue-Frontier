using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	interface Entity {
		Point3 Position { get; set; }
		Point3 Velocity { get; set; }
		bool IsActive();					//	When this is inactive, we remove it
		void UpdateRealtime();				//	For step-independent effects
		void UpdateStep();					//	30 steps is one in-game second
		ColoredString GetSymbolCenter();
	}
	interface PlayerAction {
		void Update();
		bool Done();
	}
	class WalkAction : PlayerAction {
		private Entity player;
		private Point3 displacement;
		private Point3 delta;
		private int ticks;
		public WalkAction(Entity actor, Point3 displacement) {
			this.player = actor;
			this.displacement = displacement;
			this.delta = displacement / 30;
			ticks = 30;
		}
		public void Update() {
			player.Position += delta;
			ticks--;
		}
		public bool Done() => ticks == 0;
	}
	class Impulse : PlayerAction {
		private Entity player;
		private Point3 velocity;
		private bool done;
		public Impulse(Entity actor, Point3 velocity) {
			this.player = actor;
			this.velocity = velocity;
			done = false;
		}
		public void Update() {
			player.Velocity += velocity;
			Debug.Print("JUMP");
			done = true;
		}
		public bool Done() => done;
	}
	class WaitAction : PlayerAction {
		int ticks;
		public WaitAction(int ticks) {
			this.ticks = ticks;
		}
		public void Update() => ticks--;
		public bool Done() => ticks == 0;
	}
	class AlwaysUpdate : PlayerAction {
		public void Update() { }
		public bool Done() => false;
	}
	class Player : Entity {
		public Point3 Velocity { get; set; }
		public Point3 Position { get; set; }
		public GameConsole World { get; private set; }
		public HashSet<PlayerAction> Actions;
		public Player(GameConsole World, Point3 Position) {
			this.World = World;
			this.Position = Position;
			this.Velocity = new Point3(0, 0, 0);
			Actions = new HashSet<PlayerAction>();
		}
		public bool AllowUpdate() => Actions.Count > 0;
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

		public readonly ColoredString symbol = new ColoredString("@", Color.White, Color.Transparent);
		public ColoredString GetSymbolCenter() => symbol;
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
		}
		public readonly ColoredString symbol = new ColoredString("*", Color.White, Color.Transparent);
		public ColoredString GetSymbolCenter() => symbol;
	}

	interface Item : Entity {

	}
	class Gun : Item {
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }

		public bool IsActive() => true;

		public void UpdateRealtime() { }

		public void UpdateStep() { }

		public ColoredString GetSymbolCenter() => new ColoredString("R", new Cell(Color.Gray, Color.Transparent));
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
	static class GravityImpl {
		public static bool OnGround(this Gravity g) => (g.World.voxels[g.Position] is Floor || g.World.voxels[g.Position.PlusZ(-1)] is Grass);
		public static void UpdateGravity(this Gravity g) {
			//	Fall or hit the ground
			if (g.Velocity.z < 0 && g.OnGround()) {
				g.Velocity.z = 0;
			} else {
				System.Console.WriteLine("fall");
				g.Velocity += new Point3(0, 0, -9.8 / 30);
			}
		}
		public static void UpdateMotion(this Gravity g) {
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
	interface Gravity {
		GameConsole World { get; set; }
		Point3 Position { get; set; }
		Point3 Velocity { get; set; }
	}
	*/
}
