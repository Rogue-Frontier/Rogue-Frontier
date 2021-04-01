using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using TranscendenceRL.Types;

namespace TranscendenceRL {

    public interface InvokeAction {
        string GetDesc(PlayerShip player, Item item);
        void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) { }
    }
    
    public class InstallWeapon : InvokeAction {
        public string GetDesc(PlayerShip player, Item item) {
            if (player.Cargo.Contains(item)) {
                return "Install this weapon";
            } else {
                return "Remove this weapon";
            }
        }
        public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
            if(player.Cargo.Contains(item)) {
                player.AddMessage(new InfoMessage($"Installed weapon {item.type.name}"));

                player.Cargo.Remove(item);
                item.InstallWeapon();
                player.Devices.Install(item.weapon);
            } else {
                player.AddMessage(new InfoMessage($"Removed weapon {item.type.name}"));

                player.Devices.Remove(item.weapon);
                item.RemoveWeapon();
                player.Cargo.Add(item);
            }
            callback?.Invoke();
        }
    }
    public class RepairArmor : InvokeAction {
        public int repairHP;
        public string GetDesc(PlayerShip player, Item item) {
            return "Use this patch to repair armor";
        }
        public RepairArmor(XElement e) {
            this.repairHP = e.ExpectAttributeInt(nameof(repairHP));
        }
        public void Invoke(Console prev, PlayerShip player, Item item, Action callback) {
            var p = prev.Parent;
            p.Children.Remove(prev);
            p.Children.Add(SListScreen.RepairArmorScreen(prev, player, item, this, callback));
        }
    }
    public class InvokePower : InvokeAction {
        PowerType powerType;
        int charges;
        public InvokePower(TypeCollection tc, XElement e) {
            powerType = tc.Lookup<PowerType>(e.ExpectAttribute("powerType"));
            charges = e.ExpectAttributeInt("charges");
        }
        public string GetDesc(PlayerShip player, Item item) {
            return "Invoke this charm";
        }
        public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
            player.AddMessage(new InfoMessage($"Invoked the power of {item.type.name}"));

            charges--;
            if (charges == 0) {
                player.Cargo.Remove(item);
            }
            new Power(powerType).Effect.Invoke(player);

            callback?.Invoke();
        }
    }
    public class Refuel : InvokeAction {
        int energy;
        public Refuel(TypeCollection tc, XElement e) {
            energy = e.ExpectAttributeInt("energy");
        }
        public string GetDesc(PlayerShip player, Item item) {
            return "Refuel reactor";
        }
        public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
            var reactor = player.Devices.Reactors.FirstOrDefault(r => !r.desc.battery);

            if(reactor == null) {
                return;
            }

            reactor.energy += energy;
            player.AddMessage(new InfoMessage($"Refueled reactor with {item.type.name}"));

            player.Cargo.Remove(item);

            callback?.Invoke();
        }
    }

    public class ItemType : DesignType {
        public string codename;
        public string name;
        public string desc;
        public int level;
        public int mass;
        public int value;

        public ArmorDesc armor;
        public WeaponDesc weapon;
        public ShieldDesc shield;
        public ReactorDesc reactor;

        public InvokeAction invoke;


        public void Initialize(TypeCollection tc, XElement e) {
            codename = e.ExpectAttribute(nameof(codename));
            name = e.ExpectAttribute(nameof(name));
            desc = e.TryAttribute(nameof(desc), "");
            level = e.ExpectAttributeInt(nameof(level));
            mass = e.ExpectAttributeInt(nameof(mass));
            value = e.TryAttributeInt(nameof(value), -1);

            switch(e.TryAttribute(nameof(invoke), "none")) {
                case "none":
                    invoke = null;
                    break;
                case "installWeapon":
                    invoke = new InstallWeapon();
                    break;
                case "repairArmor":
                    invoke = new RepairArmor(e);
                    break;
                case "invokePower":
                    invoke = new InvokePower(tc, e);
                    break;
                case "refuel":
                    invoke = new Refuel(tc, e);
                    break;
            }

            if (e.HasElement("Weapon", out var xmlWeapon)) {
                weapon = new WeaponDesc(tc, xmlWeapon);
            }
            if (e.HasElement("Armor", out var xmlArmor)) {
                armor = new ArmorDesc(xmlArmor);
            }
            if (e.HasElement("Shield", out var xmlShield)) {
                shield = new ShieldDesc(xmlShield);
            }
            if(e.HasElement("Reactor", out var xmlReactor)) {
                reactor = new ReactorDesc(xmlReactor);
            }
        }
    }
    public class ArmorDesc {
        public int maxHP;
        public Armor GetArmor(Item i) => new Armor(i, this);
        public ArmorDesc() { }
        public ArmorDesc(XElement e) {
            maxHP = e.ExpectAttributeInt("maxHP");
        }
    }
    public class WeaponDesc {
        public int powerUse;
        public int fireCooldown;
        public int repeat;
        public bool omnidirectional;
        public FragmentDesc shot;

        public int initialCharges;
        public ItemType ammoType;

        public double maneuver;
        public bool hitProjectile;
        public bool autoFire;

        public int missileSpeed => shot.missileSpeed;
        public int damageType => shot.damageType;
        public int damageHP => shot.damageHP;
        public int lifetime => shot.lifetime;

        public int minRange => shot.missileSpeed * shot.lifetime / (TranscendenceRL.TICKS_PER_SECOND * TranscendenceRL.TICKS_PER_SECOND); //DOES NOT INCLUDE CAPACITOR EFFECTS
        public StaticTile effect;
        public CapacitorDesc capacitor;
        public Maneuver GetManeuver(SpaceObject target) => maneuver > 0 && target != null ? new Maneuver(target, maneuver) : null;
        public Weapon GetWeapon(Item i) => new Weapon(i, this);
        public WeaponDesc() { }
        public WeaponDesc(TypeCollection types, XElement e) {
            powerUse = e.ExpectAttributeInt(nameof(powerUse));
            fireCooldown = e.ExpectAttributeInt(nameof(fireCooldown));
            repeat = e.TryAttributeInt(nameof(repeat), 0);
            omnidirectional = e.TryAttributeBool(nameof(omnidirectional), false);
            shot = new FragmentDesc(e);

            maneuver = e.TryAttributeDouble(nameof(maneuver), 0) * Math.PI / (180);

            if(e.TryAttributeBool("pointDefense", false)) {
                hitProjectile = true;
                autoFire = true;
            }

            initialCharges = e.TryAttributeInt(nameof(initialCharges), -1);
            if(e.TryAttribute(nameof(ammoType), out string at)) {
                if(!types.itemType.TryGetValue(at, out ammoType)) {
                    throw new Exception($"ItemType codename expected: ammoType=\"{at}\" ### {e} ### {e.Parent}");
                }
            }

            effect = new StaticTile(e);
            if(e.HasElement("Capacitor", out var xmlCapacitor)) {
                capacitor = new CapacitorDesc(xmlCapacitor);
            }
        }
    }
    public class FragmentDesc {
        public int count;
        public double spreadAngle;
        public int missileSpeed;
        public int damageType;
        public int damageHP;
        public int lifetime;
        public HashSet<FragmentDesc> fragments;
        public StaticTile effect;
        public TrailDesc trail;
        public FragmentDesc() {

        }
        public FragmentDesc(XElement e) {
            count = e.TryAttributeInt(nameof(count), 1);
            spreadAngle = e.TryAttributeDouble(nameof(spreadAngle), count == 1 ? 0 : 3) * Math.PI / 180;
            missileSpeed = e.ExpectAttributeInt(nameof(missileSpeed));
            damageType = e.ExpectAttributeInt(nameof(damageType));
            damageHP = e.ExpectAttributeInt(nameof(damageHP));
            lifetime = e.ExpectAttributeInt(nameof(lifetime));
            fragments = new HashSet<FragmentDesc>();
            if (e.HasElements("Fragment", out var fragmentsList)) {
                fragments.UnionWith(fragmentsList.Select(f => new FragmentDesc(f)));
            }
            if(e.HasElement("Trail", out var trail)) {
                this.trail = new TrailDesc(trail);
            }
            effect = new StaticTile(e);
        }
    }
    public class TrailDesc : ITrail {
        public int lifetime;
        public Color background;
        public TrailDesc() { }
        public TrailDesc(XElement e) {
            lifetime = e.ExpectAttributeInt(nameof(lifetime));
            background = e.ExpectAttributeColor("background");
        }
        public Effect GetTrail(XY Position) => new FadingTile(Position, new ColoredGlyph(Color.Transparent, background), lifetime);
    }
    public class CapacitorDesc {
        public double minChargeToFire;
        public double dischargePerShot;
        public double chargePerTick;
        public double maxCharge;
        public double bonusSpeedPerCharge;
        public double bonusDamagePerCharge;
        public double bonusLifetimePerCharge;
        public CapacitorDesc() { }
        public CapacitorDesc(XElement e) {
            minChargeToFire = e.TryAttributeDouble(nameof(minChargeToFire));
            dischargePerShot = e.ExpectAttributeDouble(nameof(dischargePerShot));
            chargePerTick = e.ExpectAttributeDouble(nameof(chargePerTick));
            maxCharge = e.ExpectAttributeDouble(nameof(maxCharge));
            bonusSpeedPerCharge = e.ExpectAttributeDouble(nameof(bonusSpeedPerCharge));
            bonusDamagePerCharge = e.ExpectAttributeDouble(nameof(bonusDamagePerCharge));
            bonusLifetimePerCharge = e.ExpectAttributeDouble(nameof(bonusLifetimePerCharge));
        }
    }
    public class ShieldDesc {
        public int maxHP;
        public int depletionDelay;
        public double hpPerSecond;
        public Shields GetShields(Item i) => new Shields(i, this);
        public ShieldDesc() { }
        public ShieldDesc(XElement e) {
            maxHP = e.ExpectAttributeInt(nameof(maxHP));
            depletionDelay = e.ExpectAttributeInt(nameof(depletionDelay));
            hpPerSecond = e.ExpectAttributeDouble(nameof(hpPerSecond));
        }
    }
    public class ReactorDesc {
        public int maxOutput;
        public int capacity;
        public double efficiency;
        public bool battery;        //If true, then we recharge using power from other reactors when available

        public Reactor GetReactor(Item i) => new Reactor(i, this);
        public ReactorDesc() { }
        public ReactorDesc(XElement e) {
            maxOutput = e.ExpectAttributeInt(nameof(maxOutput));
            capacity = e.ExpectAttributeInt(nameof(capacity));
            efficiency = e.TryAttributeDouble(nameof(efficiency), 1);
            battery = e.TryAttributeBool(nameof(battery), false);
        }
    }
}
