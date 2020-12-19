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

namespace TranscendenceRL {
    public class ItemType : DesignType {
        public string name;
        public int level;
        public int mass;

        public ArmorDesc armor;
        public WeaponDesc weapon;
        public ShieldDesc shield;
        public ReactorDesc reactor;

        public void Initialize(TypeCollection collection, XElement e) {
            name = e.ExpectAttribute("name");
            level = e.ExpectAttributeInt("level");
            mass = e.ExpectAttributeInt("mass");
            if (e.HasElement("Weapon", out var xmlWeapon)) {
                weapon = new WeaponDesc(xmlWeapon);
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

        public int missileSpeed => shot.missileSpeed;
        public int damageType => shot.damageType;
        public int damageHP => shot.damageHP;
        public int lifetime => shot.lifetime;

        public int minRange => shot.missileSpeed * shot.lifetime / (TranscendenceRL.TICKS_PER_SECOND * TranscendenceRL.TICKS_PER_SECOND); //DOES NOT INCLUDE CAPACITOR EFFECTS
        public StaticTile effect;
        public CapacitorDesc capacitor;

        public Weapon GetWeapon(Item i) => new Weapon(i, this);
        public WeaponDesc() { }
        public WeaponDesc(XElement e) {
            powerUse = e.ExpectAttributeInt(nameof(powerUse));
            fireCooldown = e.ExpectAttributeInt(nameof(fireCooldown));
            repeat = e.TryAttributeInt(nameof(repeat), 0);
            omnidirectional = e.TryAttributeBool(nameof(omnidirectional), false);
            shot = new FragmentDesc(e);


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
    public class TrailDesc {
        public int lifetime;
        public Color background;
        public TrailDesc() { }
        public TrailDesc(XElement e) {
            lifetime = e.ExpectAttributeInt(nameof(lifetime));
            background = e.ExpectAttributeColor("background");
        }
        public GetTrail GetTrail() => (Position) => new FadingTrail(Position, new ColoredGlyph(Color.Transparent, background), lifetime);
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
