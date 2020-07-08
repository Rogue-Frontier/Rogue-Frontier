using Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace TranscendenceRL.Types {
    public class PowerType : DesignType {
        public string name;
        public int cooldownTime;
        public int invokeDelay;

        public PowerEffect Effect;
        public void Initialize(TypeCollection collection, XElement e) {
            if(e.HasElement("Weapon", out XElement weapon)) {
                Effect = new PowerWeapon(new WeaponDesc(e));
            }
        }
    }
    //Interface for invokable powers
    public interface PowerEffect {
        void Invoke(PlayerShip invoker);
    }
    //Power that generates a weapon effect
    public class PowerWeapon : PowerEffect {
        WeaponDesc desc;
        public PowerWeapon(WeaponDesc desc) {
            this.desc = desc;
        }
        public void Invoke(PlayerShip invoker) {

        }
    }
    public class Power {
        public PowerType type;

        public int cooldownPeriod => type.cooldownTime;
        public int invokeDelay => type.invokeDelay;

        public int cooldownLeft;
        public bool ready => cooldownLeft == 0;
        public int invokeCharge;
        public bool charging;
        public Power(PowerType type) {
            this.type = type;
        }
    }
}
