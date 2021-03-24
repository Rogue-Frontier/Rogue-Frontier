using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helper = Common.Main;
using SadRogue.Primitives;
using Console = SadConsole.Console;

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
        public Weapon InstallWeapon() => weapon = type.weapon.GetWeapon(this);
        public Armor InstallArmor() => armor = type.armor.GetArmor(this);
        public Shields InstallShields() => shields = type.shield.GetShields(this);
        public Reactor InstallReactor() => reactor = type.reactor.GetReactor(this);
        public void RemoveAll() {
            weapon = null;
            armor = null;
            shields = null;
            reactor = null;
        }
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
    public static class SWeapon {
        public static void CreateShot(this FragmentDesc fragment, SpaceObject Source, double direction) {

            var World = Source.World;
            var Position = Source.Position;
            var Velocity = Source.Velocity;
            var angleInterval = fragment.spreadAngle / fragment.count;

            for (int i = 0; i < fragment.count; i++) {
                double angle = direction + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
                var p = new Projectile(Source,
                    fragment,
                    Position + XY.Polar(angle, 0.5),
                    Velocity + XY.Polar(angle, fragment.missileSpeed));
                World.AddEntity(p);
            }
        }
    }
    public class Weapon : Powered {
        public Item source { get; private set; }
        public WeaponDesc desc;
        public int powerUse => fireTime > 0 ? desc.powerUse : 0;
        public int missileSpeed { get {
                int result = desc.shot.missileSpeed;
                capacitor?.ModifyMissileSpeed(ref result);
                return result;
            }}
        public int currentRange => missileSpeed * desc.shot.lifetime / TranscendenceRL.TICKS_PER_SECOND;
        public Capacitor capacitor;
        public Aiming aiming;
        public IAmmo ammo;
        public int fireTime;
        public bool firing;
        public int repeatsLeft;

        public Weapon(Item source, WeaponDesc desc) {
            this.source = source;
            this.desc = desc;
            this.fireTime = 0;
            firing = false;
            if(desc.capacitor != null) {
                capacitor = new Capacitor(desc.capacitor);
            }
            if (desc.omnidirectional) {
                aiming = new Omnidirectional(this);
            }
            if(desc.initialCharges > -1) {
                ammo = new ChargeAmmo(desc.initialCharges);
            } else if(desc.ammoType != null) {
                ammo = new ItemAmmo(desc.ammoType);
            }
        }
        public ColoredString GetBar() {
            ColoredString bar;
            if (fireTime > 0) {
                int fireBar = (int)(16f * fireTime / desc.fireCooldown);
                bar = new ColoredString(new string('>', 16 - fireBar),
                                        Color.White, Color.Transparent)
                    + new ColoredString(new string('>', fireBar), Color.Gray, Color.Transparent);
            } else if (CanFire()) {

                bar = new ColoredString(new string('>', 16),
                                        Color.White, Color.Transparent);
            } else {

                bar = new ColoredString(new string('>', 16),
                                        Color.Gray, Color.Transparent);
            }
            if (capacitor != null) {
                var n = 16 * capacitor.charge / capacitor.desc.maxCharge;
                for (int j = 0; j < n; j++) {
                    bar[j].Foreground = bar[j].Foreground.Blend(Color.Cyan.SetAlpha(128));
                }
            }
            return bar;
        }

        public void Update(Station owner) {
            capacitor?.Update();
            if (ammo != null) {
                ammo.Update(owner);
            }

            double? direction = null;

            if (aiming != null) {
                aiming.Update(owner);
                if (aiming.GetFireAngle(out var d)) {
                    direction = d;
                }
            }

            if (fireTime > 0 && repeatsLeft == 0) {
                fireTime--;
            } else {
                //Stations always fire for now
                firing = true;

                bool beginRepeat = true;
                if (repeatsLeft > 0) {
                    repeatsLeft--;
                    firing = true;
                    beginRepeat = false;
                }

                //bool allowFire = (firing || true) && (capacitor?.AllowFire ?? true);
                capacitor?.CheckFire(ref firing);
                ammo?.CheckFire(ref firing);

                if (firing) {
                    if (direction.HasValue) {
                        Fire(owner, direction.Value);
                    }

                    fireTime = desc.fireCooldown;

                    if (beginRepeat) {
                        repeatsLeft = desc.repeat;
                    }
                } else {
                    repeatsLeft = 0;
                }
            }
            firing = false;
        }

        public void Update(IShip owner) {
            double direction = owner.rotationDegrees * Math.PI / 180;

            if (aiming != null) {
                aiming.Update(owner);
                if(aiming.GetFireAngle(out var d)) {
                    direction = d;
                }
            }

            capacitor?.Update();
            if (ammo != null) {
                ammo.Update(owner);
            }
            if(fireTime > 0 && repeatsLeft == 0) {
                fireTime--;
            } else {

                bool beginRepeat = true;
                if (repeatsLeft > 0) {
                    repeatsLeft--;
                    firing = true;
                    beginRepeat = false;
                }

                //bool allowFire = firing && (capacitor?.AllowFire ?? true);
                capacitor?.CheckFire(ref firing);
                ammo?.CheckFire(ref firing);

                if (firing) {
                    ammo?.OnFire();
                    Fire(owner, direction);
                    fireTime = desc.fireCooldown;
                    if (beginRepeat) {
                        repeatsLeft = desc.repeat;
                    }
                } else {
                    repeatsLeft = 0;
                }
            }
            firing = false;
        }
        public bool RangeCheck(SpaceObject user, SpaceObject target) {
            return (user.Position - target.Position).Magnitude < currentRange;
        }
        public bool CanFire() {
            if(fireTime > 0) {
                return false;
            }
            bool firing = true;
            capacitor?.CheckFire(ref firing);
            if(ammo != null ) {

                ammo.CheckFire(ref firing);
            }
            return firing;
        }
        public void Fire(SpaceObject source, double direction) {
            int damageHP = desc.damageHP;
            int missileSpeed = desc.shot.missileSpeed;
            int lifetime = desc.lifetime;

            capacitor?.Modify(ref damageHP, ref missileSpeed, ref lifetime);
            capacitor?.Discharge();

            var shotDesc = desc.shot;
            double angleInterval = shotDesc.spreadAngle / shotDesc.count;
            for (int i = 0; i < shotDesc.count; i++) {
                double angle = direction + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
                Projectile p = new Projectile(source,
                    shotDesc,
                    source.Position + XY.Polar(angle),
                    source.Velocity + XY.Polar(angle, missileSpeed));
                source.World.AddEntity(p);
            }
        }

        public SpaceObject target => aiming?.target;
        public void OverrideTarget(SpaceObject target) {

            if (aiming != null) {
                aiming.ResetTarget();
                aiming.UpdateTarget(target);
            }
        }
        public void SetFiring(bool firing = true) => this.firing = firing;

        //Use this if you want to override auto-aim
        public void SetFiring(bool firing = true, SpaceObject target = null) {
            this.firing = firing;
            aiming?.UpdateTarget(target);
        }

        public interface IAmmo {
            public void Update(IShip source) { }
            public void Update(Station source) { }
            void CheckFire(ref bool firing);
            void OnFire();
        }
        public class ChargeAmmo : IAmmo {
            public int charges;
            public bool AllowFire => charges > 0;
            public ChargeAmmo(int charges) {
                this.charges = charges;
            }
            public void CheckFire(ref bool firing) {
                firing &= charges > 0;
            }

            public void OnFire() {
                charges--;
            }
        }
        public class ItemAmmo : IAmmo {
            private ItemType itemType;
            private HashSet<Item> itemSource;
            private Item item;
            //public bool AllowFire => false;
            public ItemAmmo(ItemType itemType) {
                this.itemType = itemType;
            }
            public void Update(IShip source) {
                Update(source.Items);
            }
            public void Update(Station source) {
                Update(source.Items);
            }
            public void Update(HashSet<Item> items) {
                if (item == null || !items.Contains(item)) {
                    itemSource = items;
                    item = items.FirstOrDefault(i => i.type == itemType);
                }
            }
            public void CheckFire(ref bool firing) {
                firing &= item != null;
            }
            public void OnFire() {
                itemSource.Remove(item);
            }
        }
        public interface Aiming {
            public SpaceObject target => null;
            void Update(Station owner) { }
            void Update(IShip owner) { }
            bool GetFireAngle(out double direction) {
                direction = 0;
                return false;
            }
            void ResetTarget() { }
            void UpdateTarget(SpaceObject target = null) {}
        }
        public class Omnidirectional : Aiming {
            public Weapon weapon;
            public SpaceObject target { get; private set; }
            double? direction;
            public Omnidirectional(Weapon weapon) {
                this.weapon = weapon;
            }
            public void Update(Station owner) {
                if (target?.Active != true) {
                    direction = null;
                    target = owner.World.entities.GetAll(p => (owner.Position - p).Magnitude < weapon.desc.minRange).OfType<SpaceObject>().FirstOrDefault(s => owner.CanTarget(s));
                } else {
                    direction = (((target.Position - owner.Position).Magnitude < weapon.currentRange)
                        ? Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, weapon.missileSpeed, out var _)
                        : (double?)null);
                    if (direction.HasValue) {
                        Heading.AimLine(owner.World, owner.Position, direction.Value);
                        Heading.Crosshair(owner.World, target.Position);
                    }
                }
            }
            public void Update(IShip owner) {
                if (target?.Active != true) {
                    direction = null;
                    target = owner.World.entities.GetAll(p => (owner.Position - p).Magnitude < weapon.desc.minRange).OfType<SpaceObject>().FirstOrDefault(s => SShip.IsEnemy(owner, s));
                } else {
                    direction = (((target.Position - owner.Position).Magnitude < weapon.currentRange)
                        ? Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, weapon.missileSpeed, out var _)
                        : (double?)null);
                    if (direction.HasValue) {
                        Heading.AimLine(owner.World, owner.Position, direction.Value);
                        Heading.Crosshair(owner.World, target.Position);
                    }
                }
            }
            public bool GetFireAngle(out double direction) {
                if(this.direction != null) {
                    direction = this.direction.Value;
                    return true;
                } else {
                    direction = 0;
                    return false;
                }
            }
            public void ResetTarget() => target = null;
            public void UpdateTarget(SpaceObject target = null) {
                this.target = target ?? this.target;
            }
        }
        public class Capacitor {
            public CapacitorDesc desc;
            public double charge;
            public Capacitor(CapacitorDesc desc) {
                this.desc = desc;
            }
            public void CheckFire(ref bool firing) => firing = firing && AllowFire;
            public bool AllowFire => desc.minChargeToFire < charge;
            public void Update() {
                charge += desc.chargePerTick;
                if(charge > desc.maxCharge) {
                    charge = desc.maxCharge;
                }
            }
            public void ModifyMissileSpeed(ref int missileSpeed) {
                missileSpeed += (int)(desc.bonusSpeedPerCharge * charge);
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
            energy = Math.Max(0, Math.Min(energy + (energyDelta < 0 ? energyDelta / desc.efficiency : energyDelta) / 30, desc.capacity));
        }
    }

    public interface IUsable {

    }
    public class ArmorRepair : IUsable {
        public Item source;
        public ArmorRepair(Item source) {
            this.source = source;
        }
        public void Use(PlayerShip playerShip, Armor armor) {
            playerShip.Items.Remove(source);

        }
    }
}
