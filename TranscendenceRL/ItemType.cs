using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class ItemType {
        public string name;
        public uint level;
        public uint mass;
    }
    class ArmorDesc {
        public uint maxHP;
    }
    class WeaponDesc {
        public uint fireRate;
        public uint damageType;
        public uint damageHP;
        public uint lifetime;
        public ColoredGlyph effect;
    }
    class ShieldDesc {
        public uint maxHP;
        public uint depletionDelay;
        public uint ticksPerHP;
    }
    class ReactorDesc {
        public uint maxPower;
        public uint maxFuel;
        public uint powerPerFuel;
    }
}
