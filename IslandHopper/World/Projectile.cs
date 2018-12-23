using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IslandHopper.Helper;

namespace IslandHopper {
	public class ThrownItem : Entity {
        public Entity Thrower { get; private set; }
        public IItem Thrown { get; private set; }
        public World World { get => Thrown.World; }
        public Point3 Position { get => Thrown.Position; set => Thrown.Position = value; }
        public Point3 Velocity { get => Thrown.Velocity; set => Thrown.Velocity = value; }
        public bool Active => flying && Thrown.Active;
        private bool flying;
        private int tick = 0;
        public void OnRemoved() {
            if (Thrown.Active) {
                Thrown.World.AddEntity(Thrown);
            }
        }
        public ThrownItem(Entity thrower, IItem source) {
            this.Thrower = thrower;
            this.Thrown = source;
            flying = true;
        }
        public ColoredGlyph SymbolCenter => tick % 20 < 10 ? Thrown.SymbolCenter : Thrown.SymbolCenter.Brighten(51);
        public ColoredString Name => tick % 20 < 10 ? Thrown.Name : Thrown.Name.Brighten(51);

        public void UpdateRealtime(TimeSpan delta) {
            tick++;
            Thrown.UpdateRealtime(delta);
        }
        public void UpdateStep() {
            this.Info("UpdateStep()");
            Thrown.UpdateGravity();
            Thrown.UpdateMotionCollision(e => {
				if (e == Thrower) {
                    this.Info("Ignore collision with thrower");
                    return true;
                } else {
                    this.Info("Flying collision with object");
					Thrower.Witness(new InfoEvent($"Thrown item {Thrown.Name} hits {e.Name}"));
                    flying = false;
                    return false;
                }
            });
            if (this.OnGround()) {
                this.Info("Landed on ground");
                flying = false;
            }
        }
    }
	public class Beam : Entity {
		public World World { get; }
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public ColoredGlyph SymbolCenter => new ColoredString("~", tick % 20 < 10 ? Color.White : Color.Gray, Color.Black)[0];
		public ColoredString Name => new ColoredString("Beam", tick % 20 < 10 ? new Color(255, 0, 0, 255) : new Color(204, 0, 0, 255), Color.Black);

		private Entity Source;
		private Entity Target;
		private int tick;   //Used for sprite flashing

		public Beam(Entity Source, Entity Target, Point3 Velocity) {
			this.Source = Source;
			this.Target = Target;
			this.World = Source.World;
			this.Position = Source.Position;
			this.Velocity = Velocity;
			Active = true;
			tick = 0;
		}
		public void UpdateRealtime(TimeSpan delta) {
			tick++;
		}

		public void UpdateStep() {
			Func<Entity, bool> ignoreSource = e => e == Source;
			Func<Entity, bool> filterTarget = e => e != Target;
			//We only reach this if the other conditions were not met
			Func<Entity, bool> onHit = e => {
				Active = false;
				Source.Witness(new InfoEvent($"The {Name} hits {e.Name}"));
				return false;
			};

			Func<Entity, bool> collisionFilter = Helper.Or(Source.Elvis(ignoreSource), Target.Elvis(filterTarget), onHit);

			this.UpdateMotionCollision(collisionFilter);
		}
	}
	public class Bullet : Entity {
		public World World { get; }
        public Point3 Position { get; set; }
        public Point3 Velocity { get; set; }
        public bool Active { get; private set; }
		public void OnRemoved() { }
		public ColoredGlyph SymbolCenter => new ColoredString("*", tick % 20 < 10 ? Color.White : Color.Gray, Color.Black)[0];
        public ColoredString Name => new ColoredString("Bullet", tick % 20 < 10 ? Color.White : Color.Gray, Color.Black);

		private Entity Source;
		private Entity Target;
		private int tick;   //Used for sprite flashing

		public Bullet(Entity Source, Entity Target, Point3 Velocity) {
            this.Source = Source;
            this.Target = Target;
            this.World = Source.World;
            this.Position = Source.Position;
            this.Velocity = Velocity;
            Active = true;
            tick = 0;
        }
        public void UpdateRealtime(TimeSpan delta) {
            tick++;
        }

        public void UpdateStep() {
            this.UpdateGravity();

            Func<Entity, bool> ignoreSource = e => e == Source;
            Func<Entity, bool> filterTarget = e => e != Target;
			Func<Entity, bool> onHit = e => {
				Active = false;
				Source.Witness(new InfoEvent($"The {Name} hits {e.Name}"));
				return false;
			};
			Func<Entity, bool> collisionFilter = Helper.Or(Source.Elvis(ignoreSource), Target.Elvis(filterTarget), onHit);

            this.UpdateMotionCollision(collisionFilter);
        }
    }
	public class Missile : Entity {
		public World World { get; }
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public ColoredGlyph SymbolCenter => new ColoredString("M", tick % 8 < 4 ? Color.White : Color.Gray, Color.Black)[0];
		public ColoredString Name => new ColoredString("Missile", tick % 8 < 4 ? Color.White : Color.Gray, Color.Black);

		private Entity Source;
		private Entity Target;
		private int tick;   //Used for sprite flashing

		public Missile(Entity Source, Entity Target, Point3 Velocity) {
			this.Source = Source;
			this.Target = Target;
			this.World = Source.World;
			this.Position = Source.Position;
			this.Velocity = Velocity;
			Active = true;
			tick = 0;
		}
		public void UpdateRealtime(TimeSpan delta) {
			tick++;
		}

		public void UpdateStep() {
			this.UpdateGravity();

			Func<Entity, bool> ignoreSource = e => e == Source;
			Func<Entity, bool> filterTarget = e => e != Target;

			Func<Entity, bool> collisionFilter = Helper.Or(Source.Elvis(ignoreSource), Target.Elvis(filterTarget));

			this.UpdateMotionCollision(collisionFilter);
		}
	}
	class ExplosionBlock : Entity {
		public World World { get; }
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public ColoredGlyph SymbolCenter => new ColoredString("*", tick % 4 < 2 ? new Color(255, 255, 0) : new Color(255, 153, 0), Color.Black)[0];
		public ColoredString Name => new ColoredString("Explosion", tick % 4 < 2 ? new Color(255, 255, 0) : new Color(255, 153, 0), Color.Black);

		private int tick;
		private int lifetime;
		public ExplosionBlock(World World, Point3 Position) {
			this.World = World;
			this.Position = Position;
			Velocity = new Point3(0, 0, 0);
			tick = 0;
			lifetime = 10;
			Active = true;
		}

		public void UpdateRealtime(TimeSpan delta) {
			tick++;
		}

		public void UpdateStep() {
			Active = lifetime --> 0;
		}
	}
	class ExplosionSource : Entity {
		public World World { get; }
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public ColoredGlyph SymbolCenter => new ColoredString("*", tick % 4 < 2 ? new Color(255, 255, 0) : new Color(255, 153, 0), Color.Black)[0];
		public ColoredString Name => new ColoredString("Explosion", tick % 4 < 2 ? new Color(255, 255, 0) : new Color(255, 153, 0), Color.Black);

		private int tick;   //Used for sprite flashing

		private List<Point3> explosionOffsets;	//List of points surrounding our center that we will expand to
		private int tileIndex;
		private int rectRadius;

		private double maxRadius;
		private double currentRadius;
		private double expansionRate;
		
		public ExplosionSource(World World, Point3 Position, double maxRadius) {
			this.World = World;
			this.Position = Position;
			Velocity = new Point3(0, 0, 0);
			Active = true;
			tick = 0;

			//We calculate surrounding tiles as the explosion expands and fill them with explosion effects
			explosionOffsets = new List<Point3>();
			explosionOffsets.Add(new Point3(0, 0, 0));
			tileIndex = 0;
			rectRadius = 0;
			//In case we want to pre-calculate everything at once
			/*
			for (int i = 1; i <= maxRadius + 1; i++) {
				explosionPoints.AddRange(Helper.GetSurrounding(Position, i));
			}
			*/
			this.maxRadius = maxRadius;
			currentRadius = 0;
			expansionRate = 1;
		}
		public void UpdateRealtime(TimeSpan delta) {
			tick++;
		}
		public void UpdateStep() {
			currentRadius += expansionRate;
			//Expand to our edge tiles and then self destruct
			if(currentRadius > maxRadius) {
				currentRadius = maxRadius;
				Active = false;
			}

			this.Info("UpdateStep");
			this.Info($"Current Radius: {currentRadius}");

			//See if we need to calculate more surrounding tiles now (the farthest tile calculated so far is within the current radius)
			while (explosionOffsets.Last().Magnitude < currentRadius) {
				//Calculate a few rects at a time because the corners prevent us from checking further
				for (int i = 0; i < 6; i++) {
					rectRadius++;
					//Add the surrounding shell of tiles to our list.
					explosionOffsets.AddRange(Helper.GetSurrounding(rectRadius));
					this.Info($"Added surrounding tiles for radius: {rectRadius}");
				}
			}
			while (explosionOffsets[tileIndex].Magnitude < currentRadius) {
				//Expand to this tile
				World.AddEntity(new ExplosionBlock(World, Position + explosionOffsets[tileIndex]));

				this.Info($"Expanded to tile index: {tileIndex}");

				//Increment the index since we covered this tile
				//We do not remove elements because ignoring them is more efficient
				tileIndex++;    //This should not go past the list because we pre-calculated points ahead of our current radius
			}
		}
	}
}
