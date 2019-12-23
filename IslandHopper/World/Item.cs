using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IslandHopper.ItemType;

namespace IslandHopper {
	public interface IItem : Entity, Damageable {
        void Destroy();
        Grenade Grenade { get; set; }
		Gun Gun { get; set; }

	}
    public interface ItemComponent {
        void UpdateRealtime(TimeSpan delta);
        void UpdateStep();
    }
    public class Grenade {
        IItem item;
        public bool Armed;
        public int Countdown;
        public Grenade(IItem item) {
            this.item = item;
            this.Armed = false;
        }
        public void Arm(bool Armed = true) {
            this.Armed = Armed;
            this.Countdown = 3;
        }
        public void UpdateStep() {
            if(Armed) {
                if(Countdown > 0) {
                    Countdown--;
                } else {
                    item.Destroy();
                    item.World.AddEntity(new ExplosionSource(item.World, item.Position, 6));
                }
            }
        }
    }
    public class GrenadeType {
        public bool DetonateOnDamage;
        public bool DetonateOnImpact;
        public bool CanArm;
    }
	public class Gun {
        public GunType gunType;
        public int ReloadTimeLeft;
        public int FireTimeLeft;

        public int ClipLeft;
        public int AmmoLeft;


		public Gun() { }
        public void Fire(Entity user, XYZ direction, Entity target) {

        }
        /*
        public Bullet CreateShot(Entity Source, Entity Target, XYZ Velocity) {
			return new Bullet(Source, Target, Velocity);
        }
        */
	}
	public class Item : IItem {
		public Island World { get; set; }
		public XYZ Position { get; set; }
		public XYZ Velocity { get; set; }

		public ColoredGlyph SymbolCenter { get; set; }
		public ColoredString Name { get; set; }

		public ItemType type;
        public Grenade Grenade { get; set; }
		public Gun Gun { get; set; }

        public bool Active { get; private set; } = true;
		public void OnRemoved() { }

		public void UpdateRealtime(TimeSpan delta) { }
		public void UpdateStep() { }
        public void Destroy() {
            Active = false;
        }

        public void OnDamaged(Damager source) {
            if(source is Bullet b) {
                Velocity += b.Velocity.Normal * b.knockback;
            }
        }
    }

    public class Gun1 : IItem {
        public Island World { get; set; }
        public XYZ Position { get; set; }
        public XYZ Velocity { get; set; }

        public Grenade Grenade { get; set; }
        public Gun Gun { get; set; }

        public Gun1(Island World, XYZ Position) {
            this.World = World;
            this.Position = Position;
            this.Velocity = new XYZ();

            Grenade = new Grenade(this);
        }

        public bool Active { get; private set; } = true;
        public void OnRemoved() { }

        public void UpdateRealtime(TimeSpan delta) { }

        public void UpdateStep() {
            
            //Somehow this prevents the player from moving when held
            //It's because the Velocity of this item is a reference to the player's velocity
            this.UpdateGravity();
            this.UpdateMotion();
            Grenade?.UpdateStep();
            
        }
        public void Destroy() {
            Active = false;
        }

        public void OnDamaged(Damager source) {
            if (source is Bullet b) {
                Velocity += b.Velocity.Normal * b.knockback;
            }
        }

        public ColoredString Name {
            get {
                var result = new ColoredString("Gun", Color.Gray, Color.Black);
                if (Grenade?.Armed == true)
                    result = new ColoredString("[Armed] ", Color.Red, Color.Black) + result;
                return result;
            }
        }
		public ColoredGlyph SymbolCenter => new ColoredString("r", new Cell(Color.Black, Color.White))[0];
	}
	public class Parachute : Entity, Damageable {
		public Entity user { get; private set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public Island World => user.World;
		public XYZ Position { get; set; }
		public XYZ Velocity { get; set; }

        public int durability = 50;
		public Parachute(Entity user) {
			this.user = user;
			UpdateFromUser();
			Active = true;
		}

		public void UpdateRealtime(TimeSpan delta) {
		}
		public void UpdateFromUser() {
			Position = user.Position + new XYZ(0, 0, 1);
			Velocity = user.Velocity;
		}
		public void UpdateStep() {
			Debug.Print(nameof(UpdateStep));
            //This actually ends up pulling the player upward when they are moving really fast
            //Also boosts jumps
            /*
            UpdateFromUser();
			XYZ down = user.Position - Position;
			double speed = down * user.Velocity.Magnitude;
			if (speed > 3.8 / 30) {
				double deceleration = speed * 0.4;
				user.Velocity -= down * deceleration;
			}
            */
            
            UpdateFromUser();
            var vel = user.Velocity;
            var speed = vel.Magnitude;
            var terminal = 9.8 / 30;
            if (speed > terminal) {
                double deceleration = speed / 30;
                user.Velocity -= vel.Normal * deceleration;
            }
            
		}
        public void OnDamaged(Damager source) {
            if(source is Bullet b) {
                durability -= b.damage;
                user.Witness(new InfoEvent(source.Name + new ColoredString(" damages ") + Name));
            }
            if(durability < 1) {
                Active = false;
                user.Witness(new InfoEvent(Name + new ColoredString(" is destroyed!")));
            }
        }
		public readonly ColoredGlyph symbol = new ColoredString("*", Color.White, Color.Transparent)[0];
		public ColoredGlyph SymbolCenter => symbol;
		public ColoredString Name => new ColoredString("Parachute", Color.White, Color.Black);
	}

}
