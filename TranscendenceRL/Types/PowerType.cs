using Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace TranscendenceRL.Types {
    public class PowerType : DesignType {
        public string codename;
        public string name;
        public int cooldownTime;
        public int invokeDelay;
        public string message;

        public PowerEffect Effect;
        public void Initialize(TypeCollection collection, XElement e) {
            codename = e.ExpectAttribute(nameof(codename));
            name = e.ExpectAttribute(nameof(name));
            cooldownTime = e.ExpectAttributeInt(nameof(cooldownTime));
            invokeDelay = e.ExpectAttributeInt(nameof(invokeDelay));
            message = e.TryAttribute(nameof(message), null);

            if (e.HasElement("Weapon", out var xmlWeapon)) {
                Effect = new PowerWeapon(xmlWeapon);
            } else if(e.HasElement("Heal", out var xmlHeal)) {
                Effect = new PowerHeal();
            } else if (e.HasElement("ProjectileBarrier", out var xmlProjectileBarrier)) {
                Effect = new PowerProjectileBarrier(xmlProjectileBarrier);
            } else {
                throw new Exception($"Power must have effect: {codename} ### {e} ### {e.Parent}");
            }
        }
    }
    //Interface for invokable powers
    public interface PowerEffect {
        void Invoke(PlayerShip invoker);
    }
    //Power that generates a weapon effect
    public class PowerWeapon : PowerEffect {
        public FragmentDesc desc;
        public PowerWeapon() { }
        public PowerWeapon(XElement e) {
            this.desc = new FragmentDesc(e);
        }
        public void Invoke(PlayerShip invoker) => SWeapon.CreateShot(desc, invoker, invoker.rotationDeg * Math.PI / 180);
    }
    public class PowerHeal : PowerEffect {
        public PowerHeal() { }
        public PowerHeal(XElement e) {

        }
        public void Invoke(PlayerShip invoker) {
            invoker.hull.Restore();
            invoker.devices.Reactors.ForEach(r => r.energy = r.desc.capacity);
        }
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

            HashSet<(int, int)> covered = new();
            for(double r = radius; r < radius + 2; r++) {
                double step = 1f / (r * 2);
                for (double angle = 0; angle < end; angle += step) {
                    var p = XY.Polar(angle, r);
                    /*
                    if(covered.Contains(p)) {
                        continue;
                    }
                    covered.Add(p);
                    */
                    var barrier = construct(p, lifetime);
                    world.AddEntity(barrier);
                }
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
        public bool fullyCharged => invokeCharge >= invokeDelay;

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
