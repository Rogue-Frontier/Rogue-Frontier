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
    
    public Armor armor;
    public Engine engine;
    public Reactor reactor;
    public Service service;
    public Shield shield;
    public Solar solar;
    public Weapon weapon;

    public Modifier mod;

    public Item() { }
    public Item(Item copy) {
        type = copy.type;
        weapon = copy.weapon?.Copy(this);
        armor = copy.armor?.Copy(this);
        shield = copy.shield?.Copy(this);
        reactor = copy.reactor?.Copy(this);
        solar = copy.solar.Copy(this);
        service = copy.service.Copy(this);
        mod = copy.mod with { };
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
                [typeof(Armor)] = armor,
                [typeof(Engine)] = engine,
                [typeof(Reactor)] = reactor,
                [typeof(Service)]= service,
                [typeof(Shield)] = shield,
                [typeof(Solar)] = solar,
                [typeof(Weapon)] = weapon,
        }[typeof(T)];
    }
    public bool Get<T>(out T result) where T : class, Device => (result = Get<T>()) != null;
    public bool Has<T>() where T : class, Device => Get<T>() != null;
    public void Remove<T>() where T : class, Device {
        new Dictionary<Type, Func<Device>>() {
            [typeof(Armor)] = () => armor = null,
            [typeof(Engine)] = () => engine = null,
            [typeof(Reactor)] = () => reactor = null,
            [typeof(Service)] = () => service = null,
            [typeof(Shield)] = () => shield = null,
            [typeof(Solar)] = () => solar = null,
            [typeof(Weapon)] = () => weapon = null,
        }[typeof(T)]();
    }
    public T Install<T>() where T:class, Device {
        return (T) (new Dictionary<Type, Func<Device>>() {
            [typeof(Armor)] = () => armor ??= type.armor?.GetArmor(this),
            [typeof(Engine)] = () => engine = type.engine?.GetEngine(this),
            [typeof(Reactor)] = () => reactor ??= type.reactor?.GetReactor(this),
            [typeof(Service)] = () => service ??= type.service?.GetService(this),
            [typeof(Shield)] = () => shield ??= type.shield?.GetShield(this),
            [typeof(Solar)] = () => solar ??= type.solar?.GetSolar(this),
            [typeof(Weapon)] = () => weapon ??= type.weapon?.GetWeapon(this),
        }[typeof(T)]());
    }
    public bool Install<T>(out T result) where T:class, Device {
        return (result = Install<T>()) != null;
    }
    public void RemoveAll() {
        armor = null;
        engine = null;
        reactor = null;
        service = null;
        shield = null;
        solar = null;
        weapon = null;
    }
}
public interface Device {
    Item source { get; }
    void Update(IShip owner);
    int? powerUse => null;
    public bool IsEnabled(IShip owner) =>
        (owner as PlayerShip)?.energy.off.Contains(this) != false;
    public void OnOverload(PlayerShip owner) { }
    public void OnDisable() { }
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
public interface PowerSource {
    double energyDelta { get; set; }
    int maxOutput { get; }
}
public class Armor : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ArmorDesc desc;
    public int hp;
    public double hpToRecover;
    public double recoveryHP;
    public double regenHP;
    public double decayHP;

    public int killHP;

    public int damageDelay;

    public double stealth => desc.stealth == 0 ? 0 : damageDelay > 0 ? 0 : desc.stealth * hp / desc.maxHP;

    public int lifetimeDamageAbsorbed;
    public int lastDamageTick;

    List<Decay> decay=new();
    public class Decay {
        public int lifetime;
        public double rate;
        public Decay(int lifetime, double rate) => (this.lifetime, this.rate) = (lifetime, rate);
    }
    public Armor() { }
    public Armor(Item source, ArmorDesc desc) {
        this.source = source;
        this.desc = desc;
        this.hp = desc.maxHP;
    }
    public Armor Copy(Item source) => desc.GetArmor(source);
    public void Update(IShip owner) {
        if (decay.Any()) {
            foreach (var d in decay) {
                decayHP += d.rate;
                d.lifetime--;
            }
            decay.RemoveAll(r => r.lifetime == 0);
            if (decayHP >= 1) {
                var delta = Math.Min(hp, (int)decayHP);
                hp -= delta;
                decayHP = 0;
                lastDamageTick = owner.world.tick;

                lifetimeDamageAbsorbed += delta;
                hpToRecover += (delta * desc.recoveryFactor);
                damageDelay = 30;
            }
        }
        if (damageDelay > 0) {
            damageDelay--;
            return;
        }
        if (hpToRecover >= 1) {
            recoveryHP += desc.recoveryRate;
            while (recoveryHP >= 1) {
                if (hp < desc.maxHP) {
                    hp++;
                    recoveryHP--;
                    hpToRecover--;
                } else {
                    recoveryHP = 0;
                    hpToRecover = 0;
                }
            }
        }
        regenHP += desc.regenRate;
        while (regenHP >= 1) {
            if(hp < desc.maxHP) {
                hp++;
                regenHP--;
            } else {
                regenHP = 0;
            }
        }
        if (hp > 0 && killHP < desc.killHP) {
            killHP = desc.killHP;
        }
    }
    private void OnAbsorb(int absorbed) {
        lifetimeDamageAbsorbed += absorbed;
        hpToRecover += (absorbed * desc.recoveryFactor);
        damageDelay = 30;
    }
    public void Absorb(int amount) {
        if(hp == 0 || amount < 1) {
            return;
        }
        //Check if we have a kill threshold
        if (hp <= killHP) {
            if(amount < killHP) {
                //Remember this but take no damage
                OnAbsorb(amount);
            } else {
                //Otherwise, we fall
                hp = 0;
                OnAbsorb(killHP);
            }
            return;
        }
        var absorbed = Math.Min(hp, amount);
        hp -= absorbed;
        OnAbsorb(absorbed);
        if (killHP > 0 && absorbed > killHP) {
            killHP = 0;
        }
    }
    public int Absorb(Projectile p) {
        if (hp == 0 || p.damageHP < 1)
            return 0;
        //If we're below the drill threshold, then skip this armor
        if(((float)hp / desc.maxHP) < p.fragment.drillFactor) {
            return 0;
        }
        //Check if we have a kill threshold
        if (hp <= killHP) {
            if (p.damageHP < killHP) {
                var amount = p.damageHP;
                p.damageHP = 0;

                //Remember this but take no damage
                OnAbsorb(amount);
                return amount;
            } else {
                p.damageHP -= killHP;
                lastDamageTick = p.world.tick;

                //Otherwise, we fall
                hp = 0;
                OnAbsorb(killHP);
                return killHP;
            }
        }
        var absorbed = Math.Min(hp, p.damageHP);
        hp -= absorbed;
        OnAbsorb(absorbed);
        if (killHP > 0 && absorbed > killHP) {
            killHP = 0;
        }
        lastDamageTick = p.world.tick;
        p.damageHP -= absorbed;
        if (p.fragment.decay is Decay d) {
            decay.Add(new(d.lifetime, d.rate));
        }
        return absorbed;
    }
}
public class Engine : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public EngineDesc desc;
    public bool thrusting;
    public Engine() { }
    public Engine(Item source, EngineDesc desc) {
        this.source = source;
        this.desc = desc;
    }
    public Engine Copy(Item source) => desc.GetEngine(source);
    public void Update(IShip owner) {
        var rotationDeg = owner.rotationDeg;
        var ship = (owner is PlayerShip ps ? ps.ship : owner is AIShip a ? a.ship : null);
        var sc = ship.shipClass;
        UpdateThrust();
        UpdateTurn();
        UpdateRotation();
        UpdateBrake();
        void UpdateThrust() {
            if (thrusting) {
                var rotationRads = rotationDeg * Math.PI / 180;

                var exhaust = new EffectParticle(ship.position + XY.Polar(rotationRads, -1),
                    ship.velocity + XY.Polar(rotationRads, -sc.thrust),
                    new ColoredGlyph(Color.Yellow, Color.Transparent, '.'),
                    4);
                ship.world.AddEffect(exhaust);

                ship.velocity += XY.Polar(rotationRads, sc.thrust);
                if (ship.velocity.magnitude > ship.shipClass.maxSpeed) {
                    ship.velocity = ship.velocity.normal * sc.maxSpeed;
                }

                thrusting = false;
            }
        }
        void UpdateTurn() {
            if (ship.rotating != Rotating.None) {
                ref var rv = ref ship.rotatingVel;
                if (ship.rotating == Rotating.CCW) {
                    /*
                    if (rotatingSpeed < 0) {
                        rotatingSpeed += Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if (rv < 0) {
                        Decel();
                    }
                    rv += sc.rotationAccel / Program.TICKS_PER_SECOND;
                } else if (ship.rotating == Rotating.CW) {
                    /*
                    if(rotatingSpeed > 0) {
                        rotatingSpeed -= Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if (rv > 0) {
                        Decel();
                    }
                    rv -= sc.rotationAccel / Program.TICKS_PER_SECOND;
                }
                rv = Math.Min(Math.Abs(rv), sc.rotationMaxSpeed) * Math.Sign(rv);
                ship.rotating = Rotating.None;
            } else {
                Decel();
            }
            void Decel() => ship.rotatingVel -= Math.Min(Math.Abs(ship.rotatingVel), sc.rotationDecel / Program.TICKS_PER_SECOND) * Math.Sign(ship.rotatingVel); ;
        }
        void UpdateRotation() => ship.rotationDeg += ship.rotatingVel;
        void UpdateBrake() {
            if (ship.decelerating) {
                if (ship.velocity.magnitude > 0.05) {
                    ship.velocity -= ship.velocity.normal * Math.Min(ship.velocity.magnitude, sc.thrust / 2);
                } else {
                    ship.velocity = new XY();
                }
                ship.decelerating = false;
            }
        }
    }
}
public class Enhancer : Device {
    [JsonProperty]
    public Item source { get; set; }
    public EnhancerDesc desc;
    public int powerUse => desc.powerUse;
    public Enhancer() { }
    public Enhancer(Item source, EnhancerDesc desc) {
        this.source = source;
        this.desc = desc;
    }
    public Enhancer Copy(Item source) => desc.GetEnhancer(source);
    public void Update(IShip owner) {
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
    public Launcher Copy(Item source) => desc.GetLauncher(source);
    public void SetMissile(int index) {
        this.index = index;
        var l = desc.missiles[index];
        weapon.ammo = new ItemAmmo(l.ammoType);
        weapon.desc.projectile = l.shot;
    }
    public string GetReadoutName() => weapon.GetReadoutName();
    public ColoredString GetBar(int BAR) => weapon.GetBar(BAR);
    public void Update(Station owner) => weapon.Update(owner);
    public void Update(IShip owner) => weapon.Update(owner);
    public void OnDisable() => weapon.OnDisable();
    public bool RangeCheck(ActiveObject user, ActiveObject target) => weapon.RangeCheck(user, target);
    public bool AllowFire => weapon.AllowFire;
    public bool ReadyToFire => weapon.ReadyToFire;
    public void Fire(ActiveObject owner, double direction) => weapon.Fire(owner, direction);
    public ActiveObject target => weapon.target;
    public void OverrideTarget(ActiveObject target) => weapon.SetTarget(target);
    public void SetFiring(bool firing = true) => weapon.SetFiring(firing);
    public void SetFiring(bool firing = true, ActiveObject target = null) => weapon.SetFiring(firing, target);
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
    public double lifetimeEnergyUsed;
    public Reactor() { }
    public Reactor(Item source, ReactorDesc desc) {
        this.source = source;
        this.desc = desc;
        energy = desc.capacity;
        energyDelta = 0;
    }
    public Reactor Copy(Item source) => desc.GetReactor(source);
    public void Update(IShip owner) {
        var e = energy;
        energy = Math.Max(0, Math.Min(
            energy + (energyDelta < 0 ? energyDelta / desc.efficiency : energyDelta) / 30,
            desc.capacity));
        if(e > energy) {
            lifetimeEnergyUsed += e - energy;
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
    public Service Copy(Item source) => desc.GetService(source);
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
                    if (owner is PlayerShip player) {
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
public class Shield : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ShieldDesc desc;
    public int hp;
    public double regenHP;
    public int delay;
    public double absorbFactor => desc.absorbFactor;
    public int maxAbsorb => hp;
    public int lifetimeDamageAbsorbed;

    public int stealth => desc.stealth == 0 ? 0 :
        delay > 0 ? 0 :
        desc.stealth * hp / desc.maxHP;
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
    public Shield Copy(Item source) => desc.GetShield(source);
    public void OnDisable() => Deplete();
    public void Deplete() {

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
    public void Absorb(Projectile p) {
        var absorbed = (int)Math.Clamp(p.damageHP * (1 - p.fragment.shieldPass) * absorbFactor, 0, maxAbsorb);
        if (absorbed > 0) {
            hp -= absorbed;
            lifetimeDamageAbsorbed += absorbed;
            delay = (hp == 0 ? desc.depletionDelay : desc.damageDelay);
            p.damageHP -= absorbed;
        }
    }
}
public class Solar : Device, PowerSource {
    [JsonProperty]
    public Item source { get; private set; }
    public SolarDesc desc;
    public int durability;
    [JsonProperty]
    public int maxOutput { get; private set; }
    [JsonProperty]
    public double energyDelta { get; set; }
    public Solar() { }
    public Solar(Item source, SolarDesc desc) {
        this.source = source;
        this.desc = desc;
        durability = desc.durability;
    }
    public Solar Copy(Item source) => desc.GetSolar(source);
    public void Update(IShip owner) {
        void Update() {
            var t = owner.world.backdrop.starlight.GetTile(owner.position);
            var b = t.A;
            maxOutput = (b * desc.maxOutput / 255);
        }
        switch (durability) {
            case -1:
                Update();
                break;
            case 0:
                break;
            case 1:
                durability = 0;
                maxOutput = 0;
                if (owner is PlayerShip ps) {
                    ps.AddMessage(new Message($"{source.name} has stopped functioning"));
                }
                break;
            case > 1:
                durability = (int)Math.Max(1, durability + energyDelta);
                Update();
                break;
            default: throw new Exception($"Invalid durability value {durability}");
        }
    }
}
public class Weapon : Device, IContainer<Projectile.OnHitActive> {
    [JsonProperty]
    public Item source { get; private set; }
    public WeaponDesc desc;
    [JsonIgnore]
    int? Device.powerUse => (firing || delay > 0 || capacitor?.full == false) ? desc.powerUse : 0;
    public FragmentDesc projectileDesc;
    public Capacitor capacitor;
    public Aiming aiming;
    public IAmmo ammo;
    public Modifier mod;
    public int delay;
    public bool firing;
    public int repeatsLeft;
    public double angle;
    public bool blind;
    public XY offset=new(0,0);

    public delegate void OnFire(Weapon w, List<Projectile> p);
    public FuncSet<IContainer<OnFire>> onFire=new();
    public Weapon() { }
    public Weapon(Item source, WeaponDesc desc) {
        this.source = source;
        SetWeaponDesc(desc);
    }
    public void SetWeaponDesc(WeaponDesc desc) {
        this.desc = desc;
        if (desc.capacitor != null) {
            capacitor = new(desc.capacitor);
        }
        if (desc.projectile.omnidirectional) {
            aiming = new Omnidirectional();
        } else if (desc.projectile.acquireTarget) {
            aiming = new Targeting();
        }
        if (desc.initialCharges > -1) {
            ammo = new ChargeAmmo(desc.initialCharges);
        } else if (desc.ammoType != null) {
            ammo = new ItemAmmo(desc.ammoType);
        }
        UpdateProjectileDesc();
    }
    public Weapon Copy(Item source) => desc.GetWeapon(source);
    public string GetReadoutName() {
        string name = source.type.name;
        return ammo switch {
            ChargeAmmo c => $"[{c.charges, 4}] {name}",
            ItemAmmo i => $"[{i.count, 4}] {name}",
            _ => name
        };
    }
    public ColoredString GetBar(int BAR) {
        if (ammo?.AllowFire == false) {
            return new(new(' ', BAR), Color.Transparent, Color.Black);
        }
        var fireBar = (int)(BAR * (double)(desc.fireCooldown - delay) / desc.fireCooldown);
        ColoredString bar;
        if (capacitor != null && capacitor.desc.minChargeToFire > 0) {
            var chargeBar = (int)(BAR * Math.Min(1, capacitor.charge / capacitor.desc.minChargeToFire));
            bar = new ColoredString(new('>', chargeBar), Color.Gray, Color.Black)
                + new ColoredString(new(' ', BAR - chargeBar), Color.Transparent, Color.Black);
        } else {
            bar = new(new('>', BAR), Color.Gray, Color.Black);
        }
        foreach (var cg in bar.Take(fireBar)) {
            cg.Foreground = Color.White;
        }
        if (capacitor != null) {
            var n = BAR * capacitor.charge / capacitor.desc.maxCharge;
            foreach (var cg in bar.Take((int)n + 1)) {
                cg.Foreground = cg.Foreground.Blend(Color.Cyan.SetAlpha(128));
            }
        }
        return bar;
    }
    public void UpdateProjectileDesc() {
        projectileDesc = Modifier.Sum(capacitor?.mod, source.mod, mod) * desc.projectile;
    }
    public void Update(Station owner) {
        UpdateProjectileDesc();
        Heading.AimLine(owner.world, owner.position + offset, angle);

        double? direction = null;
        var hasAimTarget = false;
        if (blind) {
            blind = false;
        } else if (aiming != null) {
            aiming.Update(owner, this);
            hasAimTarget = aiming.target != null && (aiming.target.position - owner.position).magnitude2 < projectileDesc.range2;
            if (hasAimTarget) {
                direction = aiming.GetFireAngle() ?? angle;
            }
        }
        capacitor?.Update();
        if (delay > 0 && repeatsLeft == 0) {
            delay--;
        } else {
            //Stations always fire for now
            firing = true;

            bool beginRepeat = true, endRepeat = true;
            if (repeatsLeft > 0) {
                repeatsLeft--;
                firing = true;
                beginRepeat = false;
                endRepeat = repeatsLeft == 0;
            } else if (desc.autoFire) {
                firing = CheckProjectile() || CheckSpray() || hasAimTarget;
                bool CheckProjectile() {
                    if (desc.targetProjectile && !blind && Aiming.AcquireMissile(owner, this, s => SStation.IsEnemy(owner, s)) is Projectile target) {
                        direction = Omnidirectional.GetFireAngle(owner, target, this);
                        return true;
                    }
                    return false;
                }
                bool CheckSpray() {
                    direction = desc.spray ? aiming switch {
                        Omnidirectional => new Random().NextDouble() * 2 * Math.PI,
                        Swivel s => angle + new Random().NextDouble() * (s.leftRange + s.rightRange) - s.leftRange,
                        _ => angle
                    } : direction;
                    return desc.spray;
                }
            }


            if (!direction.HasValue) {
                goto Cancel;
            }

            bool clear = true;
            var d = XY.Polar(direction.Value);
            var p = owner.position;
            for (int i = 0; i < projectileDesc.range; i++) {
                p += d;
                foreach (var other in owner.world.entities[p].Select(s => s is ISegment seg ? seg.parent : s).Distinct()) {
                    if(other == owner)
                        continue;
                    if (other == aiming?.target)
                        goto LineCheckDone;
                    switch (other) {
                        case AIShip ai when owner.guards.Contains(ai):
                            continue;
                        case ActiveObject a when owner.CanTarget(a):
                            goto LineCheckDone;
                        case Wreck:
                        case Projectile:
                            continue;
                        default:
                            clear = false;
                            goto LineCheckDone;
                    }
                }
            }
        LineCheckDone:

            firing &= clear;

            if (!firing) {
                goto Cancel;
            }

            //bool allowFire = (firing || true) && (capacitor?.AllowFire ?? true);
            capacitor?.CheckFire(ref firing);

            if (ammo != null) {
                ammo.Update(owner);
                ammo.CheckFire(ref firing);
            }

            if (firing) {
                delay = endRepeat ? desc.fireCooldown : desc.repeatDelay;
                if (beginRepeat) {
                    repeatsLeft = desc.repeat;
                }
                Fire(owner, direction.Value);
                goto Done;
            }
            Cancel:

            repeatsLeft = 0;
        }

        Done:
        firing = false;
    }
    public void Update(IShip owner) {
        UpdateProjectileDesc();
        double direction = owner.rotationRad + angle;

        var hasAimTarget = false;
        if (blind) {
            blind = false;
        } else if (aiming != null) {
            aiming.Update(owner, this);
            hasAimTarget = aiming.target != null && (aiming.target.position - owner.position).magnitude2 < projectileDesc.range2;
            if (hasAimTarget) {
                direction = aiming.GetFireAngle() ?? direction;
            }
        }
        capacitor?.Update();
        if (delay > 0 && repeatsLeft == 0) {
            delay--;
        } else {
            bool beginRepeat = true, endRepeat = true;
            if (repeatsLeft > 0) {
                repeatsLeft--;
                firing = true;
                beginRepeat = false;
                endRepeat = repeatsLeft == 0;
            } else {
                if (desc.autoFire) {
                    firing = CheckProjectile() || CheckSpray() || hasAimTarget;
                    bool CheckProjectile() {
                        if (desc.targetProjectile && !blind && Aiming.AcquireMissile(owner, this, s => s != null && SShip.IsEnemy(owner, s)) is Projectile target) {
                            direction = Omnidirectional.GetFireAngle(owner, target, this);
                            return true;
                        }
                        return false;
                    }
                    bool CheckSpray() {
                        direction = desc.spray ? new Random().NextDouble() * 2 * Math.PI : direction;
                        return desc.spray;
                    }
                }
                /*
                //Shortcut to skip checks
                if (!firing) {
                    goto Cancel;
                }
                */
            }


            //bool allowFire = firing && (capacitor?.AllowFire ?? true);
            capacitor?.CheckFire(ref firing);

            if(ammo != null) {
                ammo.Update(owner);
                ammo.CheckFire(ref firing);
            }

            if (firing) {
                delay = endRepeat ? desc.fireCooldown : desc.repeatDelay;
                if (beginRepeat) {
                    repeatsLeft = desc.repeat;
                }

                Fire(owner, direction);

                //Apply on next tick (create a delta-momentum variable)
                if (desc.recoil > 0) {
                    owner.velocity += XY.Polar(direction + Math.PI, desc.recoil);
                }
                goto Done;
            }
            Cancel:
            repeatsLeft = 0;
        }
        Done:
        firing = false;
    }
    public void OnDisable() {
        delay = desc.fireCooldown;
        capacitor?.Clear();
        aiming?.ClearTarget();
    }
    public bool RangeCheck(ActiveObject user, ActiveObject target) =>
        (user.position - target.position).magnitude < projectileDesc.range;
    public bool AllowFire => ammo?.AllowFire ?? true;
    public bool ReadyToFire => delay == 0 && (capacitor?.AllowFire ?? true) && (ammo?.AllowFire ?? true);
    public void Fire(ActiveObject owner, double direction, List<Projectile> result = null) {
        var projectiles = projectileDesc.GetProjectiles(owner, target, direction, offset);
        projectiles.ForEach(owner.world.AddEntity);
        projectiles.ForEach(p => p.onHitActive += this);
        result?.AddRange(projectiles);
        ammo?.OnFire();
        capacitor?.OnFire();
        onFire.ForEach(f => f(this, projectiles));
        if(owner is PlayerShip p) {
            p.onWeaponFire.ForEach(f => f(p, this, projectiles));
        }
    }
    Projectile.OnHitActive IContainer<Projectile.OnHitActive>.Value => (projectile, hit) => {
        projectile.onHitActive -= this;
        if (projectileDesc.lightning && projectile.hitHull) {
            //delay = 5;
            hit.world.AddEntity(new LightningRod(hit, this));
        }
    };

    public ActiveObject target => aiming?.target;

    public void SetTarget(ActiveObject target) {
        aiming?.SetTarget(target);
    }
    public void SetFiring(bool firing = true) => this.firing = firing;

    //Use this if you want to override auto-aim
    public void SetFiring(bool firing = true, ActiveObject target = null) {
        this.firing = firing;
        aiming?.UpdateTarget(target);
    }
}
public class Capacitor {
    public CapacitorDesc desc;
    public double charge;
    public bool full => charge == desc.maxCharge;
    public Capacitor(CapacitorDesc desc) {
        this.desc = desc;
    }
    public void CheckFire(ref bool firing) => firing = firing && AllowFire;
    public bool AllowFire => desc.minChargeToFire <= charge;
    public void Update() =>
        charge = Math.Min(desc.maxCharge, charge + desc.rechargePerTick);
    public Modifier mod => new() {
        damageHPInc = (int)(desc.bonusDamagePerCharge * charge),
        missileSpeedInc = (int)(desc.bonusSpeedPerCharge * charge),
        lifetimeInc = (int)(desc.bonusLifetimePerCharge * charge)
    };
    /*
    public FragmentDesc Modify(FragmentDesc fd) =>
        fd with {
            damageHP = new DiceInc(fd.damageHP, (int)(desc.bonusDamagePerCharge * charge)),
            missileSpeed = fd.missileSpeed + (int)(desc.bonusSpeedPerCharge * charge),
            lifetime = fd.lifetime + (int)(desc.bonusLifetimePerCharge * charge)
        };
    public void Modify(ref FragmentDesc fd) =>
        fd = fd with {
            damageHP = new DiceInc(fd.damageHP, (int)(desc.bonusDamagePerCharge * charge)),
            missileSpeed = fd.missileSpeed + (int)(desc.bonusSpeedPerCharge * charge),
            lifetime = fd.lifetime + (int)(desc.bonusLifetimePerCharge * charge)
        };
    */
    public void OnFire() =>
        charge = Math.Max(0, charge - desc.dischargeOnFire);
    public void Clear() => charge = 0;
}
public interface Aiming {
    public ActiveObject target { get; }
    void Update(Station owner, Weapon weapon);
    void Update(IShip owner, Weapon weapon);
    double? GetFireAngle() => null;
    void ClearTarget() { }
    void SetTarget(ActiveObject target) { }
    void UpdateTarget(ActiveObject target) { }
    public static ActiveObject AcquireTarget(ActiveObject owner, Weapon weapon, Func<ActiveObject, bool> filter) =>
        owner.world.entities.GetAll(p => (owner.position - p).magnitude2 < weapon.projectileDesc.range2).OfType<ActiveObject>().FirstOrDefault(filter);
    public static Projectile AcquireMissile(ActiveObject owner, Weapon weapon, Func<ActiveObject, bool> filter) =>
        owner.world.entities.all
            .OfType<Projectile>()
            .Where(p => (owner.position - p.position).magnitude2 < weapon.projectileDesc.range2)
            .Where(p => filter(p.source))
            .OrderBy(p => (owner.position - p.position).Dot(p.velocity))
            //.OrderBy(p => (owner.Position - p.Position).Magnitude2)
            .FirstOrDefault();    
}
public class Targeting : Aiming, IDestructionEvents {

    IDestructionEvents.Destroyed IDestructionEvents.Value => (s, d) => {
        if (s == target) {
            target = null;
        }
    };
    public ActiveObject target { get; set; }
    public Targeting() { }
    public void Update(ActiveObject owner, Weapon weapon, Func<ActiveObject, bool> filter) {
        if(owner.world.tick%30 != 0) {
            return;
        }
        if (target?.active == true
            && owner.IsVisible(target)
            && (owner.position - target.position).magnitude2 < weapon.projectileDesc.range2) {
            return;
        }
        switch (target) {
            case Station st:    st.onDestroyed -= this; break;
            case AIShip ai:     ai.onDestroyed -= this; break;
            case PlayerShip ps: ps.onDestroyed -= this; break;
        }
        target = Aiming.AcquireTarget(owner, weapon, filter);
        switch(target) {
            case Station st:    st.onDestroyed += this; break;
            case AIShip ai:     ai.onDestroyed += this; break;
            case PlayerShip ps: ps.onDestroyed += this; break;
        }
    }
    public void Update(Station owner, Weapon weapon) =>
        Update(owner, weapon, other => owner.IsVisible(other) && SStation.IsEnemy(owner, other));
    public void Update(IShip owner, Weapon weapon) =>
        Update(owner, weapon, other => owner.IsVisible(other) && SShip.IsEnemy(owner, other));
    
    public void ClearTarget() => target = null;
    public void SetTarget(ActiveObject target) => this.target = target;
    public void UpdateTarget(ActiveObject target) =>
        this.target = target ?? this.target;
}
public class Omnidirectional : Aiming {

    private Targeting targeting=new();
    public ActiveObject target {
        get => targeting.target;
        set => targeting.target = value;
    }
    double? direction;
    public Omnidirectional() { }
    public static double GetFireAngle(MovingObject owner, MovingObject target, Weapon w) =>
        Helper.CalcFireAngle(target.position - (owner.position + w.offset),
            target.velocity - owner.velocity,
            w.projectileDesc.missileSpeed, out var _);
    public void UpdateDirection(ActiveObject owner, Weapon weapon) {
        if (targeting.target != null) {

            direction = GetFireAngle(owner, target, weapon);
            Heading.AimLine(owner.world, owner.position + weapon.offset, direction.Value);
            Heading.Crosshair(owner.world, target.position);
        } else {
            direction = null;
        }
    }
    public void Update(Station owner, Weapon weapon) {
        targeting.Update(owner, weapon);
        UpdateDirection(owner, weapon);
    }
    public void Update(IShip owner, Weapon weapon) {
        targeting.Update(owner, weapon);
        UpdateDirection(owner, weapon);
    }
    public double? GetFireAngle() => direction;
    public void ClearTarget() => target = null;
    public void SetTarget(ActiveObject target) => this.target = target;
    public void UpdateTarget(ActiveObject target) =>
        this.target = target ?? this.target;
}


public class Swivel : Aiming {
    public double weaponAngle;
    public double leftRange, rightRange;
    private Targeting targeting = new();
    public ActiveObject target {
        get => targeting.target;
        set => targeting.target = value;
    }
    double? direction;
    public Swivel(double range) { leftRange = rightRange = range / 2; }
    public Swivel(double left, double right) => (this.leftRange, this.rightRange) = (left, right);
    public void Update(ActiveObject owner, Weapon weapon, Func<ActiveObject, bool> filter) {
        if (targeting.target != null) {
            direction = Omnidirectional.GetFireAngle(owner, target, weapon);
            Heading.AimLine(owner.world, owner.position + weapon.offset, direction.Value);
            Heading.Crosshair(owner.world, target.position);
        } else {
            direction = null;
        }
    }
    public void Update(Station owner, Weapon weapon) {
        weaponAngle = owner.rotation + weapon.angle;
        targeting.Update(owner, weapon);
        Update(owner, weapon, s => SStation.IsEnemy(owner, s));
    }
    public void Update(IShip owner, Weapon weapon) {
        weaponAngle = owner.rotationRad + weapon.angle;
        targeting.Update(owner, weapon);
        Update(owner, weapon, s => SShip.IsEnemy(owner, s));
    }
    public double? GetFireAngle() => direction;
    public void ClearTarget() => target = null;
    public void SetTarget(ActiveObject target) => this.target = target;
    public void UpdateTarget(ActiveObject target) =>
        this.target = target ?? this.target;
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
    public void OnFire() => charges--;
}
public class ItemAmmo : IAmmo {
    public ItemType itemType;
    public HashSet<Item> inventory;
    public Item unit;
    public bool AllowFire => unit != null;
    public int count;
    public ItemAmmo(ItemType itemType) =>
        this.itemType = itemType;
    public void Update(IShip source) {
        if (source.world.tick % 90 == 0) {
            Update(source.cargo);
        }
    }
    public void Update(Station source) {
        if(source.world.tick%90 == 0) {
            Update(source.cargo);
        }
    }
    public void Update(HashSet<Item> inventory) {
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