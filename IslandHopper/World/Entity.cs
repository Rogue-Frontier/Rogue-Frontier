using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IslandHopper.Constants;

namespace IslandHopper {
    /*
    public interface Visible {
        XYZ Position { get; set; }
        ColoredGlyph SymbolCenter { get; }
        bool Active { get; }
    }
    */
    public interface Actor : Entity {
        HashSet<EntityAction> actions { get; }
    }
    public interface Entity : Effect {
        Island World { get; }
		//XYZ Position { get; set; }			//Position in meters
		XYZ Velocity { get; set; }			//Velocity in meters per step
		//bool Active { get; }                    //	When this is inactive, we remove it
		void OnRemoved();
		//void UpdateRealtime(TimeSpan delta);				//	For step-independent effects
		//void UpdateStep();					//	The number of steps per one in-game second is defined in Constants as STEPS_PER_SECOND

		//ColoredGlyph SymbolCenter { get; }
		ColoredString Name { get; }
	}
    public interface Damageable {
        void OnDamaged(Damager source);
    }
    public interface Damager {
    }
	public static class EntityHelper {
		public static bool OnGround(this Entity g) => g.World.voxels.InBounds(g.Position) && (g.World.voxels[g.Position].Collision == VoxelType.Floor || g.World.voxels[g.Position.PlusZ(-0.8)].Collision == VoxelType.Solid);
		public static void UpdateGravity(this Entity g) {
            g.UpdateFriction();
            //	Fall or hit the ground
            if (g.OnGround()) {
                if(g.Velocity.z < 0) {
                    g.Velocity.z = 0;
                }
			} else {
				Debug.Print("fall");
				g.Velocity += new XYZ(0, 0, -9.8 / STEPS_PER_SECOND);
			}
		}
		//We attempt to enforce continuous collision detection by incrementing the motion in small steps
		private static XYZ CalcMotionStep(XYZ Velocity) {
			if (Velocity.Magnitude < 1) {
				return Velocity / 4;
			} else {
				return Velocity.Normal / 4;
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
            //TO DO: Implement fall damage

			if(g.Velocity < 0.1) {
                var p = g.Position + g.Velocity;
                var v = g.World.voxels.Try(p);
                if (v is Air) {
                    g.Position = p;
                }
                return;
			}
			XYZ step = CalcMotionStep(g.Velocity);
			XYZ final = g.Position;
			for (XYZ p = g.Position + step; (g.Position - p).Magnitude < g.Velocity.Magnitude; p += step) {
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
		public static void UpdateMotionCollision(this Entity g, Func<Entity, bool> ignoreEntityCollision = null, Func<Voxel, bool> ignoreTileCollision = null) {
            //If the velocity is too small, then we get an infinite loop from trying to increment
            if (g.Velocity < 0.1) {
                var p = g.Position + g.Velocity;
                var v = g.World.voxels.Try(p);
                if (v is Air || ignoreTileCollision?.Invoke(v) == true) {
                    if (ignoreEntityCollision != null) {
                        var entities = g.World.entities[p].Where(e => !ReferenceEquals(e, g));
                        foreach (var entity in entities) {
                            if (!ignoreEntityCollision(entity)) {
                                return;
                            }
                        }
                        g.Position = p;
                    } else {
                        g.Position = p;
                    }
                }
                return;
			}
            //ignoreEntityCollision = ignoreEntityCollision ?? (e => true);
            //ignoreTileCollision = ignoreTileCollision ?? (v => false);
			XYZ step = CalcMotionStep(g.Velocity);
			XYZ final = g.Position;
			for (XYZ p = g.Position + step; (g.Position - p).Magnitude < g.Velocity.Magnitude; p += step) {
				var v = g.World.voxels.Try(p);
				if (v is Air || ignoreTileCollision?.Invoke(v) == true) {
					if(ignoreEntityCollision != null) {
						var entities = g.World.entities[p].Where(e => !ReferenceEquals(e, g));
                        foreach(var entity in entities) {
                            if(!ignoreEntityCollision(entity)) {
                                goto Done;
                            }
                        }
                        final = p;
                    } else {
						final = p;
					}
				} else {
					break;
				}
			}
            Done:
			g.Position = final;
		}
        public static void UpdateMotionCollisionTrail(this Entity g, out HashSet<XYZ> trail, Func<Entity, bool> ignoreEntityCollision = null, Func<Voxel, bool> ignoreTileCollision = null) {
            trail = new HashSet<XYZ>(new XYZGridComparer());

            if (g.Velocity < 0.1) {
                var p = g.Position + g.Velocity;
                var v = g.World.voxels.Try(p);
                if (v is Air || ignoreTileCollision?.Invoke(v) == true) {
                    if (ignoreEntityCollision != null) {
                        var entities = g.World.entities[p].Where(e => !ReferenceEquals(e, g));
                        foreach (var entity in entities) {
                            if (!ignoreEntityCollision(entity)) {
                                return;
                            }
                        }
                        g.Position = p;
                    } else {
                        g.Position = p;
                    }
                }
                trail.Add(p.i);
                return;
            }

            //ignoreEntityCollision = ignoreEntityCollision ?? (e => true);
            //ignoreTileCollision = ignoreTileCollision ?? (v => false);
            XYZ step = CalcMotionStep(g.Velocity);
            XYZ final = g.Position;
            trail.Add(final.i);
            for (XYZ p = g.Position + step; (g.Position - p).Magnitude < g.Velocity.Magnitude; p += step) {
                var v = g.World.voxels.Try(p);
                if (v is Air || ignoreTileCollision?.Invoke(v) == true) {
                    if (ignoreEntityCollision != null) {
                        var entities = g.World.entities[p].Where(e => !ReferenceEquals(e, g));
                        foreach(var entity in entities) {
                            if(!ignoreEntityCollision(entity)) {
                                goto Done;
                            }
                        }
                        final = p;
                        trail.Add(final.i);
                    } else {
                        final = p;
                        trail.Add(final.i);
                    }
                } else {
                    break;
                }
            }
            Done:
            g.Position = final;
        }
        public static void Witness(this Entity e, WorldEvent we) {
			if (e is Witness w)
				w.Witness(we);
		}
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
