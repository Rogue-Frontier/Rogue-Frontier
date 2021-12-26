using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using Helper = Common.Main;
using SadRogue.Primitives;
using Newtonsoft.Json;

namespace RogueFrontier;
public class Item {
    public string name => type.name;
    public ItemType type;

    //These fields are to remain null while the item is not installed and to be populated upon installation
    public Weapon weapon;
    public Armor armor;
    public Shield shield;
    public Reactor reactor;
    public Solar solar;
    public Service service;

    public Modifier mod;

    public Item() { }
    public Item(Item clone) {
        type = clone.type;
        weapon = clone.weapon != null ? new Weapon(this, clone.weapon.desc) : null;
        armor = clone.armor != null ? new Armor(this, clone.armor.desc) : null;
        shield = clone.shield != null ? new Shield(this, clone.shield.desc) : null;
        reactor = clone.reactor != null ? new Reactor(this, clone.reactor.desc) : null;
        solar = clone.solar != null ? new Solar(this, clone.solar.desc) : null;
        service = clone.service != null ? new Service(this, clone.service.desc) : null;
    }
    public Item(ItemType type, Modifier mod = null) {
        this.type = type;
        this.mod = mod;

        weapon = null;
        armor = null;
        shield = null;
        reactor = null;
        solar = null;
        service = null;
    }
    public T Get<T>() where T:class, Device{
        return (T)new Dictionary<Type, Device>() {
                [typeof(Weapon)] = weapon,
                [typeof(Armor)] = armor,
                [typeof(Shield)] = shield,
                [typeof(Reactor)] = reactor,
                [typeof(Solar)] = solar,
                [typeof(Service)]=service,
        }[typeof(T)];
    }
    public bool Get<T>(out T result) where T : class, Device => (result = Get<T>()) != null;
    public bool Has<T>() where T : class, Device => Get<T>() != null;
    public void Remove<T>() where T : class, Device {
        new Dictionary<Type, Func<Device>>() {
            [typeof(Weapon)] = () => weapon = null,
            [typeof(Armor)] = () => armor = null,
            [typeof(Shield)] = () => shield = null,
            [typeof(Reactor)] = () => reactor = null,
            [typeof(Solar)] = () => solar = null,
            [typeof(Service)] = () => service = null,
        }[typeof(T)]();
    }
    public T Install<T>() where T:class, Device {
        return (T) (new Dictionary<Type, Func<Device>>() {
            [typeof(Weapon)] = () => weapon ??= type.weapon?.GetWeapon(this),
            [typeof(Armor)] = () => armor ??= type.armor?.GetArmor(this),
            [typeof(Shield)] = () => shield ??= type.shield?.GetShield(this),
            [typeof(Reactor)] = () => reactor ??= type.reactor?.GetReactor(this),
            [typeof(Solar)] = () => solar ??= type.solar?.GetSolar(this),
            [typeof(Service)] = () => service ??= type.misc?.GetMisc(this),
        }[typeof(T)]());
    }
    public bool Install<T>(out T result) where T:class, Device {
        return (result = Install<T>()) != null;
    }
    public void RemoveAll() {
        weapon = null;
        armor = null;
        shield = null;
        reactor = null;
        solar = null;
        service = null;
    }
}
public interface Device {
    Item source { get; }
    void Update(IShip owner);
    int? powerUse => null;
    public void OnOverload(PlayerShip owner) { }
    public void OnDisable() { }
}
public static class SWeapon {
    public static void CreateShot(this FragmentDesc fragment, SpaceObject source, double direction) {
        var world = source.world;
        var position = source.position;
        var velocity = source.velocity;
        var angleInterval = fragment.spreadAngle / fragment.count;
        for (int i = 0; i < fragment.count; i++) {
            double angle = direction + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
            var p = new Projectile(source,
                fragment,
                position + XY.Polar(angle, 0.5),
                velocity + XY.Polar(angle, fragment.missileSpeed));
            world.AddEntity(p);
        }
    }
}
public class Weapon : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public WeaponDesc desc;
    [JsonIgnore]
    int? Device.powerUse => (firing || delay > 0) ? desc.powerUse : desc.powerUse / 10;

    public FragmentDesc GetFragmentDesc() {
        var d = desc.shot;
        capacitor?.Modify(ref d);
        source.mod?.ModifyWeapon(ref d);
        return d;
    }
    [JsonIgnore]
    public int missileSpeed => GetFragmentDesc().missileSpeed;
    [JsonIgnore]
    public int currentRange { get {
            var f = GetFragmentDesc();
            return f.missileSpeed * f.lifetime / Program.TICKS_PER_SECOND;
    }}
    public int lifetime => GetFragmentDesc().lifetime;
    [JsonIgnore]
    public int currentRange2 => currentRange * currentRange;
    public Capacitor capacitor;
    public Aiming aiming;
    public IAmmo ammo;
    public int delay;
    public bool firing;
    public int repeatsLeft;
    public Weapon() { }
    public Weapon(Item source, WeaponDesc desc) {
        this.source = source;
        this.desc = desc;
        this.delay = 0;
        firing = false;
        if (desc.capacitor != null) {
            capacitor = new Capacitor(desc.capacitor);
        }
        if (desc.shot.omnidirectional) {
            aiming = new Omnidirectional();
        } else if (desc.shot.maneuver > 0) {
            aiming = new Targeting();
        }
        if (desc.initialCharges > -1) {
            ammo = new ChargeAmmo(desc.initialCharges);
        } else if (desc.ammoType != null) {
            ammo = new ItemAmmo(desc.ammoType);
        }
    }
    public string GetReadoutName() {
        if (ammo is ChargeAmmo c) {
            return $"{source.type.name} [{c.charges}]";
        } else if (ammo is ItemAmmo i) {
            return $"{source.type.name} [{i.count}]";
        }
        return source.type.name;
    }
    public ColoredString GetBar() {
        if (ammo?.AllowFire == false) {
            return new ColoredString(new string(' ', 16), Color.Transparent, Color.Black);
        }

        int fireBar = (int)(16f * (desc.fireCooldown - delay) / desc.fireCooldown);
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
            foreach (var cg in bar.Take((int)n + 1)) {
                cg.Foreground = cg.Foreground.Blend(Color.Cyan.SetAlpha(128));
            }
        }
        return bar;
    }
    public void Update(Station owner) {
        double? direction = null;
        if (aiming != null) {
            aiming.Update(owner, this);
            if (aiming.GetFireAngle(ref direction)) {

            } else if (target != null) {
                Aiming.CalcFireAngle(owner, aiming.target, this, out direction);
            }
        }
        capacitor?.Update();
        if (ammo != null) {
            ammo.Update(owner);
        }
        if (delay > 0 && repeatsLeft == 0) {
            delay--;
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
                delay = desc.fireCooldown;
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
        if (delay > 0 && repeatsLeft == 0) {
            delay--;
        } else {
            bool beginRepeat = true;
            if (repeatsLeft > 0) {
                repeatsLeft--;
                firing = true;
                beginRepeat = false;
            } else if (desc.autoFire) {
                if (desc.targetProjectile) {
                    var target = Aiming.AcquireMissile(owner, this, s => s == null || SShip.IsEnemy(owner, s));
                    if (target != null
                        && Aiming.CalcFireAngle(owner, target, this, out var d)) {
                        direction = d;
                        firing = true;
                    }
                } else if (aiming?.target != null) {
                    firing = true;
                }
            }

            //bool allowFire = firing && (capacitor?.AllowFire ?? true);
            capacitor?.CheckFire(ref firing);
            ammo?.CheckFire(ref firing);

            if (firing) {
                ammo?.OnFire();
                Fire(owner, direction.Value);

                //Apply on next tick (create a delta-momentum variable)
                if (desc.recoil > 0) {
                    owner.velocity += XY.Polar(direction.Value + Math.PI, desc.recoil);
                }

                delay = desc.fireCooldown;
                if (beginRepeat) {
                    repeatsLeft = desc.repeat;
                }
            } else {
                repeatsLeft = 0;
            }
        }
        firing = false;
    }

    public void OnDisable() {
        delay = desc.fireCooldown;
        capacitor?.Clear();
        aiming?.ClearTarget();
    }
    public bool RangeCheck(SpaceObject user, SpaceObject target) {
        return (user.position - target.position).magnitude < currentRange;
    }
    public bool AllowFire => ammo?.AllowFire ?? true;
    public bool ReadyToFire => delay == 0 && (capacitor?.AllowFire ?? true) && (ammo?.AllowFire ?? true);
    public void Fire(SpaceObject owner, double direction) {
        /*
        var damageHP = desc.shot.damageHP.Roll();
        var missileSpeed = desc.shot.missileSpeed;
        var lifetime = desc.shot.lifetime;
        capacitor?.Modify(ref damageHP, ref missileSpeed, ref lifetime);
        source.mod?.ModifyWeapon(ref damageHP, ref missileSpeed, ref lifetime);
        */
        var shotDesc = desc.shot;
        double angleInterval = shotDesc.spreadAngle / shotDesc.count;
        for (int i = 0; i < shotDesc.count; i++) {
            double angle = direction + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
            Projectile p = new Projectile(owner, this,
                owner.position + XY.Polar(angle),
                owner.velocity + XY.Polar(angle, missileSpeed)
                );
            owner.world.AddEntity(p);
        }
        capacitor?.OnFire();
    }

    public SpaceObject target => aiming?.target;
    public void OverrideTarget(SpaceObject target) {

        if (aiming != null) {
            aiming.ClearTarget();
            aiming.UpdateTarget(target);
        }
    }
    public void SetFiring(bool firing = true) => this.firing = firing;

    //Use this if you want to override auto-aim
    public void SetFiring(bool firing = true, SpaceObject target = null) {
        this.firing = firing;
        aiming?.UpdateTarget(target);
    }

}

public class Launcher : Device {
    public LauncherDesc desc;

    public Weapon weapon;
    public int index;
    [JsonIgnore]
    public Item source => weapon.source;
    [JsonIgnore]
    public LaunchDesc fragmentDesc => desc.missiles[index];
    [JsonIgnore]
    int? Device.powerUse => ((Device)weapon).powerUse;
    public FragmentDesc GetFragmentDesc() => weapon.GetFragmentDesc();
    [JsonIgnore]
    public int missileSpeed => GetFragmentDesc().missileSpeed;
    [JsonIgnore]
    public int currentRange => GetFragmentDesc().range;
    [JsonIgnore]
    public int lifetime => GetFragmentDesc().lifetime;
    [JsonIgnore]
    public int currentRange2 => currentRange * currentRange;
    [JsonIgnore]
    public Capacitor capacitor => weapon.capacitor;
    [JsonIgnore]
    public Aiming aiming => weapon.aiming;
    [JsonIgnore]
    public IAmmo ammo => weapon.ammo;
    [JsonIgnore]
    public int delay => weapon.delay;
    [JsonIgnore] 
    public bool firing => weapon.firing;
    [JsonIgnore] 
    public int repeatsLeft => weapon.repeatsLeft;
    public Launcher() { }
    public Launcher(Item source, LauncherDesc desc) {
        this.weapon = desc.GetWeapon(source);
        this.desc = desc;
    }
    public void SetMissile(int index) {
        this.index = index;
        var l = desc.missiles[index];
        weapon.ammo = new ItemAmmo(l.ammoType);
        weapon.desc.shot = l.shot;
    }
    public string GetReadoutName() => weapon.GetReadoutName();
    public ColoredString GetBar() => weapon.GetBar();
    public void Update(Station owner) => weapon.Update(owner);
    public void Update(IShip owner) => weapon.Update(owner);
    public void OnDisable() => weapon.OnDisable();
    public bool RangeCheck(SpaceObject user, SpaceObject target) => weapon.RangeCheck(user, target);
    public bool AllowFire => weapon.AllowFire;
    public bool ReadyToFire => weapon.ReadyToFire;
    public void Fire(SpaceObject owner, double direction) => weapon.Fire(owner, direction);
    public SpaceObject target => weapon.target;
    public void OverrideTarget(SpaceObject target) => weapon.OverrideTarget(target);
    public void SetFiring(bool firing = true) => weapon.SetFiring(firing);
    public void SetFiring(bool firing = true, SpaceObject target = null) => weapon.SetFiring(firing, target);

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
        charge = Math.Min(desc.maxCharge, charge + desc.rechargePerTick);
    }

    public FragmentDesc Modify(FragmentDesc fd) {
        return fd with {
            damageHP = new DiceInc(fd.damageHP, (int)(desc.bonusDamagePerCharge * charge)),
            missileSpeed = fd.missileSpeed + (int)(desc.bonusSpeedPerCharge * charge),
            lifetime = fd.lifetime + (int)(desc.bonusLifetimePerCharge * charge)
        };
    }
    public void Modify(ref FragmentDesc fd) {
        fd = fd with {
            damageHP = new DiceInc(fd.damageHP, (int)(desc.bonusDamagePerCharge * charge)),
            missileSpeed = fd.missileSpeed + (int)(desc.bonusSpeedPerCharge * charge),
            lifetime = fd.lifetime + (int)(desc.bonusLifetimePerCharge * charge)
        };
    }


    public void OnFire() {
        charge = Math.Max(0, charge - desc.dischargeOnFire);
    }
    public void Clear() => charge = 0;
}
public interface Aiming {
    public SpaceObject target { get; }
    void Update(Station owner, Weapon weapon);
    void Update(IShip owner, Weapon weapon);
    bool GetFireAngle(ref double? direction) {
        return false;
    }
    void ClearTarget() { }
    void UpdateTarget(SpaceObject target = null) { }

    static bool CalcFireAngle(MovingObject owner, MovingObject target, Weapon weapon, out double? result) {
        if (((target.position - owner.position).magnitude < weapon.currentRange)) {
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
        if (velDiff.magnitude < missileSpeed) {
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
    public void ClearTarget() => target = null;
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

        void UpdateDirection() {
            if (Aiming.CalcFireAngle(owner, target, weapon, out direction)) {
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
        if (this.direction != null) {
            direction = this.direction.Value;
            return true;
        }
        return false;
    }
    public void ClearTarget() => target = null;
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
    public HashSet<Item> inventory;
    public Item unit;
    public bool AllowFire => unit != null;

    public int count;
    public int ticks;
    public ItemAmmo(ItemType itemType) {
        this.itemType = itemType;
    }
    public void Update(IShip source) {
        Update(source.cargo);
    }
    public void Update(Station source) {
        Update(source.cargo);
    }
    public void Update(HashSet<Item> inventory) {
        ticks++;
        if (ticks % 10 != 0) {
            return;
        }
        this.inventory = inventory;
        UpdateUnit();
    }
    public void UpdateUnit() {
        var units = inventory.Where(i => i.type == itemType);
        unit = units.FirstOrDefault();
        count = inventory.Count(i => i.type == itemType);
    }
    public void OnFire() {
        inventory.Remove(unit);
        UpdateUnit();
    }
}
/*
public class MultiItemAmmo : IAmmo {
    public int index;
    public List<IAmmo> missiles;
    public IAmmo current => missiles[index];
    public bool AllowFire => current.AllowFire;
    public MultiItemAmmo(List<IAmmo> missiles) {
        this.missiles = missiles;
    }
    public void Update(IShip source) => current.Update(source);
    public void Update(Station source) => current.Update(source);

    public void OnFire() => current.OnFire();
}
*/

public class Armor : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ArmorDesc desc;
    public int hp;
    public int lastDamageTick;
    public Armor() { }
    public Armor(Item source, ArmorDesc desc) {
        this.source = source;
        this.desc = desc;
        this.hp = desc.maxHP;
    }
    public void Update(IShip owner) { }
}
public class Shield : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ShieldDesc desc;
    public int hp;
    public double regenHP;
    public int delay;
    public double absorbFactor => desc.absorbFactor;
    public int maxAbsorb => hp;
    /*
    public int maxAbsorb => desc.absorbMaxHP == -1 ?
        hp : Math.Min(hp, absorbHP);
    public int absorbHP;
    public double absorbRegenHP;
    */

    int? Device.powerUse => hp < desc.maxHP ? desc.powerUse : desc.idlePowerUse;
    public Shield() { }
    public Shield(Item source, ShieldDesc desc) {
        this.source = source;
        this.desc = desc;
    }

    public void OnDisable() {
        hp = 0;
        regenHP = 0;
        delay = desc.depletionDelay;
    }
    public void Update(IShip owner) {
        if (delay > 0) {
            delay--;
        } else {
            regenHP += desc.regen;
            while (regenHP >= 1) {
                if (hp < desc.maxHP) {
                    hp++;
                    regenHP--;
                } else {
                    regenHP = 0;
                }
            }
            /*
            absorbRegenHP += desc.absorbRegen;
            while(absorbRegenHP >= 1) {
                if(absorbHP < desc.absorbMaxHP) {
                    absorbHP++;
                    absorbRegenHP--;
                } else {
                    absorbRegenHP = 0;
                }
            }
            */
        }
    }
    public void Absorb(int damage) {
        hp = Math.Max(0, hp - damage);
        //absorbHP = Math.Max(0, absorbHP - damage);
        delay = (hp == 0 ? desc.depletionDelay : desc.damageDelay);
    }
}
public interface PowerSource {
    double energyDelta { get; set; }
    int maxOutput { get; }
}
public class Reactor : Device, PowerSource {
    [JsonProperty]
    public Item source { get; set; }
    public ReactorDesc desc;
    public double energy;
    [JsonProperty]
    public double energyDelta { get; set; }
    public int rechargeDelay;
    public int maxOutput => energy > 0 ? desc.maxOutput : 0;
    public Reactor() { }
    public Reactor(Item source, ReactorDesc desc) {
        this.source = source;
        this.desc = desc;
        energy = desc.capacity;
        energyDelta = 0;
    }
    public void Update(IShip owner) {
        energy = Math.Max(0, Math.Min(
            energy + (energyDelta < 0 ? energyDelta / desc.efficiency : energyDelta) / 30,
            desc.capacity));
    }
}
public class Solar : Device, PowerSource {
    [JsonProperty]
    public Item source { get; private set; }
    public SolarDesc desc;
    public int lifetimeOutput;
    public bool dead;
    [JsonProperty]
    public int maxOutput { get; private set; }
    [JsonProperty]
    public double energyDelta { get; set; }
    public Solar() { }
    public Solar(Item source, SolarDesc desc) {
        this.source = source;
        this.desc = desc;
        lifetimeOutput = desc.lifetimeOutput;
    }
    public void Update(IShip owner) {
        void Update() {
            var t = owner.world.backdrop.starlight.GetTile(owner.position);
            var b = t.A;
            maxOutput = (b * desc.maxOutput / 255);
        }
        switch (lifetimeOutput) {
            case -1: 
                Update();
                break;
            case 0: 
                break;
            case 1:
                lifetimeOutput = 0;
                maxOutput = 0;
                if (owner is PlayerShip ps) {
                    ps.AddMessage(new Message($"{source.name} has stopped functioning"));
                }
                break;
            default:
                lifetimeOutput = (int)Math.Max(1, lifetimeOutput + energyDelta);
                Update();
                break;

        }

    }
}
public class Service : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ServiceDesc desc;
    public int ticks;
    [JsonProperty]
    public int powerUse { get; private set; }
    int? Device.powerUse => powerUse;
    public Service() { }
    public Service(Item source, ServiceDesc desc) {
        this.source = source;
        this.desc = desc;
        powerUse = 0;
    }
    public void Update(IShip owner) {
        ticks++;
        if (ticks % desc.interval == 0) {
            var powerUse = 0;
            switch (desc.type) {
                case ServiceType.missileJack: {
                        //May not work in Arena mode if we assume control
                        //bc weapon locks are focused on the old AI ship
                        var missile = owner.world.entities.all
                            .OfType<Projectile>()
                            .FirstOrDefault(
                                p => (owner.position - p.position).magnitude < 24
                                  && p.maneuver != null
                                  && p.maneuver.maneuver > 0
                                  && Equals(p.maneuver.target, owner)
                                );
                        if (missile != null) {
                            missile.maneuver.target = missile.source;
                            missile.source = owner;
                            var offset = (missile.position - owner.position);
                            var dist = offset.magnitude;
                            var inc = offset.normal;
                            for (var i = 0; i < dist; i++) {
                                var p = owner.position + inc * i;
                                owner.world.AddEffect(new EffectParticle(p, new ColoredGlyph(Color.Orange, Color.Transparent, '-'), 10));
                            }
                            powerUse = desc.powerUse;
                        }
                        break;
                    }
                case ServiceType.armorRepair: {
                        break;
                    }
                case ServiceType.grind:
                    if(owner is PlayerShip player) {
                        powerUse = this.powerUse + (player.energy.totalOutputMax - player.energy.totalOutputUsed);
                    }
                    break;
            }
            this.powerUse = powerUse;
        }
    }
    void Device.OnOverload(PlayerShip owner) {
        powerUse = owner.energy.totalOutputLeft;
    }
}