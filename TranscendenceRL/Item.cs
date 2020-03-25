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

            if(type.weapon != null) {
                weapon = new Weapon(this, type.weapon);
            }
        }
    }
    public interface Device {
        Item source { get; }
        void Update();
    }
    public class Weapon : Device {
        public Item source { get; private set; }
        public WeaponDesc desc;
        public int fireTime;
        public Weapon(Item source, WeaponDesc desc) {
            this.source = source;
            this.desc = desc;
            this.fireTime = 0;
        }
        public void Update() {
            if(fireTime > 0) {
                fireTime--;
            }
        }
        public void CreateShot(Ship source, double direction) {
            var shot = new Projectile(desc.effect, source.Position + XY.Polar(direction), desc.lifetime); ;
            source.world.AddEntity(shot);
        }
    }
    class Shields : Device {
        public Item source { get; private set; }
        public ShieldDesc desc;
        public uint hp;
        public uint depletionTime;
        private uint tick;
        public void Update() {
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
