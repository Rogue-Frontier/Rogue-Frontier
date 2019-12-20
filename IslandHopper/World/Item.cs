using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IslandHopper.ItemType;

namespace IslandHopper {
	public interface IItem : Entity {
		Gun Gun { get; set; }
	}
	public class Gun {
		public static Gun itLeadPipeDevice = new Gun() {
			ReloadTime = -1,
			CooldownTime = 30,
			AmmoLeft = -1,
			NoiseRange = -1
		};
		public int? ReloadTime { get; private set; }
		public int CooldownTime { get; private set; }

		public int AmmoLeft { get; private set; }

		public int NoiseRange { get; private set; }


		public Gun() { }

        public Bullet CreateShot(Entity Source, Entity Target, XYZ Velocity) {
			return new Bullet(Source, Target, Velocity);
        }
	}
	public class Item : IItem {
		public World World { get; set; }
		public XYZ Position { get; set; }
		public XYZ Velocity { get; set; }

		public ColoredGlyph SymbolCenter { get; set; }
		public ColoredString Name { get; set; }

		public ItemType type;
		public Gun Gun { get; set; }

		public bool Active => true;
		public void OnRemoved() { }

		public void UpdateRealtime(TimeSpan delta) { }
		public void UpdateStep() { }
	}

	public class Gun1 : IItem {
		public World World { get; set; }
		public XYZ Position { get; set; }
		public XYZ Velocity { get; set; }

		public Gun Gun { get; set; }

		public Gun1(World World, XYZ Position) {
			this.World = World;
			this.Position = Position;
			this.Velocity = new XYZ();
		}

		public bool Active => true;
		public void OnRemoved() { }

		public void UpdateRealtime(TimeSpan delta) { }

		public void UpdateStep() {
			this.UpdateGravity();
			this.UpdateMotion();
		}


		public ColoredString Name => new ColoredString("Gun", new Cell(Color.Gray, Color.Black));
		public ColoredGlyph SymbolCenter => new ColoredString("r", new Cell(Color.Black, Color.White))[0];
	}
	public class Parachute : Entity {
		public Entity user { get; private set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public World World => user.World;
		public XYZ Position { get; set; }
		public XYZ Velocity { get; set; }
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
            if(speed > 9.8 / 30) {
                double deceleration = speed / 30;
                user.Velocity -= vel.Normal * deceleration;
            }
            
		}
		public readonly ColoredGlyph symbol = new ColoredString("*", Color.White, Color.Transparent)[0];
		public ColoredGlyph SymbolCenter => symbol;
		public ColoredString Name => new ColoredString("Parachute", Color.White, Color.Black);
	}

}
