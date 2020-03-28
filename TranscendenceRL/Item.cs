using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class Item {
        public ItemType type;
        public Weapon weapon;

        public Item(ItemType type) {
            this.type = type;
            if(type.weapon != null) {
                weapon = new Weapon(this, type.weapon);
            }
        }
    }
    public interface Device {
        Item source { get; }
        void Update(IShip owner);
    }
    public class Weapon : Device {
        public Item source { get; private set; }
        public WeaponDesc desc;
        public int fireTime;
        public bool firing;
        public Weapon(Item source, WeaponDesc desc) {
            this.source = source;
            this.desc = desc;
            this.fireTime = 0;
            firing = false;
        }
        public void Update(IShip owner) {
            if(fireTime > 0) {
                fireTime--;
            } else if(firing) {
                Fire(owner, owner.rotationDegrees * Math.PI / 180);
                fireTime = desc.fireCooldown;
            }
            firing = false;
        }
        public void Fire(IShip source, double direction) {
            var shot = new Projectile(source, source.World,
                desc.effect.Glyph,
                source.Position + XY.Polar(direction),
                source.Velocity + XY.Polar(direction, desc.missileSpeed),
                desc.damageHP,
                desc.lifetime);
            source.World.AddEntity(shot);
        }
        public void SetFiring(bool firing = true) => this.firing = firing;
    }
    class Shields : Device {
        public Item source { get; private set; }
        public ShieldDesc desc;
        public uint hp;
        public uint depletionTime;
        private uint tick;
        public void Update(IShip owner) {
            if(depletionTime > 0) {
                depletionTime--;
            } else if(hp < desc.maxHP) {
                tick++;
                if(tick%desc.ticksPerHP == 0) {
                    hp++;
                }
            }
        }
        public void Absorb(uint damage) {
            hp = Math.Max(0, hp - damage);
            if(hp == 0) {
                depletionTime = desc.depletionDelay;
            }
        }
    }
}
