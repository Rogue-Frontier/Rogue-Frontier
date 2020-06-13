using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        public ArmorDesc(XElement e) {
            maxHP = e.ExpectAttributeInt("maxHP");
        }
    }
    public class WeaponDesc {
        public int powerUse;
        public int fireCooldown;
        public int missileSpeed;
        public int damageType;
        public int damageHP;
        public int lifetime;
        public bool omnidirectional;
        public int range => missileSpeed * lifetime / TranscendenceRL.TICKS_PER_SECOND;
        public StaticTile effect;

        public CapacitorDesc capacitor;
        public WeaponDesc(XElement e) {
            powerUse = e.ExpectAttributeInt(nameof(powerUse));
            fireCooldown = e.ExpectAttributeInt(nameof(fireCooldown));
            missileSpeed = e.ExpectAttributeInt(nameof(missileSpeed));
            damageType = e.ExpectAttributeInt(nameof(damageType));
            damageHP = e.ExpectAttributeInt(nameof(damageHP));
            lifetime = e.ExpectAttributeInt(nameof(lifetime));
            omnidirectional = e.TryAttributeBool(nameof(omnidirectional), false);

            effect = new StaticTile(e);
            if(e.HasElement("Capacitor", out var xmlCapacitor)) {
                capacitor = new CapacitorDesc(xmlCapacitor);
            }
        }
    }
    public class CapacitorDesc {
        public double dischargePerShot;
        public double chargePerTick;
        public double maxCharge;
        public double bonusSpeedPerCharge;
        public double bonusDamagePerCharge;
        public double bonusLifetimePerCharge;

        public CapacitorDesc(XElement e) {
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

        public ReactorDesc(XElement e) {
            maxOutput = e.ExpectAttributeInt(nameof(maxOutput));
            capacity = e.ExpectAttributeInt(nameof(capacity));
            efficiency = e.TryAttributeDouble(nameof(efficiency), 1);
            battery = e.TryAttributeBool(nameof(battery), false);
        }
    }
}
