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
        public Armor armor;
        public Shields shields;
        public Reactor reactor;

        public Item(ItemType type) {
            this.type = type;
            //These fields are to remain null while the item is not installed and to be populated upon installation
            weapon = null;
            armor = null;
            shields = null;
        }
        public Weapon InstallWeapon() => weapon = new Weapon(this, type.weapon);
        public Armor InstallArmor() => armor = new Armor(this, type.armor);
        public Shields InstallShields() => shields = new Shields(this, type.shield);
        public Reactor InstallReactor() => reactor = new Reactor(this, type.reactor);

        public void RemoveWeapon() => weapon = null;
        public void RemoveArmor() => armor = null;
        public void RemoveShields() => shields = null;
        public void RemoveReactor() => reactor = null;
    }
    public interface Device {
        Item source { get; }
        void Update(IShip owner);
    }
    public interface Powered : Device {
        int powerUse { get; }
    }
    public class Weapon : Powered {
        public Item source { get; private set; }
        public WeaponDesc desc;
        public int powerUse => desc.powerUse;
        public Capacitor capacitor;
        public SpaceObject target;
        public int fireTime;
        public bool firing;

        public Weapon(Item source, WeaponDesc desc) {
            this.source = source;
            this.desc = desc;
            this.fireTime = 0;
            firing = false;
            if(desc.capacitor != null) {
                capacitor = new Capacitor(desc.capacitor);
            }
        }

        public void Update(Station owner) {
            double? targetAngle = null;
            if (target?.Active != true) {
                target = owner.World.entities.GetAll(p => (owner.Position - p).Magnitude < desc.range).OfType<SpaceObject>().FirstOrDefault(s => owner.CanTarget(s));
            } else {
                var angle = Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, desc.missileSpeed, out var _);
                if (desc.omnidirectional) {
                    Heading.AimLine(owner.World, owner.Position, angle);
                    Heading.Crosshair(owner.World, target.Position);
                }
                targetAngle = angle;
            }
            capacitor?.Update();
            if (fireTime > 0) {
                fireTime--;
            } else /* if (firing) */ {  //Stations always fire for now
                if (targetAngle != null) {
                    Fire(owner, targetAngle.Value);
                }
                fireTime = desc.fireCooldown;
            }
            firing = false;
        }

        public void Update(IShip owner) {
            double? targetAngle = null;
            if(target?.Active != true) {
                target = owner.World.entities.GetAll(p => (owner.Position - p).Magnitude < desc.range).OfType<SpaceObject>().FirstOrDefault(s => SShip.CanTarget(owner, s));
            } else {
                var angle = Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, desc.missileSpeed, out var _);
                if(desc.omnidirectional) {
                    Heading.AimLine(owner.World, owner.Position, angle);
                    Heading.Crosshair(owner.World, target.Position);
                }
                targetAngle = angle;
            }
            capacitor?.Update();
            if(fireTime > 0) {
                fireTime--;
            } else if(firing) {
                if(desc.omnidirectional && targetAngle != null) {
                    Fire(owner, targetAngle.Value);
                } else {
                    Fire(owner, owner.rotationDegrees * Math.PI / 180);
                }
                fireTime = desc.fireCooldown;
            }
            firing = false;
        }
        public void Fire(SpaceObject source, double direction) {
            int damageHP = desc.damageHP;
            int missileSpeed = desc.missileSpeed;
            int lifetime = desc.lifetime;

            capacitor?.Modify(ref damageHP, ref missileSpeed, ref lifetime);
            capacitor?.Discharge();

            var shot = new Projectile(source, source.World,
                desc.effect.Glyph,
                source.Position + XY.Polar(direction),
                source.Velocity + XY.Polar(direction, missileSpeed),
                damageHP,
                lifetime);
            source.World.AddEntity(shot);
        }
        public void SetFiring(bool firing = true) => this.firing = firing;

        //Use this if you want to override auto-aim
        public void SetFiring(bool firing = true, SpaceObject target = null) {
            this.firing = firing;
            this.target = target ?? this.target;
        }

        public class Capacitor {
            public CapacitorDesc desc;
            public double charge;
            public Capacitor(CapacitorDesc desc) {
                this.desc = desc;
            }
            public void Update() {
                charge += desc.chargePerTick;
                if(charge > desc.maxCharge) {
                    charge = desc.maxCharge;
                }
            }
            public void Modify(ref int damage, ref int missileSpeed, ref int lifetime) {
                damage += (int) (desc.bonusDamagePerCharge * charge);
                missileSpeed += (int)(desc.bonusSpeedPerCharge * charge);
                lifetime += (int)(desc.bonusLifetimePerCharge * charge);
            }
            public void Discharge() {
                charge = Math.Max(0, charge - desc.dischargePerShot);
            }

        }
    }
    public class Armor {
        public Item source { get; private set; }
        public ArmorDesc desc;
        public int hp;
        public Armor(Item source, ArmorDesc desc) {
            this.source = source;
            this.desc = desc;
            this.hp = desc.maxHP;
        }
        public void Update(IShip owner) {

        }
    }
    public class Shields : Device {
        public Item source { get; private set; }
        public ShieldDesc desc;
        public int hp;
        public int depletionTime;
        public double regenHP;
        public Shields(Item source, ShieldDesc desc) {
            this.source = source;
            this.desc = desc;
        }
        public void Update(IShip owner) {
            if (depletionTime > 0) {
                depletionTime--;
            } else if (hp < desc.maxHP) {
                regenHP += desc.hpPerSecond / 30;

                Regen:
                if(regenHP >= 1) {
                    hp++;
                    regenHP--;
                    if(hp < desc.maxHP) {
                        goto Regen;
                    } else {
                        regenHP = 0;
                    }
                }


            }
        }
        public void Absorb(int damage) {
            hp = Math.Max(0, hp - damage);
            if (hp == 0) {
                depletionTime = desc.depletionDelay;
            }
        }
    }
    public class Reactor : Device {
        public Item source { get; private set; }
        public ReactorDesc desc;
        public double energy;
        public double energyDelta;
        public int maxOutput => energy > 0 ? desc.maxOutput : 0;
        public Reactor(Item source, ReactorDesc desc) {
            this.source = source;
            this.desc = desc;
            energy = desc.capacity;
            energyDelta = 0;
        }
        public void Update(IShip owner) {
            energy = Math.Max(0, Math.Min(energy + energyDelta, desc.capacity));
        }
    }
}
