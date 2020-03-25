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
        public uint level;
        public uint mass;

        public ArmorDesc armor;
        public WeaponDesc weapon;
        public ShieldDesc shield;
        public ReactorDesc reactor;

        public void Initialize(TypeCollection collection, XElement e) {
            throw new NotImplementedException();
        }
    }
    public class ArmorDesc {
        public uint maxHP;
    }
    public class WeaponDesc {
        public uint fireRate;
        public uint damageType;
        public uint damageHP;
        public uint lifetime;
        public ColoredGlyph effect;
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
