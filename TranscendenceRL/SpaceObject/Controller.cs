using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TranscendenceRL.SShip;
using static TranscendenceRL.SSpaceObject;

namespace TranscendenceRL {
    public interface Order {
        bool Active { get; }
        void Update();
    }
    public class GuardOrder : Order {
        public Ship owner;
        public SpaceObject guard;
        public SpaceObject target;
        public GuardOrder(Ship owner, SpaceObject guard) {
            this.owner = owner;
            this.guard = guard;
        }
        public void Update() {
            

            //Otherwise find enemy to attack
            if (target != null && !guard.CanTarget(target)) {
                target = null;
            }
            target = target ?? owner.World.entities.GetAll(p => (guard.Position - p).Magnitude < 20).OfType<SpaceObject>().Where(o => !o.IsEqual(owner) &&  guard.CanTarget(o)).GetRandomOrDefault(owner.destiny);

            if(target != null) {
                //Attack now
                new AttackOrder(owner, target).Update();
            } else {
                if((owner.Position - guard.Position).Magnitude < 6) {
                    //If no enemy in range of station, dock at station
                    new Docking(owner, guard).Update();
                } else {
                    new ApproachOrder(owner, guard).Update();
                }
            }
        }
        public bool Active => guard.Active;
    }
    public class AttackAllOrder : Order {
        public Ship owner;
        public SpaceObject target;
        public AttackAllOrder(Ship owner) {
            this.owner = owner;
        }
        public void Update() {
            if(!(target?.Active ?? false)) {
                var weapon = owner.Devices.Weapons.FirstOrDefault();
                if(weapon == null) {
                    return;
                }
                target = owner.World.entities.GetAll(p => (owner.Position - p).Magnitude < weapon.desc.range).OfType<SpaceObject>().Where(so => owner.CanTarget(so)).GetRandomOrDefault(owner.destiny);
            } else {
                new AttackOrder(owner, target).Update();
            }
        }
        public bool Active => true;
    }
    public class AttackOrder {
        public Ship owner;
        public SpaceObject target;
        private Weapon weapon;
        public AttackOrder(Ship owner, SpaceObject target) {
            this.owner = owner;
            this.target = target;
            this.weapon = owner.Devices.Weapons.FirstOrDefault();
        }
        public void Update() {
            if (weapon == null) {
                weapon = owner.Devices.Weapons.FirstOrDefault();
                if(weapon == null) {
                    return;
                }
            }
            var offset = (target.Position - owner.Position);
            var dist = offset.Magnitude;
            if(dist < 10) {
                //If we are too close, then move away

                //Face away from the target
                new FaceOrder(owner, offset.Angle + Math.PI).Update();

                //Get moving!
                owner.SetThrusting(true);
            } else if (dist < weapon.desc.range/4) {
                //If we are in range, then aim and fire

                //Aim at the target
                var aim = new AimOrder(owner, target, weapon.desc.missileSpeed);
                aim.Update();

                if(Math.Abs(aim.angleDiff) < 10 && (owner.Velocity - target.Velocity).Magnitude < 5) {
                    owner.SetThrusting(true);
                }
                //Fire if we are close enough
                if (weapon.desc.omnidirectional || Math.Abs(aim.angleDiff) < 30) {
                    weapon.SetFiring(true, target);
                }
            } else {
                //Otherwise, get closer

                new ApproachOrder(owner, target).Update();

                var aim = new AimOrder(owner, target, weapon.desc.missileSpeed);
                //Fire if we are close enough
                if (weapon.desc.omnidirectional || Math.Abs(aim.angleDiff) < 30) {
                    weapon.SetFiring(true, target);
                }

            }
        }
        public bool Active => target.Active && weapon != null;
    }
    public class ApproachOrder : Order {
        Ship owner;
        SpaceObject target;
        public ApproachOrder(Ship owner, SpaceObject target) {
            this.owner = owner;
            this.target = target;
        }
        public void Update() {
            //Find the direction we need to go
            var offset = (target.Position - owner.Position);

            var randomOffset = new XY((2 * owner.destiny.NextDouble() - 1) * offset.x, (2 * owner.destiny.NextDouble() - 1) * offset.y) / 5;

            offset += randomOffset;

            var speedTowards = (owner.Velocity - target.Velocity).Dot(offset.Normal);
            if (speedTowards < -1) {
                //Decelerate
                var Face = new FaceOrder(owner, Math.PI + owner.Velocity.Angle);
                Face.Update();
                owner.SetThrusting(true);
            } else {
                //Approach

                //Face the target
                var Face = new FaceOrder(owner, offset.Angle);
                Face.Update();
                if (Math.Abs(Helper.AngleDiff(owner.rotationDegrees, offset.Angle * 180 / Math.PI)) < 10 && speedTowards < 10) {

                    //Go
                    owner.SetThrusting(true);
                }
            }
        }
        public bool Active => true;
    }
    public class AimOnceOrder : Order {
        public AimOrder order;
        public AimOnceOrder(Ship owner, Ship target, double missileSpeed) {
            this.order = new AimOrder(owner, target, missileSpeed);
        }
        public void Update() {
            order.Update();
        }

        public bool Active => Math.Abs(order.angleDiff) > 1;
    }
    public class AimOrder : Order {
        public Ship owner;
        public SpaceObject target;
        public double missileSpeed;
        public double targetRads => Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, missileSpeed);
        public double angleDiff => Helper.AngleDiff(owner.rotationDegrees, targetRads * 180 / Math.PI);
        public AimOrder(Ship owner, SpaceObject target, double missileSpeed) {
            this.owner = owner;
            this.target = target;
            this.missileSpeed = missileSpeed;
        }
        public bool Active => true;
        public void Update() {
            var targetRads = this.targetRads;
            var facingRads = owner.stoppingRotation * Math.PI / 180;

            var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            if (ccw < cw) {
                owner.SetRotating(Rotating.CCW);
            } else if (cw < ccw) {
                owner.SetRotating(Rotating.CW);
            }
        }
    }
    public class FaceOrder : Order {
        Ship owner;
        double targetRads;
        public FaceOrder(Ship owner, double targetRads) {
            this.owner = owner;
            this.targetRads = targetRads;
        }
        public void Update() {
            var facingRads = owner.stoppingRotation * Math.PI / 180;

            var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            if (ccw < cw) {
                owner.SetRotating(Rotating.CCW);
            } else if (cw < ccw) {
                owner.SetRotating(Rotating.CW);
            }
        }
        public bool Active => true;
    }

}
