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
        void Update(AIShip owner);
    }
    public class EscortOrder : Order {
        public IShip target;
        public XY offset;
        public EscortOrder(IShip target, XY offset) {
            this.target = target;
            this.offset = offset;
        }
        public void Update(AIShip owner) {
            new FollowOrder(target, offset).Update(owner);
        }
        public bool Active => target.Active;
    }
    public class FollowOrder : Order {
        public IShip target;
        public XY offset;
        public FollowOrder(IShip target, XY offset) {
            this.target = target;
            this.offset = offset;
        }
        public void Update(AIShip owner) {
            var offset = this.offset.Rotate(target.stoppingRotation);
            new ApproachOrder(target, offset).Update(owner);
        }
        public bool Active => target.Active;
    }
    public class GuardOrder : Order {
        public SpaceObject guard;
        public SpaceObject target;
        public GuardOrder(SpaceObject guard) {
            this.guard = guard;
        }
        public void Update(AIShip owner) {
            

            //Otherwise find enemy to attack
            if (target?.Active != true) {
                target = owner.World.entities.GetAll(p => (guard.Position - p).Magnitude < 20).OfType<SpaceObject>().Where(o => !o.IsEqual(owner) && guard.CanTarget(o)).GetRandomOrDefault(owner.destiny);

            }

            if (target != null) {
                //Attack now
                new AttackOrder(target).Update(owner);
            } else {
                if((owner.Position - guard.Position).Magnitude < 6) {
                    //If no enemy in range of station, dock at station

                    owner.docking = new Docking(guard);
                } else {
                    new ApproachOrder(guard).Update(owner);
                }
            }
        }
        public bool Active => guard.Active;
    }
    public class AttackAllOrder : Order {
        public SpaceObject target;
        public void Update(AIShip owner) {
            if(!(target?.Active ?? false)) {
                var weapon = owner.Devices.Weapons.FirstOrDefault();
                if(weapon == null) {
                    return;
                }
                //currentRange is variable and minRange is constant, so weapon dynamics may affect attack range
                target = owner.World.entities.GetAll(p => (owner.Position - p).Magnitude < weapon.desc.minRange).OfType<SpaceObject>().Where(so => owner.IsEnemy(so)).GetRandomOrDefault(owner.destiny);
            } else {
                new AttackOrder(target).Update(owner);
            }
        }
        public bool Active => true;
    }
    public class AttackOrder : Order {
        public SpaceObject target;
        private Weapon weapon;
        public AttackOrder(SpaceObject target) {
            this.target = target;
        }
        public void Update(AIShip owner) {
            if (weapon == null) {
                weapon = owner.Devices.Weapons.FirstOrDefault();
                if(weapon == null) {
                    return;
                }
            }

            //Remove dock
            owner.docking = null;

            var offset = (target.Position - owner.Position);
            var dist = offset.Magnitude;
            if(dist < 10) {
                //If we are too close, then move away

                //Face away from the target
                new FaceOrder(offset.Angle + Math.PI).Update(owner);

                //Get moving!
                owner.SetThrusting(true);
            } else if (dist < weapon.currentRange/2) {
                //If we are in range, then aim and fire

                //Aim at the target
                var aim = new AimOrder(target, weapon.missileSpeed);
                aim.Update(owner);

                if(Math.Abs(aim.GetAngleDiff(owner)) < 10 && (owner.Velocity - target.Velocity).Magnitude < 5) {
                    owner.SetThrusting(true);
                }
                //Fire if we are close enough
                if (weapon.desc.omnidirectional || Math.Abs(aim.GetAngleDiff(owner)) < 30) {
                    weapon.SetFiring(true, target);
                }
            } else {
                //Otherwise, get closer

                new ApproachOrder(target).Update(owner);

                var aim = new AimOrder(target, weapon.missileSpeed);
                //Fire if we are close enough
                if (weapon.desc.omnidirectional || Math.Abs(aim.GetAngleDiff(owner)) < 30) {
                    weapon.SetFiring(true, target);
                }

            }
        }
        public bool Active => target.Active && weapon != null;
    }
    public class ApproachOrder : Order {
        SpaceObject target;
        XY offset;
        public ApproachOrder(SpaceObject target) : this(target, new XY()) {

        }
        public ApproachOrder(SpaceObject target, XY offset) {
            this.target = target;
            this.offset = offset;
        }

        public void Update(AIShip owner) {
            //Remove dock
            owner.docking = null;

            //Find the direction we need to go
            var offset = (target.Position - owner.Position) + this.offset;

            var randomOffset = new XY((2 * owner.destiny.NextDouble() - 1) * offset.x, (2 * owner.destiny.NextDouble() - 1) * offset.y) / 5;

            offset += randomOffset;

            var speedTowards = (owner.Velocity - target.Velocity).Dot(offset.Normal);
            if (speedTowards < -1) {
                //Decelerate
                var Face = new FaceOrder(Math.PI + owner.Velocity.Angle);
                Face.Update(owner);
                owner.SetThrusting(true);
            } else {
                //Approach

                //Face the target
                var Face = new FaceOrder(offset.Angle);
                Face.Update(owner);

                //If we're facing close enough
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
        public AimOnceOrder(BaseShip owner, BaseShip target, double missileSpeed) {
            this.order = new AimOrder(target, missileSpeed);
            Active = true;
        }
        public void Update(AIShip owner) {
            order.Update(owner);
            Active = Math.Abs(order.GetAngleDiff(owner)) > 1;
        }

        public bool Active { get; private set; }
    }
    public class AimOrder : Order {
        public SpaceObject target;
        public double missileSpeed;
        public double GetTargetRads(AIShip owner) => Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, missileSpeed, out var _);
        public double GetAngleDiff(AIShip owner) => Helper.AngleDiff(owner.rotationDegrees, GetTargetRads(owner) * 180 / Math.PI);
        public AimOrder(SpaceObject target, double missileSpeed) {
            this.target = target;
            this.missileSpeed = missileSpeed;
        }
        public bool Active => true;
        public void Update(AIShip owner) {
            var targetRads = this.GetTargetRads(owner);
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
        double targetRads;
        public FaceOrder(double targetRads) {
            this.targetRads = targetRads;
        }
        public void Update(AIShip owner) {
            var facingRads = owner.Ship.stoppingRotationWithCounterTurn * Math.PI / 180;

            var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            if (ccw < cw) {
                owner.SetRotating(Rotating.CCW);
            } else if (cw < ccw) {
                owner.SetRotating(Rotating.CW);
            } else {
                if (owner.Ship.rotatingVel > 0) {
                    owner.SetRotating(Rotating.CW);
                } else {
                    owner.SetRotating(Rotating.CCW);
                }
            }
        }
        public bool Active => true;
    }

}
