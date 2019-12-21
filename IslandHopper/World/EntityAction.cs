using System;
using static IslandHopper.Constants;
namespace IslandHopper {
	public interface EntityAction {
		void Update();
		bool Done();
	}
    /*
    public class DelayedAction: EntityAction {
        public EntityAction action;
        public int delay;

        public DelayedAction(EntityAction e, int predelay) {
            this.action = e;
            this.delay = predelay;
        }
        public DelayedAction(Action a, int delay) {
            this.action = new CustomAction(a);
            this.delay = delay;
        }
        public void Update() {
            if(delay > 0) {
                delay--;
            } else {
                action.Update();
            }
        }
        public bool Done() => delay > 0 && action.Done();
    }
    public class CustomAction : EntityAction {
        public Action action;
        public bool done;

        public CustomAction(Action a) {
            this.done = false;
        }
        public void Update() {
            if(done) {

            } else {
                done = true;
                action();
            }
        }
        public bool Done() => done;
    }
    */

	public class WalkAction : EntityAction {
		private Entity player;
		private XYZ displacement;
		private XYZ delta;
		private int ticks;
		public WalkAction(Entity actor, XYZ displacement) {
			this.player = actor;
			this.displacement = displacement;
			this.delta = displacement / STEPS_PER_SECOND;
			ticks = STEPS_PER_SECOND;
		}
		public void Update() {
			player.Position += delta;
			ticks--;
		}
		public bool Done() => ticks == 0;
	}
    //Impulse that lasts for a while so that the player cannot jump multiple times at once
    public class Jump : EntityAction {
        private Entity player;
        private XYZ velocity;
        private int ticks;
        private int lifetime;
        private int deltaTime;
        public Jump(Entity actor, XYZ velocity, int lifetime = 20, int deltaTime = 1) {
            this.player = actor;
            this.velocity = velocity;
            ticks = 0;
            this.lifetime = lifetime;
            this.deltaTime = deltaTime;
        }
        public void Update() {
            if(ticks < deltaTime) {
                player.Velocity += velocity / deltaTime;
                Debug.Print("JUMP");
            }
            ticks++;
        }
        public bool Done() => ticks > lifetime;
    }
    public class Impulse : EntityAction {
		private Entity player;
		private XYZ velocity;
		private bool done;
		public Impulse(Entity actor, XYZ velocity) {
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
	public class WaitAction : EntityAction {
		private int ticks;
		public WaitAction(int ticks) {
			this.ticks = ticks;
		}
		public void Update() => ticks--;
		public bool Done() => ticks == 0;
	}
	public class AlwaysUpdate : EntityAction {
		public void Update() { }
		public bool Done() => false;
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
				Velocity += new Point3(0, 0, -9.8 / STEPS_PER_SECOND);
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
