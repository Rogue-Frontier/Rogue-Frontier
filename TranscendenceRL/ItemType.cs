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
            if(e.HasElement("Armor", out var xmlArmor)) {
                armor = new ArmorDesc(xmlArmor);
            }
            if (e.HasElement("Weapon", out var xmlWeapon)) {
                weapon = new WeaponDesc(xmlWeapon);
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
        public int fireCooldown;
        public int missileSpeed;
        public int damageType;
        public int damageHP;
        public int lifetime;
        public bool omnidirectional;
        public int range => missileSpeed * lifetime / 30;
        public StaticTile effect;
        public WeaponDesc(XElement e) {
            fireCooldown = e.ExpectAttributeInt("fireCooldown");
            missileSpeed = e.ExpectAttributeInt("missileSpeed");
            damageType = e.ExpectAttributeInt("damageType");
            damageHP = e.ExpectAttributeInt("damageHP");
            lifetime = e.ExpectAttributeInt("lifetime");
            omnidirectional = e.TryAttributeBool("omnidirectional", false);

            effect = new StaticTile(e);
        }

    }
    public class ShieldDesc {
        public uint maxHP;
        public uint depletionDelay;
        public uint ticksPerHP;
    }
    public class ReactorDesc {
        public uint maxPower;
        public uint maxFuel;
        public uint powerPerFuel;
    }
}
