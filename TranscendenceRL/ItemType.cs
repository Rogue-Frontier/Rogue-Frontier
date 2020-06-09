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
            powerUse = e.ExpectAttributeInt("powerUse");
            fireCooldown = e.ExpectAttributeInt("fireCooldown");
            missileSpeed = e.ExpectAttributeInt("missileSpeed");
            damageType = e.ExpectAttributeInt("damageType");
            damageHP = e.ExpectAttributeInt("damageHP");
            lifetime = e.ExpectAttributeInt("lifetime");
            omnidirectional = e.TryAttributeBool("omnidirectional", false);

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
            dischargePerShot = e.ExpectAttributeDouble("dischargePerShot");
            chargePerTick = e.ExpectAttributeDouble("chargePerTick");
            maxCharge = e.ExpectAttributeDouble("maxCharge");
            bonusSpeedPerCharge = e.ExpectAttributeDouble("bonusSpeedPerCharge");
            bonusDamagePerCharge = e.ExpectAttributeDouble("bonusDamagePerCharge");
            bonusLifetimePerCharge = e.ExpectAttributeDouble("bonusLifetimePerCharge");
        }
    }
    public class ShieldDesc {
        public int maxHP;
        public int depletionDelay;
        public double hpPerSecond;
        public ShieldDesc(XElement e) {
            maxHP = e.ExpectAttributeInt("maxHP");
            depletionDelay = e.ExpectAttributeInt("depletionDelay");
            hpPerSecond = e.ExpectAttributeDouble("hpPerSecond");
        }
    }
    public class ReactorDesc {
        public int maxOutput;
        public int capacity;
        public double efficiency;
        public bool battery;        //If true, then we recharge using power from other reactors when available
    }
}
