using Microsoft.Xna.Framework;
using SadConsole;
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
    public class ShootAction : EntityAction {
        private Entity player;
        private IItem item;
        private Entity target;
        private XYZ targetPos;
        private XYZ aim;    //Offset from the player. When player pos + aim is close enough to the target pos, we fire
        private int shotsLeft;
        //When creating this object, caller must remember to add the Reticles to the world
        public Reticle targetReticle;
        public Reticle aimReticle;
        public ShootAction(Entity player, IItem item, Entity target, int shotsLeft = 1) {
            this.player = player;
            this.item = item;
            this.target = target;
            this.targetPos = target.Position;
            aim = new XYZ();
            this.shotsLeft = shotsLeft;
            targetReticle = new Reticle(Active, targetPos);
            aimReticle = new Reticle(Active, player.Position + aim);
            player.World.AddEffect(targetReticle);
            player.World.AddEffect(aimReticle);
            if(player is Player p) {
                p.Watch.Add(aimReticle);
            }
        }
        public ShootAction(Entity player, IItem item, XYZ targetPos, int shotsLeft = 1) {
            this.player = player;
            this.item = item;
            this.target = null;
            this.targetPos = targetPos;
            aim = new XYZ();
            this.shotsLeft = shotsLeft;
            targetReticle = new Reticle(Active, targetPos);
            aimReticle = new Reticle(Active, player.Position + aim);
            player.World.AddEffect(targetReticle);
            player.World.AddEffect(aimReticle);
            if (player is Player p) {
                p.Watch.Add(aimReticle);
            }
        }
        public void Update() {
            if (shotsLeft == 0)
                return;
            if (target != null) {
                targetPos = target.Position;
                targetReticle.Position = targetPos;
            }
            /*
            var aimAngle = aim.xyAngle;
            var targetOffset = (targetPos - player.Position);
            var targetAngle = targetOffset.xyAngle;
            var angleDiff = targetAngle - aimAngle;
            if (angleDiff > 180)
                angleDiff -= 360;
            if (angleDiff < -180)
                angleDiff += 360;

            var radiusDiff = targetOffset.Magnitude - aim.Magnitude;
            if (Math.Abs(angleDiff) > 5) {
                //Bring our aim closer to the target position
                var dir = angleDiff / Math.Abs(angleDiff);
                var delta = Math.Min(angleDiff, Math.Max(1, 1 / aim.Magnitude));
                aim = aim.RotateZ(delta * dir);
                aimReticle.Position = player.Position + aim;
            } else if(Math.Abs(radiusDiff) > 5) {
                var dir = radiusDiff / Math.Abs(radiusDiff);
                aim = aim.Extend(dir);
                aimReticle.Position = player.Position + aim;
            }
            */
            var targetOffset = targetPos - player.Position;
            var aimPos = player.Position + aim;
            var diff = targetPos.i - aimPos.i;
            if (diff.Magnitude > 0.5) {
                //Bring our aim closer to the target position
                //aim += diff.Normal * Math.Min(diff.Magnitude, 1);
                //If the player is running towards/away from the target, adjust aim faster
                //var speed = Math.Abs(player.Velocity.Dot(targetOffset.Normal));
                //If the player is running, that shouldn't prevent them from aiming
                var speed = player.Velocity.Magnitude;
                var delta = Math.Min(diff.Magnitude, Math.Max(10/30f, 2 * diff.Magnitude / 30 + speed));
                aim += diff.Normal * delta;
                aimReticle.Position = player.Position + aim;
            } else {
                //Close enough to fire
                if(target != null) {
                    Shoot(item, target);
                } else {
                    Shoot(item, targetPos);
                }
                shotsLeft--;
            }
        }
        public void Shoot(IItem item, Entity target) {
            var bulletSpeed = 30;
            var bulletVel = (target.Position - player.Position).Normal * bulletSpeed;
            Bullet b = new Bullet(player, item, target, bulletVel);
            player.World.AddEntity(b);
            if (player is Player p) {
                p.Watch.Add(b);
                p.frameCounter = Math.Max(p.frameCounter, 30);
            }
            player.World.AddEffect(new Reticle(() => b.Active, target.Position, Color.Red));
            player.Witness(new InfoEvent(player.Name + new ColoredString(" shoots ") + item.Name.WithBackground(Color.Black) + new ColoredString(" at: ") + target.Name.WithBackground(Color.Black)));
        }
        public void Shoot(IItem item, XYZ target) {
            var bulletSpeed = 30;
            var bulletVel = (target - player.Position).Normal * bulletSpeed;
            Bullet b = new Bullet(player, item, null, bulletVel);
            player.World.AddEntity(b);
            if (player is Player p) {
                p.Watch.Add(b);
                p.frameCounter = Math.Max(p.frameCounter, 30);
            }
            player.World.AddEffect(new Reticle(() => b.Active, target, Color.Red));
            player.Witness(new InfoEvent(player.Name + new ColoredString(" shoots ") + item.Name.WithBackground(Color.Black)));
        }
        public bool Active() => shotsLeft > 0;
        public bool Done() => shotsLeft == 0;

        public class Targeting {
            private Entity target;
            private XYZ pos;
            public Targeting(Entity target) {
                this.target = target;
            }
            public Targeting(XYZ pos) {
                this.pos = pos;
            }
            public XYZ Position => target?.Position ?? pos;
            public Entity Target => target;
        }
    }

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
