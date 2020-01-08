using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
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
        public IItem item;
        private Entity target;
        private XYZ targetPos;
        private XYZ aim;    //Offset from the player. When player pos + aim is close enough to the target pos, we fire
        private int shotsLeft;
        //When creating this object, caller must remember to add the Reticles to the world
        public Reticle targetReticle;
        public Reticle aimReticle;
        //We need a way to cancel this action for the player and enemies
        public ShootAction(Entity player, IItem item, Entity target, XYZ aim = null, int shotsLeft = -1) {
            this.player = player;
            this.item = item;
            this.target = target;
            this.targetPos = target.Position;
            this.aim = aim ?? new XYZ();
            this.shotsLeft = shotsLeft;
            targetReticle = new Reticle(Active, targetPos, Color.Yellow);
            aimReticle = new Reticle(Active, player.Position + this.aim, Color.Yellow);
            player.World.AddEffect(targetReticle);
            player.World.AddEffect(aimReticle);
            if(player is Player p) {
                p.Watch.Add(aimReticle);
            }
        }
        public ShootAction(Entity player, IItem item, XYZ targetPos, XYZ aim = null, int shotsLeft = -1) {
            this.player = player;
            this.item = item;
            this.target = null;
            this.targetPos = targetPos;
            this.aim = aim ?? new XYZ();
            this.shotsLeft = shotsLeft;
            targetReticle = new Reticle(Active, targetPos, Color.Yellow);
            aimReticle = new Reticle(Active, player.Position + this.aim, Color.Yellow);
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

                //We should also add target lead
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

            /*
            if(target != null) {
                var bulletSpeed;
                var travelTime = targetOffset.Magnitude / bulletSpeed;
                targetOffset += target.Velocity * travelTime;
            }
            */

            var aimPos = player.Position + aim;
            var diff = targetPos.i - aimPos.i;

            bool needAdjust = diff.Magnitude > 0.5;

            /*
            var aimAngle = aim.xyAngle;
            var targetAngle = targetOffset.xyAngle;
            var angleDiff = targetAngle - aimAngle;
            if (angleDiff > 180)
                angleDiff -= 360;
            if (angleDiff < -180)
                angleDiff += 360;

            //If the user is skilled enough to aim this weapon, then we can fire earlier if the angle is right
            if(Math.Abs(angleDiff) < 5 && diff.Magnitude < targetOffset.Magnitude / 2) {

            }
            */
            if (item.Gun.GetState() == Gun.State.NeedsAmmo) {
                //TO DO
                //For now, we should just leave a message saying that the gun is out of ammo
                shotsLeft = 0;
                player.Witness(new InfoEvent(new ColoredString("The ") + item.Name + new ColoredString(" is out of ammo!")));
            } else if (item.Gun.GetState() == Gun.State.NeedsReload) {
                //For now, we just reload if we need to
                item.Gun.Reload();

                player.Witness(new InfoEvent(player.Name + new ColoredString(" reloads ") + item.Name.WithBackground(Color.Black)));
            } else if (item.Gun.GetState() == Gun.State.Reloading) {
                //Don't allow aiming while we're reloading
            } else if (needAdjust) {
                //Bring our aim closer to the target position
                //aim += diff.Normal * Math.Min(diff.Magnitude, 1);
                //If the player is running towards/away from the target, adjust aim faster
                //var speed = Math.Abs(player.Velocity.Dot(targetOffset.Normal));
                //If the player is running, that shouldn't prevent them from aiming

                //Radial jitter to simulate difficulty of aiming at long range
                //var jitter = aim.Magnitude / 20f
                //var speed = player.Velocity.Magnitude - Math.Abs(player.Velocity.Dot(aim.Normal));

                //TO DO: We need to add jitter and inaccuracy based on difficulty
                var speed = player.Velocity.Magnitude;
                if(target != null) {
                    speed += target.Velocity.Magnitude;
                }

                var maxDelta = Math.Max(10 / 30f, 2 * diff.Magnitude / 30 + speed);

                var delta = Math.Min(diff.Magnitude, maxDelta);

                aim += diff.Normal * delta;
                aimReticle.Position = player.Position + aim;
            } else if(item.Gun.GetState() == Gun.State.Ready) {
                //Close enough to fire
                item.Gun.Fire(player, item, target, targetPos);
                shotsLeft--;
            }
        }
        public bool Active() => !Done();
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
			this.delta = (displacement / STEPS_PER_SECOND);
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
        public double z => velocity.z;
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
    public interface CompoundAction : EntityAction {}
    public class FollowPath : CompoundAction {
        public Actor actor;
        public LinkedList<XYZ> points;
        private WalkAction action;
        public FollowPath(Actor actor, LinkedList<XYZ> points) {
            this.actor = actor;
            this.points = points;
        }
        public void Update() {
            //Note: If the actor is pushed during this compound action, it will automatically warp back to the path. We should handle interruptions where the actor is attacked
            if(action?.Done() != false) {
                if(points.Count == 0) {
                    return;
                }
                //Make a new action
                points.RemoveFirst();
                if(points.Count > 0) {
                    action = new WalkAction(actor, points.First.Value - actor.Position);
                }
            } else {
                action.Update();
            }
        }
        public bool Done() => points.Count == 0;
    }
}
