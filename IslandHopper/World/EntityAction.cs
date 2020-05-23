using Common;
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
    public interface ICompoundAction : EntityAction {}
    public class CompoundAction : ICompoundAction {
        public EntityAction[] actions;
        int index = 0;
        public CompoundAction(params EntityAction[] actions) {
            this.actions = actions;
        }
        public void Update() {
            if (Done()) {
                return;
            }
            actions[index].Update();
            if (actions[index].Done()) {
                index++;
            }
        }
        public bool Done() => index == actions.Length;
    }
    public class TakeItem : ICompoundAction {
        public Entity actor;
        public IItem item;
        public bool done;
        public TakeItem(Actor actor, IItem item) {
            this.actor = actor;
            this.item = item;
        }
        public void Update() {
            if(actor.World.entities[actor.Position].Contains(item)) {
                
                done = true;
            }
        }
        public bool Done() => done;
    }
    public class FollowPath : ICompoundAction {
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
