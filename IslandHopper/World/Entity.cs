using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IslandHopper.Constants;

namespace IslandHopper {
	public interface Entity {
		World World { get; }
		Point3 Position { get; set; }			//Position in meters
		Point3 Velocity { get; set; }			//Velocity in meters per step
		bool Active { get; }                    //	When this is inactive, we remove it
		void OnRemoved();
		void UpdateRealtime(TimeSpan delta);				//	For step-independent effects
		void UpdateStep();					//	The number of steps per one in-game second is defined in Constants as STEPS_PER_SECOND

		ColoredGlyph SymbolCenter { get; }
		ColoredString Name { get; }
	}
	public static class SGravity {
		public static bool OnGround(this Entity g) => (g.World.voxels[g.Position].Collision == VoxelType.Floor || g.World.voxels[g.Position.PlusZ(-1)].Collision == VoxelType.Solid);
		public static void UpdateGravity(this Entity g) {
			//	Fall or hit the ground
			if (g.Velocity.z < 0 && g.OnGround()) {
				g.Velocity.z = 0;
			} else {
				Debug.Print("fall");
				g.Velocity += new Point3(0, 0, -9.8 / STEPS_PER_SECOND);
			}
		}
		//We attempt to enforce continuous collision detection by incrementing the motion in small steps
		private static Point3 CalcMotionStep(Point3 Velocity) {
			if (Velocity.Magnitude < 1) {
				return Velocity / 4;
			} else {
				return Velocity.Normal;
			}
		}
		private static void UpdateFriction(this Entity g) {
			//Ground friction
			if (g.OnGround()) {
				g.Velocity.x *= 0.9;
				g.Velocity.y *= 0.9;
			}
		}
		public static void UpdateMotion(this Entity g) {
			g.UpdateFriction();
			if(g.Velocity < 0.1) {
				return;
			}
			Point3 step = CalcMotionStep(g.Velocity);
			Point3 final = g.Position;
			for (Point3 p = g.Position + step; (g.Position - p).Magnitude < g.Velocity.Magnitude; p += step) {
				if (g.World.voxels.Try(p) is Air) {
					final = p;
				} else {
					break;
				}
			}
			/*
			//The velocity is the displacement that we were supposed to travel for this step
			//This is the average velocity for the actual displacement we traveled in this step
			Point3 velocityAverage = (final - g.Position);
			//This is the displacement that we did not get to travel this tick
			Point3 velocityDelta = velocityAverage - g.Velocity;
			*/
			g.Position = final;
		}
		public static void UpdateMotionCollision(this Entity g, Func<Entity, bool> ignoreCollision) {
			g.UpdateFriction();
			if (g.Velocity < 0.1) {
				return;
			}
			Point3 step = CalcMotionStep(g.Velocity);
			Point3 final = g.Position;
			for (Point3 p = g.Position + step; (g.Position - p).Magnitude < g.Velocity.Magnitude; p += step) {
				if (g.World.voxels.Try(p) is Air) {
					if(g.World.entities.Try(p).All(ignoreCollision)) {
						final = p;
					}
				} else {
					break;
				}
			}
			g.Position = final;
		}
	}
	public class Player : Entity {
		public Point3 Velocity { get; set; }
		public Point3 Position { get; set; }
		public World World { get; set; }
		public HashSet<EntityAction> Actions { get; private set; }
		public HashSet<IItem> Inventory { get; private set; }

		public List<ColoredString> HistoryLog { get; }	//All events that the player has witnessed
		public List<HistoryEntry> HistoryRecent { get; }   //Events that the player is currently witnessing
		public class HistoryEntry {
			public ColoredString Desc { get; }
			public double ScreenTime;
			public HistoryEntry(ColoredString Desc, double ScreenTime = 4) {
				this.Desc = Desc;
				this.ScreenTime = ScreenTime;
			}
		}


		public int frameCounter = 0;

		public Player(World World, Point3 Position) {
			this.World = World;
			this.Position = Position;
			this.Velocity = new Point3(0, 0, 0);
			Actions = new HashSet<EntityAction>();
			Inventory = new HashSet<IItem>();

			HistoryLog = new List<ColoredString>();
			HistoryRecent = new List<HistoryEntry>();

			World.AddEntity(new Parachute(this));
		}
		public bool AllowUpdate() => Actions.Count > 0 && frameCounter == 0;
		public bool Active => true;
		public void OnRemoved() { }
		public void UpdateRealtime(TimeSpan delta) {
            HistoryRecent.RemoveAll(e => (e.ScreenTime -= delta.TotalSeconds) < 0);
			if(frameCounter > 0)
				frameCounter--;
		}
		public void UpdateStep() {
			this.UpdateGravity();
			this.UpdateMotion();
			Actions.ToList().ForEach(a => a.Update());
			Actions.RemoveWhere(a => a.Done());
			foreach(var i in Inventory) {
				i.Position = Position;
				i.Velocity = Velocity;
			}
			if(!this.OnGround())
				frameCounter = 20;

			HistoryRecent.RemoveAll(e => e.ScreenTime < 1);
		}

		public void Witness(WorldEvent e) {
            HistoryLog.Add(e.Self);
            HistoryRecent.Add(new HistoryEntry(e.Self));
        }

		public ColoredGlyph SymbolCenter => new ColoredString("@", Color.White, Color.Black)[0];
		public ColoredString Name => new ColoredString("Player", Color.White, Color.Black);
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
		public void UpdateRealtime(TimeSpan delta) {

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
