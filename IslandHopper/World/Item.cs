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
    public interface Usable {
        bool CanUse();
        void Use();
    }
    public class Grenade {
        public IItem item;
        public GrenadeType type;
        public bool Armed;
        public int Countdown;
        public Grenade(IItem item) {
            this.item = item;
            this.Armed = false;
        }
        public void Arm(bool Armed = true) {
            this.Armed = Armed;
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
    public class Gun {
        public GunType gunType;
        public int ReloadTimeLeft;
        public int FireTimeLeft;

        public int ClipLeft;
        public int AmmoLeft;

        public Gun() { }

        public void OnHit(Damager b, Damageable d) {

        }
        public void Reload() {
            ReloadTimeLeft = gunType.reloadTime;
        }
        private void OnReload() {
            int reloaded = Math.Min(gunType.clipSize - ClipLeft, AmmoLeft);
            ClipLeft += reloaded;
            AmmoLeft -= reloaded;
        }
        public bool CanFire() {
            return ReloadTimeLeft + FireTimeLeft == 0;
        }
        public void UpdateStep() {
            if(ReloadTimeLeft > 0) {
                if(--ReloadTimeLeft == 0) {
                    OnReload();
                }
            } else if(FireTimeLeft > 0) {
                FireTimeLeft--;
            }
        }
        public void Fire(Entity user, IItem item, Entity target, XYZ targetPos) {
            var bulletSpeed = 30;
            var bulletVel = (targetPos - user.Position).Normal * bulletSpeed;
            Bullet b = new Bullet(user, item, target, bulletVel);
            user.World.AddEntity(b);
            if (user is Player p) {
                p.Watch.Add(b);
                p.frameCounter = Math.Max(p.frameCounter, 30);
            }
            user.World.AddEffect(new Reticle(() => b.Active, targetPos, Color.Red));
            user.Witness(new InfoEvent(user.Name + new ColoredString(" fires ") + item.Name.WithBackground(Color.Black) + (target != null ? (new ColoredString(" at ") + target.Name.WithBackground(Color.Black)) : new ColoredString(""))));
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

        public ColoredGlyph SymbolCenter { get; set; } = new ColoredGlyph('r', Color.Black, Color.White);

        public ColoredString Name { get {
                ColoredString result = new ColoredString(type.name, Color.Black, Color.White);
                if(Grenade?.Armed == true) {
                    result = new ColoredString("[Armed] ", Color.Red, Color.White) + result;
                }
                return result;
            } }

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
            Gun = new Gun();
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
            Gun?.UpdateStep();
            
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
		public ColoredGlyph SymbolCenter => new ColoredGlyph('r', Color.Black, Color.White);
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
