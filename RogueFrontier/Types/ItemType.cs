using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SadRogue.Primitives;
using Con = SadConsole.ScreenSurface;
using Newtonsoft.Json;
using SFML.Audio;

namespace RogueFrontier;
public interface ItemUse {
    string GetDesc(PlayerShip player, Item item);
    void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) { }
}
public record DeployShip : ItemUse {
    [Req] public string shipClass;
    public ShipClass shipType;
    public DeployShip() { }
    public DeployShip(TypeCollection tc, XElement e) {
        e.Initialize(this);
        shipType = tc.Lookup<ShipClass>(shipClass);
    }
    public string GetDesc(PlayerShip player, Item item) => $"Deploy {shipType.name}";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        var w = new Wingmate(player);
        var a = new AIShip(
            new(player.world, shipType, player.position),
            player.sovereign,
            behavior:w
            );
        player.onDestroyed += w;
        player.world.AddEntity(a);
        player.world.AddEffect(new Heading(a));
        player.wingmates.Add(a);
        player.AddMessage(new Transmission(a, $"Deployed {shipType.name}"));
        player.cargo.Remove(item);
        callback?.Invoke();
    }
}
public record DeployStation : ItemUse {
    [Req] public string stationType;
    public StationType stationTypeDesc;
    public DeployStation() { }
    public DeployStation(TypeCollection tc, XElement e) {
        e.Initialize(this);
        stationTypeDesc = tc.Lookup<StationType>(stationType);
    }
    public string GetDesc(PlayerShip player, Item item) => $"Deploy {stationTypeDesc.name}";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        var a = new Station(player.world, stationTypeDesc, player.position) { sovereign = player.sovereign };
        player.world.AddEntity(a);
        a.CreateSegments();
        player.AddMessage(new Transmission(a, $"Deployed {stationTypeDesc.name}"));
        player.cargo.Remove(item);
        callback?.Invoke();
    }
}


public record InstallWeapon : ItemUse {
    public string GetDesc(PlayerShip player, Item item) =>
        player.cargo.Contains(item) ? "Install this weapon" : "Remove this weapon";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        if (player.cargo.Contains(item)) {

            if(player.shipClass.restrictWeapon?.Matches(item) == false) {
                player.AddMessage(new Message($"Unable to install weapon (incompatible): {item.type.name}"));
            } else {
                player.AddMessage(new Message($"Installed weapon: {item.type.name}"));

                player.cargo.Remove(item);
                player.devices.Install(item.Get<Weapon>());
            }

        } else {
            player.AddMessage(new Message($"Removed weapon: {item.type.name}"));

            player.devices.Remove(item.weapon);
            player.cargo.Add(item);
        }
        callback?.Invoke();
    }
}
public record RepairArmor : ItemUse {
    [Req] public int repairHP;
    public string GetDesc(PlayerShip player, Item item) => "Repair armor";
    public RepairArmor() { }
    public RepairArmor(XElement e) {
        e.Initialize(this);
    }
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SMenu.RepairArmorFromItem(prev, player, item, this, callback));
    }
}
public record InvokePower : ItemUse {
    [Req] public string powerType;
    [Req] public int charges;
    public PowerType power;
    public InvokePower() { }
    public InvokePower(TypeCollection tc, XElement e) {
        e.Initialize(this);
        power = tc.Lookup<PowerType>(powerType);
    }
    public string GetDesc(PlayerShip player, Item item) =>
        $"Invoke {power.name} ({charges} charges remaining)";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        player.AddMessage(new Message($"Invoked the power of {item.type.name}"));

        charges--;
        if (charges == 0) {
            player.cargo.Remove(item);
        }
        power.Effect.ForEach(e=>e.Invoke(player));
        callback?.Invoke();
    }
}
public record Refuel : ItemUse {
    [Req] public int energy;
    public Refuel() { }
    public Refuel(TypeCollection tc, XElement e) {
        e.Initialize(this);
    }
    public string GetDesc(PlayerShip player, Item item) => "Refuel reactor";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SMenu.RefuelFromItem(prev, player, item, this, callback));
    }
}
public record DepleteTargetShields() : ItemUse {
    public DepleteTargetShields(XElement e) : this() {
        e.Initialize(this);
    }
    public string GetDesc(PlayerShip player, Item item) =>
        player.GetTarget(out var t) ? $"Deplete shields on {t.name}" : "Deplete shields on target";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        if(!player.GetTarget(out var t)) {
            player.AddMessage(new Message($"No target available"));
            return;
        } 
        
        if(!(t is IShip s)) {
            player.AddMessage(new Message($"Target must be a ship"));
            return;
        }

        if (s.devices.Shield.Count == 0) {
            player.AddMessage(new Message($"Target does not have installed shields"));
            return;
        }
        if(!s.devices.Shield.Any(s => s.hp > 0)) {
            player.AddMessage(new Message($"Target does not have active shields"));
            return;
        }
        s.devices.Shield.ForEach(s => s.Deplete());
        player.AddMessage(new Message($"Depleted shields on {s.name}"));

        player.cargo.Remove(item);
        callback?.Invoke();
    }
}
public record ReplaceDevice() : ItemUse {
    public ItemType from, to;
    public ReplaceDevice(TypeCollection tc, XElement e) : this() =>
        (from, to) = (tc.Lookup<ItemType>(e.ExpectAtt("from")), tc.Lookup<ItemType>(e.ExpectAtt("to")));
    public string GetDesc(PlayerShip player, Item item) =>
        $"Replace installed {from.name}";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SMenu.ReplaceDeviceFromItem(prev, player, item, this, callback));

        player.cargo.Remove(item);
        callback?.Invoke();
    }
}
public record RechargeWeapon() : ItemUse {
    public WeaponDesc weaponType;
    [Req] public int charges;
    public RechargeWeapon(TypeCollection tc, XElement e) : this() {
        weaponType = tc.Lookup<ItemType>(e.ExpectAtt(nameof(weaponType))).weapon;
        e.Initialize(this);
    }
    public string GetDesc(PlayerShip player, Item item) =>
        $"Recharged {item.name}";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SMenu.RechargeWeaponFromItem(prev, player, item, this, callback));
        player.cargo.Remove(item);
        callback?.Invoke();
    }
}
public record UnlockPrescience() : ItemUse {
    Power prescience = new Power(new() {
        codename="power_prescience",
        name="PRESCIENCE",
        cooldownTime=9000,
        invokeDelay=90,
        message="You invoked PRESCIENCE!",
        Effect = new()
    });
    public string GetDesc(PlayerShip player, Item item) =>
        $"Read book";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        if (player.powers.Contains(prescience)) {
            player.AddMessage(new Message("You already have PRESCIENCE!"));
        } else {
            player.powers.Add(prescience);
            player.AddMessage(new Message("You have gained PRESCIENCE!"));
        }

        callback?.Invoke();
    }
}
public record ApplyMod() : ItemUse {
    Modifier mod;
    public ApplyMod(XElement e) : this() {
        e.Initialize(this);
        mod = new(e);
    }
    public string GetDesc(PlayerShip player, Item item) =>
        $"Apply modifier to item (shows menu)";
    public void Invoke(Con prev, PlayerShip player, Item item, Action callback = null) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SMenu.SetMod(prev, player, item, mod, callback));
    }
}
public record ItemType : IDesignType {
    [Req] public string codename;
    [Req] public string name;
    [Opt] public string desc = "";
    [Req] public int level;
    [Req] public int mass;
    [Opt] public int value;
    public HashSet<string> attributes;
    public FragmentDesc ammo;
    public ArmorDesc armor;
    public EngineDesc engine;
    public LauncherDesc launcher;
    public ReactorDesc reactor;
    public ServiceDesc service;
    public ShieldDesc shield;
    public SolarDesc solar;
    public WeaponDesc weapon;
    public ItemUse invoke;

    public bool HasAtt(string att) => attributes.Contains(att);
    public T Get<T>() =>
        (T)new Dictionary<Type, object>{
            [typeof(FragmentDesc)] = ammo,
            [typeof(ArmorDesc)] = armor,
            [typeof(EngineDesc)] = engine,
            [typeof(LaunchDesc)] = launcher,
            [typeof(ReactorDesc)] = reactor,
            [typeof(ServiceDesc)] = service,
            [typeof(ShieldDesc)] = shield,
            [typeof(SolarDesc)] = solar,
            [typeof(WeaponDesc)] = weapon,
            [typeof(ItemUse)] = invoke
        }[typeof(T).GetType()];
    public enum EItemUse {
        none,
        fireWeapon,
        deployShip,
        deployStation,
        installWeapon,
        repairArmor,
        invokePower,
        refuel,
        depleteTargetShields,
        replaceDevice,
        unlockPrescience
    }
    public void Initialize(TypeCollection tc, XElement e) {
        attributes = e.TryAtt("attributes").Split(";").ToHashSet();
        e.Initialize(this);
        invoke = e.HasElement("Invoke", out var xmlInvoke) ? ParseInvoke(xmlInvoke) : ParseInvoke(e);
        ItemUse ParseInvoke(XElement e) =>
            e.TryAttEnum(nameof(invoke), EItemUse.none) switch {
                EItemUse.none => null,
                EItemUse.deployShip => new DeployShip(tc, e),
                EItemUse.deployStation => new DeployStation(tc, e),
                EItemUse.installWeapon => new InstallWeapon(),
                EItemUse.repairArmor => new RepairArmor(e),
                EItemUse.invokePower => new InvokePower(tc, e),
                EItemUse.refuel => new Refuel(tc, e),
                EItemUse.depleteTargetShields => new DepleteTargetShields(e),
                EItemUse.replaceDevice => new ReplaceDevice(tc, e),
                EItemUse.unlockPrescience => new UnlockPrescience(),
                _ => null
            };
        foreach (var (tag, action) in new Dictionary<string, Action<XElement>> {
            ["Ammo"] = e =>     ammo = new(e),
            ["Armor"] = e =>    armor = new(e),
            ["Engine"] = e =>   engine = new(e),
            ["Launcher"] = e => launcher = new(tc, e),
            ["Reactor"] = e =>  reactor = new(e),
            ["Service"] = e =>  service = new(e),
            ["Shield"] = e =>   shield = new(e),
            ["Solar"] = e =>    solar = new(e),
            ["Weapon"] = e =>   weapon = new(tc, e),
        }) {
            if (e.HasElement(tag, out var sub)) {
                action(sub);
            }
        }
    }
}
public record ArmorDesc() {
    [Req] public int maxHP;
    [Opt] public int initialHP = -1;
    [Opt] public double recoveryFactor;
    [Opt] public double recoveryRate;
    [Opt] public double regenRate;
    [Opt] public int killHP;
    [Opt] public double stealth;
    [Opt] public double lifetimeDegrade = 1/50.0;
    [Opt] public double reflectFactor;
    [Opt] public IDice minAbsorb = new Constant(0);
    [Opt] public int powerUse = -1;
    [Opt] public int damageGate = -1;
    public TitanDesc titan;
    public ItemFilter restrictRepair;
    public Armor GetArmor(Item i) => new(i, this);
    public ArmorDesc(XElement e) : this() {
        e.Initialize(this);
        restrictRepair = e.HasElement("RestrictRepair", out var xmlRestrictRepair) ?
            new(xmlRestrictRepair) : null;
        titan = e.HasElement("Titan", out var xmlTitan) ? new(xmlTitan) : null;
    }

    public record TitanDesc() {
        [Opt] public double gain = 1.0;
        [Opt] public double factor = 1.0;
        [Opt] public double decay = 1.0;
        [Opt] public double duration = 1.0;
        public TitanDesc(XElement e) : this() => e.Initialize(this);
    }
}
public record EngineDesc {
    [Req] public int powerUse;
    [Req] public double thrust;
    [Req] public double maxSpeed;
    [Req] public double rotationMaxSpeed;
    [Req] public double rotationDecel;
    [Req] public double rotationAccel;
    public Engine GetEngine(Item i) => new(i, this);
    public EngineDesc() { }
    public EngineDesc(XElement e) {
        e.Initialize(this);
    }
}
public record EnhancerDesc {
    [Req] public int powerUse;
    public Modifier mod;
    public Enhancer GetEnhancer(Item i) => new(i, this);
    public EnhancerDesc() { }
    public EnhancerDesc(XElement e) {
        e.Initialize(this);
        mod = new(e);
    }
}
public record LaunchDesc {
    public FragmentDesc shot;
    public ItemType ammoType;
    public LaunchDesc() {}
    public LaunchDesc(TypeCollection types, XElement e) {
        ammoType = types.Lookup<ItemType>(e.ExpectAtt(nameof(ammoType)));
        shot = ammoType.ammo ?? new(e);
    }
}
public record LauncherDesc {
    [Req] public int powerUse;
    [Req] public int fireCooldown;
    [Opt] public int recoil = 0;
    [Opt] public int repeat = 0;
    public CapacitorDesc capacitor;
    public List<LaunchDesc> missiles;
    public Launcher GetLauncher(Item i) => new(i, this);
    public Weapon GetWeapon(Item i) => new(i, weaponDesc);
    public LauncherDesc() { }
    public LauncherDesc(TypeCollection types, XElement e) {
        e.Initialize(this);

        if (e.HasElement("Capacitor", out var xmlCapacitor)) {
            capacitor = new(xmlCapacitor);
        }
        missiles = new();
        if(e.HasElements("Missile", out var xmlMissileArr)) {
            missiles.AddRange(xmlMissileArr.Select(m => new LaunchDesc(types, m)));
        }
    }

    public WeaponDesc weaponDesc => new() {
        powerUse = powerUse,
        fireCooldown = fireCooldown,
        recoil = recoil,
        repeat = repeat,
        projectile = missiles.First().shot,
        initialCharges = -1,
        capacitor = capacitor,
        ammoType = missiles.First().ammoType,
        targetProjectile = false,
        autoFire = false
    };
}
public record WeaponDesc {
    [Req] public int powerUse;
    [Req] public int fireCooldown;
    [Opt] public int recoil = 0;
    [Opt] public int repeat = 0;
    [Opt] public int repeatDelay = 3;
    [Opt] public int repeatDelayEnd = 3;
    [Opt] public double failureRate = 0;
    [Opt] public int initialCharges = -1;

    [Opt] public bool pointDefense;
    public bool targetProjectile;
    [Opt] public bool autoFire;
    [Opt] public bool spray;
    [Opt] public bool structural;
    [Opt] public bool omnidirectional = false;
    [Opt] public double angle = 0, sweep, leftRange, rightRange = 0;

    public ItemType ammoType;
    public FragmentDesc projectile;
    public CapacitorDesc capacitor;
    public SoundBuffer sound;

    public int missileSpeed => projectile.missileSpeed;
    public int damageType => projectile.damageType;
    public IDice damageHP => projectile.damageHP;
    public int lifetime => projectile.lifetime;
    public int minRange => projectile.missileSpeed * projectile.lifetime / (Program.TICKS_PER_SECOND * Program.TICKS_PER_SECOND); //DOES NOT INCLUDE CAPACITOR EFFECTS
    public Weapon GetWeapon(Item i) => new(i, this);
    public WeaponDesc() { }
    public WeaponDesc(TypeCollection types, XElement e) {
        e.Initialize(this);
        angle *= Math.PI / 180;
        sweep *= Math.PI / 180;
        leftRange *= Math.PI / 180;
        rightRange *= Math.PI / 180;
        ammoType = e.TryAtt(nameof(ammoType), out string at) ?
            types.Lookup<ItemType>(at) : null;
        projectile = new(e.ExpectElement("Projectile"));
        capacitor = e.HasElement("Capacitor", out var xmlCapacitor) ?
            new(xmlCapacitor) : null;
        sound = e.TryAtt("sound", out string s) ? new SoundBuffer(s) : null;
        if (pointDefense) {
            projectile.hitProjectile = true;
            targetProjectile = true;
            autoFire = true;
        }
        if (spray) {
            autoFire = true;
        }
    }
}
public record FragmentDesc {
    [Opt] public int count = 1;
    [Opt] public bool? targetLocked;
    [Opt] public bool omnidirectional;
    [Opt] public double spreadAngle;
    [Req] public int missileSpeed;
    [Req] public int damageType;
    [Req] public IDice damageHP;
    [Opt] public double antiReflect;

    [Opt] public double detonateFailChance;

    [Opt] public int knockback;
    [Opt] public int shock = 1;
    [Req] public int lifetime;
    [Opt] public bool passthrough;
    [Opt] public bool precise = true;
    [Opt] public int armorSkip = 0;
    [Opt] public double shieldDrill;
    /// <summary>
    /// If armor integrity ratio is below this amount, then we bypass the armor completely and go to the next layer
    /// </summary>
    [Opt] public double armorDrill;
    [Opt] public double shieldFactor = 1;
    [Opt] public double armorFactor = 1;
    /// <summary>
    /// If true, then the weapon has Targeting
    /// </summary>
    [Opt] public bool acquireTarget;
    [Opt] public bool multiTarget;
    [Opt] public double maneuver;
    [Opt] public double maneuverRadius;
    [Opt] public int detonateRadius;
    [Opt] public int fragmentInterval;
    [Opt] public bool hitSource;
    [Opt] public bool hitProjectile;
    [Opt] public bool hitBarrier = true;
    [Opt] public bool magic;
    [Opt] public IDice blind;
    [Opt] public int ricochet;
    [Opt] public int tracker;
    [Opt] public bool beacon;
    [Opt] public bool hook;
    [Opt] public bool lightning;    //On hit, the projectile attaches an overlay that automatically makes future shots hit instantly
    public FlashDesc flash;
    public Decay decay;
    public DisruptorDesc disruptor;
    public int range => missileSpeed * lifetime / Program.TICKS_PER_SECOND;
    public double angleInterval => spreadAngle / count;
    public int range2 => range * range;
    public HashSet<FragmentDesc> fragments;
    public StaticTile effect;
    public TrailDesc trail;
    public Modifier mod;

    public SoundBuffer detonateSound;
    public FragmentDesc() { }
    public FragmentDesc(XElement e) {
        Initialize(e);
        detonateSound =
            e.TryAtt(nameof(detonateSound), out var s) ?
                new(s) :
            null;
    }
    public void Initialize(XElement e) {
        e.Initialize(this);
        acquireTarget = acquireTarget || maneuver > 0;
        if (e.TryAttBool("spreadOmni")) {
            spreadAngle = (2 * Math.PI) / count;
        } else {
            spreadAngle = e.TryAttDouble(nameof(spreadAngle), count == 1 ? 0 : 3) * Math.PI / 180;
        }
        maneuver *= Math.PI / (180);

        if(e.HasElement("Flash", out var xmlFlash)) {
            flash = new(xmlFlash);
        }
        if(e.HasElement("Decay", out var xmlDecay)) {
            decay = new(xmlDecay);
        }
        fragments = e.HasElements("Fragment", out var fragmentsList) ?
            new(fragmentsList.Select(f => new FragmentDesc(f))) : null;
        disruptor = e.HasElement("Disruptor", out var xmlDisruptor) ?
            new(xmlDisruptor) : null;
        trail = e.HasElement("Trail", out var xmlTrail) ? 
            new(xmlTrail) : null;
        effect = new(e);
    }
    public IEnumerable<double> GetAngles(double direction) =>
        Enumerable.Range(0, count).Select(i => direction + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1));
    public List<Projectile> CreateProjectiles(ActiveObject owner, List<ActiveObject> targets, double direction, XY offset = null, HashSet<Entity> exclude = null /*, int projectilesPerTarget = 0*/) {
        var position = owner.position + (offset ?? new(0, 0));
        var i = 0;
        var adj = count % 2 == 0 ? -angleInterval / 2 : 0;
        var projectiles = new List<Projectile>();
        Func<Maneuver> getManeuver = targets switch {
            { Count: 1 } => () => GetManeuver(targets.First()),
            { Count: > 1 } => () => GetManeuver(targets[i++ % targets.Count]),
            _ => () => null
        };
        var angles = GetAngles(direction + adj);
        /*
        if(projectilesPerTarget > 0 && targets?.Any() == true) {
            angles = angles.Take(projectilesPerTarget * targets.Count);
        }
        */
        projectiles.AddRange(angles.Select(angle =>
            new Projectile(owner, this,
                position + XY.Polar(angle),
                owner.velocity + XY.Polar(angle, missileSpeed),
                angle,
                getManeuver(),
                exclude
                ) { salvo = projectiles }
        ));
        return projectiles;
    }
    public Maneuver GetManeuver(ActiveObject target) =>
        (acquireTarget && target != null) ? new(target, maneuver, maneuverRadius) : null;
}
public record TrailDesc : ITrail {
    [Req] public int lifetime;
    [Req] public char glyph;
    [Req] public Color foreground;
    [Req] public Color background;
    public TrailDesc() { }
    public TrailDesc(XElement e) {
        e.Initialize(this);
    }
    public Effect GetParticle(XY Position) => new FadingTile(Position, new(foreground, background, glyph), lifetime);
}
public record DisruptorDesc {
    bool? thrustMode, turnMode, brakeMode, fireMode;
    public int lifetime;
    public DisruptorDesc() { }
    public DisruptorDesc(XElement e) {
        thrustMode = GetMode(e.TryAtt(nameof(thrustMode), null));
        turnMode = GetMode(e.TryAtt(nameof(turnMode), null));
        brakeMode = GetMode(e.TryAtt(nameof(brakeMode), null));
        fireMode = GetMode(e.TryAtt(nameof(fireMode), null));
        lifetime = e.TryAttInt(nameof(lifetime), 60);
    }
    public Disrupt GetHijack() => new() {
        thrustMode = thrustMode,
        turnMode = turnMode,
        brakeMode = brakeMode,
        fireMode = fireMode,
        ticksLeft = lifetime
    };
    public bool? GetMode(string str) => str switch {
        "on" => true,
        "off" => false,
        "none" => null,
        null => null,
        _ => throw new Exception($"Invalid value {str}")
    };
}
public record CapacitorDesc {
    [Opt] public double minChargeToFire = 0;
    [Req] public double dischargeOnFire,
                        rechargePerTick,
                        maxCharge;
    [Opt] public double bonusSpeedPerCharge,
                        bonusDamagePerCharge,
                        bonusLifetimePerCharge;
    
    public CapacitorDesc() { }
    public CapacitorDesc(XElement e) {
        e.Initialize(this);
    }
}
public record ReactorDesc {
    [Req] public int maxOutput;
    [Req] public int capacity;
    [Opt] public double efficiency = 1;
    [Opt] public double minOutput = 0;
    //[Opt] public double maxInput = 0;
    //public bool isBattery => maxInput > 0;
    [Opt] public bool battery = false;        //If true, then we recharge using power from other reactors when available

    public Reactor GetReactor(Item i) => new(i, this);
    public ReactorDesc() { }
    public ReactorDesc(XElement e) {
        e.Initialize(this);
    }
}
public enum ServiceType {
    missileJack,
    armorRepair,
    grind
}
public record ServiceDesc {
    public ServiceType type;
    [Req] public int powerUse;
    [Req] public int interval;
    public Service GetService(Item i) => new(i, this);
    public ServiceDesc() { }
    public ServiceDesc(XElement e) {
        e.Initialize(this);
        type = e.ExpectAttEnum<ServiceType>(nameof(type));
    }
}
public record ShieldDesc {
    [Req] public int powerUse, idlePowerUse;
    [Req] public int maxHP;
    [Req] public int damageDelay, depletionDelay;
    [Req] public double regen;
    [Opt] public double absorbFactor = 1;
    [Opt] public int stealth;

    [Opt] public double reflectFactor = 0;
    public Shield GetShield(Item i) => new(i, this);
    public ShieldDesc() { }
    public ShieldDesc(XElement e) {
        e.Initialize(this);
    }
}
public record SolarDesc {
    [Req] public int maxOutput;
    [Opt] public int durability = -1 + 0 * 360000;
    public Solar GetSolar(Item i) => new(i, this);
    public SolarDesc() { }
    public SolarDesc(XElement e) {
        e.Initialize(this);
    }
}