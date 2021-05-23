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
        public MiscDevice misc;
        public Item() { }
        public Item(Item clone) {
            type = clone.type;
            weapon = clone.weapon != null ? new Weapon(this, clone.weapon.desc) : null;
            armor = clone.armor != null ? new Armor(this, clone.armor.desc) : null;
            shields = clone.shields != null ? new Shields(this, clone.shields.desc) : null;
            reactor = clone.reactor != null ? new Reactor(this, clone.reactor.desc) : null;
            misc = clone.misc != null ? new MiscDevice(this, clone.misc.desc) : null;
        }
        public Item(ItemType type) {
            this.type = type;
            //These fields are to remain null while the item is not installed and to be populated upon installation
            weapon = null;
            armor = null;
            shields = null;
            reactor = null;
        }
        public T GetDevice<T>() {
            var type = typeof(T);
            return (T)new Dictionary<Type, object>() {
                { typeof(Weapon), weapon },
                { typeof(Armor), armor },
                { typeof(Shields), shields },
                { typeof(Reactor), reactor },
                { typeof(MiscDevice), misc }
            }[type];
        }
        public Weapon InstallWeapon() => type.weapon != null ?
            weapon = new Weapon(this, type.weapon) : null;
        public Armor InstallArmor() => type.armor != null ?
            armor = new Armor(this, type.armor) : null;
        public Shields InstallShields() => type.shield != null ?
            shields = new Shields(this, type.shield) : null;
        public Reactor InstallReactor() => type.reactor != null ?
            reactor = new Reactor(this, type.reactor) : null;
        public MiscDevice InstallMisc() => type.misc != null ?
            misc = new MiscDevice(this, type.misc) : null;
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

            var world = Source.world;
            var position = Source.position;
            var velocity = Source.velocity;
            var angleInterval = fragment.spreadAngle / fragment.count;

            for (int i = 0; i < fragment.count; i++) {
                double angle = direction + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
                var p = new Projectile(Source, world,
                    fragment,
                    position + XY.Polar(angle, 0.5),
                    velocity + XY.Polar(angle, fragment.missileSpeed));
                world.AddEntity(p);
            }
        }
    }
    public class Weapon : Powered {
        public Item source { get; private set; }
        public WeaponDesc desc;
        public int powerUse => fireTime > 0 ? desc.powerUse : desc.powerUse / 10;
        public int missileSpeed { get {
                int result = desc.shot.missileSpeed;
                capacitor?.ModifyMissileSpeed(ref result);
                return result;
            }}
        public int currentRange => missileSpeed * desc.shot.lifetime / Program.TICKS_PER_SECOND;
        public int currentRange2 => currentRange * currentRange;
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
            if (desc.shot.omnidirectional) {
                aiming = new Omnidirectional();
            } else if(desc.shot.maneuver > 0) {
                aiming = new Targeting();
            }
            if(desc.initialCharges > -1) {
                ammo = new ChargeAmmo(desc.initialCharges);
            } else if(desc.ammoType != null) {
                ammo = new ItemAmmo(desc.ammoType);
            }
        }
        public string GetReadoutName() {
            if(ammo is ChargeAmmo c) {
                return $"{source.type.name} [{c.charges}]";
            }
            return source.type.name;
        }
        public ColoredString GetBar() {
            if(ammo?.AllowFire == false) {
                return new ColoredString(new string(' ', 16), Color.Transparent, Color.Black);
            }

            int fireBar = (int)(16f * (desc.fireCooldown - fireTime) / desc.fireCooldown);
            ColoredString bar;
            if (capacitor != null && capacitor.desc.minChargeToFire > 0) {
                var chargeBar = (int)(16 * Math.Min(1, capacitor.charge / capacitor.desc.minChargeToFire));
                bar = new ColoredString(new string('>', chargeBar), Color.Gray, Color.Black)
                    + new ColoredString(new string(' ', 16 - chargeBar), Color.Transparent, Color.Black);
            } else {
                bar = new ColoredString(new string('>', 16), Color.Gray, Color.Black);
            }
            foreach (var cg in bar.Take(fireBar)) {
                cg.Foreground = Color.White;
            }

            if (capacitor != null) {
                var n = 16 * capacitor.charge / capacitor.desc.maxCharge;
                foreach(var cg in bar.Take((int)n + 1)) {
                    cg.Foreground = cg.Foreground.Blend(Color.Cyan.SetAlpha(128));
                }
            }
            return bar;
        }

        public void Update(Station owner) {
            double? direction = null;
            if (aiming != null) {
                aiming.Update(owner, this);
                if(aiming.GetFireAngle(ref direction)) {
                    
                } else if(target != null) {
                    Aiming.CalcFireAngle(owner, aiming.target, this, out direction);
                }
            }
            capacitor?.Update();
            if (ammo != null) {
                ammo.Update(owner);
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
                } else if (desc.autoFire) {
                    if (desc.targetProjectile) {
                        var target = Aiming.AcquireMissile(owner, this, s => SStation.IsEnemy(owner, s));
                        if (target != null
                            && Aiming.CalcFireAngle(owner, target, this, out var d)) {
                            direction = d;
                            firing = true;
                        }
                    } else if (aiming?.target != null) {
                        firing = true;
                    }
                }
                //bool allowFire = (firing || true) && (capacitor?.AllowFire ?? true);
                capacitor?.CheckFire(ref firing);
                ammo?.CheckFire(ref firing);
                if (firing && direction.HasValue) {
                    ammo?.OnFire();
                    Fire(owner, direction.Value);
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
            double? direction = owner.rotationDeg * Math.PI / 180;

            if (aiming != null) {
                aiming.Update(owner, this);
                aiming.GetFireAngle(ref direction);
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
                } else if(desc.autoFire) {
                    if(desc.targetProjectile) {
                        var target = Aiming.AcquireMissile(owner, this, s => s == null || SShip.IsEnemy(owner, s));
                        if(target != null
                            && Aiming.CalcFireAngle(owner, target, this, out var d)) {
                            direction = d;
                            firing = true;
                        }
                    } else if(aiming?.target != null) {
                        firing = true;
                    }
                }

                //bool allowFire = firing && (capacitor?.AllowFire ?? true);
                capacitor?.CheckFire(ref firing);
                ammo?.CheckFire(ref firing);

                if (firing) {
                    ammo?.OnFire();
                    Fire(owner, direction.Value);
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
            return (user.position - target.position).magnitude < currentRange;
        }
        public bool AllowFire => ammo?.AllowFire ?? true;
        public bool CanFire => fireTime == 0 && (capacitor?.AllowFire ?? true) && (ammo?.AllowFire ?? true);
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
                var maneuver = new Maneuver(aiming?.target, desc.shot.maneuver, desc.shot.maneuverRadius);
                Projectile p = new Projectile(source, source.world,
                    shotDesc,
                    source.position + XY.Polar(angle),
                    source.velocity + XY.Polar(angle, missileSpeed),
                    maneuver) { hitProjectile = desc.targetProjectile };
                source.world.AddEntity(p);
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
        public class Capacitor {
            public CapacitorDesc desc;
            public double charge;
            public Capacitor(CapacitorDesc desc) {
                this.desc = desc;
            }
            public void CheckFire(ref bool firing) => firing = firing && AllowFire;
            public bool AllowFire => desc.minChargeToFire <= charge;
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
        public interface Aiming {
            public SpaceObject target { get; }
            void Update(Station owner, Weapon weapon);
            void Update(IShip owner, Weapon weapon);
            bool GetFireAngle(ref double? direction) {
                return false;
            }
            void ResetTarget() { }
            void UpdateTarget(SpaceObject target = null) {}

            static bool CalcFireAngle(MovingObject owner, MovingObject target, Weapon weapon, out double? result) {
                if(((target.position - owner.position).magnitude < weapon.currentRange)) {
                    result = Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, weapon.missileSpeed, out var _);
                    return true;
                } else {
                    result = null;
                    return false;
                }
            }
            static double CalcFireAngle(MovingObject owner, MovingObject target, int missileSpeed) {
                return Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, missileSpeed, out var _);
            }
            static bool CalcFireAngle(MovingObject owner, MovingObject target, int missileSpeed, out double result) {
                var velDiff = target.velocity - owner.velocity;
                if(velDiff.magnitude < missileSpeed) {
                    result = Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, missileSpeed, out var _);
                    return true;
                } else {
                    result = 0;
                    return false;
                }
            }

            static bool CalcFireAngle(MovingObject owner, Projectile target, Weapon weapon, out double? result) {
                if (((target.position - owner.position).magnitude < weapon.currentRange)) {
                    result = Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, weapon.missileSpeed, out var _);
                    return true;
                } else {
                    result = null;
                    return false;
                }
            }
            static SpaceObject AcquireTarget(SpaceObject owner, Weapon weapon, Func<SpaceObject, bool> filter) {
                return owner.world.entities.GetAll(p => (owner.position - p).magnitude2 < weapon.currentRange2).OfType<SpaceObject>().FirstOrDefault(filter);
            }
            static Projectile AcquireMissile(SpaceObject owner, Weapon weapon, Func<SpaceObject, bool> filter) {
                return owner.world.entities.all
                                    .OfType<Projectile>()
                                    .Where(p => (owner.position - p.position).magnitude2 < weapon.currentRange2)
                                    .Where(p => filter(p.source))
                                    .OrderBy(p => (owner.position - p.position).Dot(p.velocity))
                                    //.OrderBy(p => (owner.Position - p.Position).Magnitude2)
                                    .FirstOrDefault();
            }
        }
        public class Targeting : Aiming {
            public SpaceObject target { get; set; }
            public Targeting() { }
            public void Update(SpaceObject owner, Weapon weapon, Func<SpaceObject, bool> filter) {
                if (target?.active != true
                    || (owner.position - target.position).magnitude > weapon.currentRange
                    ) {
                    target = Aiming.AcquireTarget(owner, weapon, filter);
                }
            }
            public void Update(Station owner, Weapon weapon) {
                Update(owner, weapon, s => SStation.IsEnemy(owner, s));
            }
            public void Update(IShip owner, Weapon weapon) {
                Update(owner, weapon, s => SShip.IsEnemy(owner, s));
            }
            public void ResetTarget() => target = null;
            public void UpdateTarget(SpaceObject target = null) {
                this.target = target ?? this.target;
            }
        }
        public class Omnidirectional : Aiming {
            public SpaceObject target { get; set; }
            double? direction;
            public Omnidirectional() { }
            public void Update(SpaceObject owner, Weapon weapon, Func<SpaceObject, bool> filter) {
                if (target?.active == true) {
                    UpdateDirection();
                } else {
                    direction = null;
                    target = Aiming.AcquireTarget(owner, weapon, filter);

                    if (target?.active == true) {
                        UpdateDirection();
                    }
                }

                void UpdateDirection () {
                    if(Aiming.CalcFireAngle(owner ,target, weapon, out direction)) {
                        Heading.AimLine(owner.world, owner.position, direction.Value);
                        Heading.Crosshair(owner.world, target.position);
                    }
                }
            }
            public void Update(Station owner, Weapon weapon) {
                Update(owner, weapon, s => SStation.IsEnemy(owner, s));
            }
            public void Update(IShip owner, Weapon weapon) {
                Update(owner, weapon, s => SShip.IsEnemy(owner, s));
            }
            public bool GetFireAngle(ref double? direction) {
                if(this.direction != null) {
                    direction = this.direction.Value;
                    return true;
                }
                return false;
            }
            public void ResetTarget() => target = null;
            public void UpdateTarget(SpaceObject target = null) {
                this.target = target ?? this.target;
            }
        }
        public interface IAmmo {
            bool AllowFire { get; }
            public void Update(IShip source) { }
            public void Update(Station source) { }
            void CheckFire(ref bool firing) => firing &= AllowFire;
            void OnFire();
        }
        public class ChargeAmmo : IAmmo {
            public int charges;
            public bool AllowFire => charges > 0;
            public ChargeAmmo(int charges) {
                this.charges = charges;
            }

            public void OnFire() {
                charges--;
            }
        }
        public class ItemAmmo : IAmmo {
            public ItemType itemType;
            public HashSet<Item> itemSource;
            public Item item;
            public bool AllowFire => item != null;
            public ItemAmmo(ItemType itemType) {
                this.itemType = itemType;
            }
            public void Update(IShip source) {
                Update(source.cargo);
            }
            public void Update(Station source) {
                Update(source.cargo);
            }
            public void Update(HashSet<Item> items) {
                if (item == null || !items.Contains(item)) {
                    itemSource = items;
                    item = items.FirstOrDefault(i => i.type == itemType);
                }
            }
            public void OnFire() {
                itemSource.Remove(item);
            }
        }

    }
    public class Armor {
        public Item source;
        public ArmorDesc desc;
        public int hp;
        public Armor() { }
        public Armor(Item source, ArmorDesc desc) {
            this.source = source;
            this.desc = desc;
            this.hp = desc.maxHP;
        }
        public void Update(IShip owner) {

        }
    }
    public class Shields : Device {
        public Item source { get; set; }
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
        public Item source { get; set; }
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
    public class MiscDevice : Device {
        public Item source { get; set; }
        public MiscDesc desc;
        public int ticks;
        public MiscDevice(Item source, MiscDesc desc) {
            this.source = source;
            this.desc = desc;
        }
        public void Update(IShip owner) {
            ticks++;
            if(ticks%desc.interval == 0) {
                if(desc.missileJack) {
                    //May not work in Arena mode if we assume control
                    //bc weapon locks are focused on the old AI ship
                    var missile = owner.world.entities.all
                        .OfType<Projectile>()
                        .FirstOrDefault(
                            p => (owner.position - p.position).magnitude < 24
                              && p.maneuver != null
                              && p.maneuver.maneuver > 0
                              && SSpaceObject.Equals(p.maneuver.target, owner)
                            );
                    if(missile != null) {

                        if (owner is PlayerShip) {
                            int i = 0;
                        }

                        missile.maneuver.target = missile.source;
                        missile.source = owner;

                        var offset = (missile.position - owner.position);
                        var dist = offset.magnitude;
                        var inc = offset.normal;
                        for(var i = 0; i < dist; i++) {
                            var p = owner.position + inc * i;
                            owner.world.AddEffect(new EffectParticle(p, new ColoredGlyph(Color.Orange, Color.Transparent, '-'), 10));
                        }

                    } else {
                        if(owner is PlayerShip && owner.world.entities.all.OfType<Projectile>().Any()) {
                            int i = 0;
                        }
                    }
                }
            }
        }
    }
}
