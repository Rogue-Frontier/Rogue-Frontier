using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using Newtonsoft.Json;

namespace RogueFrontier;

public enum EInvokeAction {
    none, deployShip, installWeapon, repairArmor, invokePower, refuel
}
public interface IInvokeAction {
    string GetDesc(PlayerShip player, Item item);
    void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) { }
}
public record DeployShip : IInvokeAction {
    [Req] public string shipClass;
    public ShipClass shipType;
    public DeployShip() { }
    public DeployShip(TypeCollection tc, XElement e) {
        e.Initialize(this);
        shipType = tc.Lookup<ShipClass>(shipClass);
    }
    public string GetDesc(PlayerShip player, Item item) => $"Deploy {shipType.name}";
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {

        var w = new Wingmate(player);
        var a = new AIShip(
            new BaseShip(player.world, shipType, player.sovereign, player.position),
            w
            );
        player.onDestroyed += w;
        player.world.AddEntity(a);
        player.wingmates.Add(a);
        player.AddMessage(new Transmission(a, $"Deployed {shipType.name}"));
        player.cargo.Remove(item);
        callback?.Invoke();
    }
    class Avenge : IContainer<BaseShip.Destroyed> {
        AIShip avenger;
        public Avenge(AIShip avenger) {
            this.avenger = avenger;
        }
        [JsonIgnore]
        public BaseShip.Destroyed Value => (s, d, w) => {
            avenger.behavior = new AttackOrder(d);
        };
    }
}
public record InstallWeapon : IInvokeAction {
    public string GetDesc(PlayerShip player, Item item) =>
        player.cargo.Contains(item) ? "Install this weapon" : "Remove this weapon";
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
        if (player.cargo.Contains(item)) {
            player.AddMessage(new Message($"Installed weapon {item.type.name}"));

            player.cargo.Remove(item);
            player.devices.Install(item.InstallWeapon());
        } else {
            player.AddMessage(new Message($"Removed weapon {item.type.name}"));

            player.devices.Remove(item.weapon);
            item.RemoveWeapon();
            player.cargo.Add(item);
        }
        callback?.Invoke();
    }
}
public record RepairArmor : IInvokeAction {
    [Req] public int repairHP;
    public string GetDesc(PlayerShip player, Item item) => "Repair armor";
    public RepairArmor() { }
    public RepairArmor(XElement e) {
        e.Initialize(this);
    }
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SListScreen.RepairArmorScreen(prev, player, item, this, callback));
    }
}
public record InvokePower : IInvokeAction {
    [Req] public string powerType;
    [Req] public int charges;
    public PowerType power;
    public InvokePower() { }
    public InvokePower(TypeCollection tc, XElement e) {
        e.Initialize(this);
        power = tc.Lookup<PowerType>(powerType);
    }
    public string GetDesc(PlayerShip player, Item item) {
        return $"Invoke {power.name} ({charges} charges remaining)";
    }
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
        player.AddMessage(new Message($"Invoked the power of {item.type.name}"));

        charges--;
        if (charges == 0) {
            player.cargo.Remove(item);
        }
        power.Effect.Invoke(player);

        callback?.Invoke();
    }
}
public record Refuel : IInvokeAction {
    public int energy;
    public Refuel() { }
    public Refuel(TypeCollection tc, XElement e) {
        energy = e.ExpectAttInt("energy");
    }
    public string GetDesc(PlayerShip player, Item item) {
        return "Refuel reactor";
    }
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SListScreen.RefuelReactor(prev, player, item, this, callback));
    }
}
public record ItemType : DesignType {
    [Req] public string codename;
    [Req] public string name;
    [Opt] public string desc;
    [Req] public int level;
    [Req] public int mass;
    [Opt<int>(0)] public int value;
    public ArmorDesc armor;
    public WeaponDesc weapon;
    public ShieldDesc shield;
    public ReactorDesc reactor;
    public SolarDesc solar;
    public ServiceDesc misc;
    public IInvokeAction invoke;
    public void Initialize(TypeCollection tc, XElement e) {
        e.Initialize(this);
        invoke = e.TryAttEnum(nameof(invoke), EInvokeAction.none) switch {
            EInvokeAction.none => null,
            EInvokeAction.deployShip => new DeployShip(tc, e),
            EInvokeAction.installWeapon => new InstallWeapon(),
            EInvokeAction.repairArmor => new RepairArmor(e),
            EInvokeAction.invokePower => new InvokePower(tc, e),
            EInvokeAction.refuel => new Refuel(tc, e),
            _ => null
        };
        if (e.HasElement("Weapon", out var xmlWeapon)) {
            weapon = new WeaponDesc(tc, xmlWeapon);
        }
        if (e.HasElement("Armor", out var xmlArmor)) {
            armor = new ArmorDesc(xmlArmor);
        }
        if (e.HasElement("Shield", out var xmlShield)) {
            shield = new ShieldDesc(xmlShield);
        }
        if (e.HasElement("Reactor", out var xmlReactor)) {
            reactor = new ReactorDesc(xmlReactor);
        }
        if (e.HasElement("Solar", out var xmlSolar)) {
            solar = new SolarDesc(xmlSolar);
        }
        if (e.HasElement("Service", out var xmlService)) {
            misc = new ServiceDesc(xmlService);
        }
    }

}
public record ArmorDesc {
    [Req] public int maxHP;
    public Armor GetArmor(Item i) => new(i, this);
    public ArmorDesc() { }
    public ArmorDesc(XElement e) {
        e.Initialize(this);
    }
}
public record WeaponDesc {
    [Req] public int powerUse;
    [Req] public int fireCooldown;
    [Opt<int>(0)] public int recoil;
    [Opt<int>(0)] public int repeat;
    public FragmentDesc shot;
    [Opt<int>(-1)] public int initialCharges;

    public StaticTile effect;
    public CapacitorDesc capacitor;
    public ItemType ammoType;
    public bool targetProjectile;
    public bool autoFire;
    public int missileSpeed => shot.missileSpeed;
    public int damageType => shot.damageType;
    public IDice damageHP => shot.damageHP;
    public int lifetime => shot.lifetime;

    public int minRange => shot.missileSpeed * shot.lifetime / (Program.TICKS_PER_SECOND * Program.TICKS_PER_SECOND); //DOES NOT INCLUDE CAPACITOR EFFECTS
    
    public Weapon GetWeapon(Item i) => new Weapon(i, this);
    public WeaponDesc() { }
    public WeaponDesc(TypeCollection types, XElement e) {
        e.Initialize(this);
        shot = new FragmentDesc(e);

        effect = new StaticTile(e);
        if (e.HasElement("Capacitor", out var xmlCapacitor)) {
            capacitor = new CapacitorDesc(xmlCapacitor);
        }
        if (e.TryAttribute(nameof(ammoType), out string at)) {
            if (!types.itemType.TryGetValue(at, out ammoType)) {
                throw new Exception($"ItemType codename expected: ammoType=\"{at}\" ### {e} ### {e.Parent}");
            }
        }
        if (e.TryAttBool("pointDefense")) {
            targetProjectile = true;
            autoFire = true;
        }
        

    }
}
public record FragmentDesc {
    [Opt<int>(1)]   public int count;
    [Opt]           public bool omnidirectional;
    [Opt]           public bool? targetLocked;
    [Opt]           public double spreadAngle;
    [Req]           public int missileSpeed;
    [Req]           public int damageType;
    [Req]           public IDice damageHP;
    [Opt]           public int knockback;
    [Opt]           public int shock;
    [Req]           public int lifetime;
    [Opt]           public bool passthrough;
    [Opt]           public double maneuver;
    [Opt]           public double maneuverRadius;
    [Opt]           public int fragmentInterval;
    public DisruptorDesc disruptor;
    public HashSet<FragmentDesc> fragments;
    public StaticTile effect;
    public TrailDesc trail;
    public FragmentDesc() { }
    public FragmentDesc(XElement e) {
        e.Initialize(this);
        if (e.TryAttBool("spreadOmni")) {
            spreadAngle = (2 * Math.PI) / count;
        } else {
            spreadAngle = e.TryAttDouble(nameof(spreadAngle), count == 1 ? 0 : 3) * Math.PI / 180;
        }
        maneuver *= Math.PI / (180);
        fragments = new HashSet<FragmentDesc>();
        if (e.HasElements("Fragment", out var fragmentsList)) {
            fragments.UnionWith(fragmentsList.Select(f => new FragmentDesc(f)));
        }
        if (e.HasElement("Disruptor", out var xmlDisruptor)) {
            disruptor = new DisruptorDesc(xmlDisruptor);
        }
        if (e.HasElement("Trail", out var trail)) {
            this.trail = new TrailDesc(trail);
        }
        effect = new StaticTile(e);
    }
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
    public Effect GetTrail(XY Position) => new FadingTile(Position, new ColoredGlyph(foreground, background, glyph), lifetime);
}
public record DisruptorDesc {
    DisruptMode thrustMode, turnMode, brakeMode, fireMode;
    public int lifetime;
    public DisruptorDesc() { }
    public DisruptorDesc(XElement e) {
        thrustMode = GetMode(e.TryAtt(nameof(thrustMode), null));
        turnMode = GetMode(e.TryAtt(nameof(turnMode), null));
        brakeMode = GetMode(e.TryAtt(nameof(brakeMode), null));
        fireMode = GetMode(e.TryAtt(nameof(fireMode), null));
        lifetime = e.TryAttInt(nameof(lifetime), 60);
    }
    public Disrupt GetHijack() => new Disrupt() {
        thrustMode = thrustMode,
        turnMode = turnMode,
        brakeMode = brakeMode,
        fireMode = fireMode,
        ticksLeft = lifetime
    };
    public DisruptMode GetMode(string str) {
        switch (str) {
            case "on":
                return DisruptMode.FORCE_ON;
            case "off":
                return DisruptMode.FORCE_OFF;
            case "none":
            case null:
                return DisruptMode.NONE;
            default:
                throw new Exception($"Invalid value {str}");

        }
    }
}
public record CapacitorDesc {
    [Opt<double>(0)] public double minChargeToFire;
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
public record ShieldDesc {
    [Req]            public int powerUse, idlePowerUse;
    [Req]            public int maxHP;
    [Req]            public int damageDelay, depletionDelay;
    [Req]            public double regen;
    [Opt<double>(1)] public double absorbFactor;
    public Shield GetShield(Item i) => new Shield(i, this);
    public ShieldDesc() { }
    public ShieldDesc(XElement e) {
        e.Initialize(this);
    }
}
public record ReactorDesc {
    [Req]               public int maxOutput;
    [Req]               public int capacity;
    [Opt<double>(1)]    public double efficiency;
    [Opt<bool>(false)]  public bool battery;        //If true, then we recharge using power from other reactors when available

    public Reactor GetReactor(Item i) => new Reactor(i, this);
    public ReactorDesc() { }
    public ReactorDesc(XElement e) {
        e.Initialize(this);
    }
}
public record SolarDesc {
    [Req] public int maxOutput;
    [Opt<int>(-1 + 0 * 360000)] public int lifetimeOutput;
    public Solar GetSolar(Item i) => new(i, this);
    public SolarDesc() { }
    public SolarDesc(XElement e) {
        e.Initialize(this);
    }
}
public enum Service {
    missileJack,
    armorRepair,
    grind
}
public record ServiceDesc {
    public Service type;
    [Req] public int powerUse;
    [Req] public int interval;
    public ServiceDevice GetMisc(Item i) => new(i, this);
    public ServiceDesc() { }
    public ServiceDesc(XElement e) {
        e.Initialize(this);
        type = e.ExpectAttEnum<Service>(nameof(type));
    }
}