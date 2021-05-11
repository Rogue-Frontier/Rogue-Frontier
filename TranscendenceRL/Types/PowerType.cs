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
            name = e.ExpectAttribute(nameof(name));
            cooldownTime = e.ExpectAttributeInt(nameof(cooldownTime));
            invokeDelay = e.ExpectAttributeInt(nameof(invokeDelay));

            if(e.HasElement("Weapon", out XElement xmlWeapon)) {
                Effect = new PowerWeapon(xmlWeapon);
            }
            if(e.HasElement("ProjectileBarrier", out XElement xmlProjectileBarrier)) {
                Effect = new PowerProjectileBarrier(xmlProjectileBarrier);
            }
        }
    }
    //Interface for invokable powers
    public interface PowerEffect {
        void Invoke(PlayerShip invoker);
    }
    //Power that generates a weapon effect
    public class PowerWeapon : PowerEffect {
        FragmentDesc desc;
        public PowerWeapon() { }
        public PowerWeapon(XElement e) {
            this.desc = new FragmentDesc(e);
        }
        public void Invoke(PlayerShip invoker) => SWeapon.CreateShot(desc, invoker, invoker.rotationDeg * Math.PI / 180);
    }
    public class PowerProjectileBarrier : PowerEffect {
        public enum BarrierType {
            echo, accuse
        }
        public BarrierType barrierType;
        public int radius;
        public int lifetime;
        public PowerProjectileBarrier() { }
        public PowerProjectileBarrier(XElement e) {
            barrierType = e.ExpectAttributeEnum<BarrierType>(nameof(barrierType));
            lifetime = e.ExpectAttributeInt(nameof(lifetime));
            radius = e.ExpectAttributeInt(nameof(radius));
        }
        public void Invoke(PlayerShip invoker) {
            var world = invoker.world;
            var end = 2 * Math.PI;

            Func<XY, int, ProjectileBarrier> construct = null;
            switch(barrierType) {
                case BarrierType.accuse:
                    HashSet<Projectile> cloneList = new HashSet<Projectile>();
                    construct = (position, lifetime) => new AccuseBarrier(invoker, position, lifetime, cloneList);
                    break;
                case BarrierType.echo:
                    construct = (position, lifetime) => new EchoBarrier(invoker, position, lifetime);
                    break;
            }
            double step = 1f / radius;
            for (double angle = 0; angle < end; angle += step) {
                var barrier = construct(XY.Polar(angle, radius), lifetime);
                world.AddEntity(barrier);
            }

            step = 1f / (radius + 1);
            for (double angle = 0; angle < end; angle += step) {
                var barrier = construct(XY.Polar(angle, radius + 1), lifetime);
                world.AddEntity(barrier);
            }

        }
    }
    public interface IPower {
        public int cooldownPeriod { get; }
        public int invokeDelay { get; }
        public bool ready => cooldownLeft == 0;
        public int cooldownLeft { get; set; }
        public int invokeCharge { get; set; }
        public bool charging { get; set; }
        public PowerEffect Effect { get; }
    }
    public class Power : IPower {
        public PowerType type;

        public int cooldownPeriod => type.cooldownTime;
        public int invokeDelay => type.invokeDelay;

        public int cooldownLeft { get; set; }
        public bool ready => cooldownLeft == 0;
        public int invokeCharge { get; set; }
        public bool charging { get; set; }

        public PowerEffect Effect => type.Effect;
        public Power(PowerType type) {
            this.type = type;
        }
    }


}
