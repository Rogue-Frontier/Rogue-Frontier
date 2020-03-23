using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class Item {
        ItemType type;
        Weapon weapon;
    }
    interface Device {
        void Update();
    }
    class Weapon : Device {
        WeaponDesc desc;
        int fireTime;
        public void Update() {
            if(fireTime > 0) {
                fireTime--;
            }
        }
        public void CreateShot(Ship source, double direction) {
            var shot = new Projectile(desc.effect, source.position + XY.Polar(direction), desc.lifetime); ;
            source.world.AddEntity(shot);
        }
    }
    class Shields : Device {
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
