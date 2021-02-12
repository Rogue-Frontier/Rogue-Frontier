using Common;
using System;
using System.Linq;

using Helper = Common.Main;
namespace TranscendenceRL {
    public interface Order {
        bool Active { get; }
        void Update(AIShip owner);
    }

    public interface ICombatOrder {
        public bool CanTarget(SpaceObject other) => false;
    }
    public class EscortOrder : Order, ICombatOrder {
        public SpaceObject attacker;
        public IShip target;
        public XY offset;
        public EscortOrder(IShip target, XY offset) {
            this.target = target;
            this.offset = offset;
        }

        public bool CanTarget(SpaceObject other) => other == attacker;
        public void Update(AIShip owner) {

            attacker = ((attacker?.Active == true) ? attacker : null) ?? owner.World.entities.all
                .OfType<AIShip>()
                .FirstOrDefault(s => (s.Position - owner.Position).Magnitude < 100
                            && s.controller is ICombatOrder o && (o.CanTarget(target) || o.CanTarget(owner)));
            if (attacker != null) {
                new AttackOrder(attacker).Update(owner);
            } else {
                new FollowOrder(target, offset).Update(owner);
            }
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
            var offset = this.offset.Rotate(target.stoppingRotation * Math.PI / 180);
            Heading.Crosshair(owner.World, target.Position + offset);
            new ApproachOrder(target, this.offset).Update(owner);
        }
        public bool Active => target.Active;
    }
    public class ApproachOrder : Order {
        public IShip target;
        public XY offset;
        public ApproachOrder(IShip target, XY offset) {
            this.target = target;
            this.offset = offset;
        }

        public void Update(AIShip owner) {
            //Remove dock
            owner.Dock = null;

            var velDiff = owner.Velocity - target.Velocity;
            double decel = owner.ShipClass.thrust * TranscendenceRL.TICKS_PER_SECOND / 2;
            double stoppingTime = velDiff.Magnitude / decel;
            double stoppingDistance = owner.Velocity.Magnitude * stoppingTime - (decel * stoppingTime * stoppingTime) / 2;
            var stoppingPoint = owner.Position;
            if (!owner.Velocity.IsZero) {
                stoppingPoint += owner.Velocity.Normal * stoppingDistance;
            }
            var dest = target.Position + (target.Velocity * stoppingTime) + this.offset.Rotate(target.stoppingRotation * Math.PI / 180);
            var offset = dest - stoppingPoint;

            //Heading.Crosshair(owner.World, dest);

            var velProjection = velDiff * velDiff.Dot(offset.Normal) / velDiff.Dot(velDiff);
            var velRejection = velDiff - velProjection;

            //Make sure we're going the right way
            if (velDiff.Magnitude > 5 && velRejection.Magnitude > velProjection.Magnitude/2) {
                //Decelerate
                var backAngle = Math.PI + velRejection.Angle;
                var faceBack = new FaceOrder(backAngle);
                faceBack.Update(owner);
                var angleDiff = Math.Abs(Helper.AngleDiff(owner.rotationDegrees, backAngle * 180 / Math.PI));
                if (angleDiff < 5) {
                    owner.SetThrusting(true);
                }
            } else {
                //Prepare to decelerate
                if (offset.Magnitude < 1) {
                    //If we're close enough to dest, then just teleport there
                    if((dest - owner.Position).Magnitude > 1) {
                        owner.SetDecelerating(true);
                    } else {
                        owner.Velocity = target.Velocity;
                        owner.Position = dest;

                        //Match the target's facing
                        var Face = new FaceOrder(target.rotationDegrees * Math.PI / 180);
                        Face.Update(owner);
                    }

                } else {
                    //Approach the target

                    //Face the target
                    var Face = new FaceOrder(offset.Angle);
                    //var Face = new FaceOrder(Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, owner.ShipClass.thrust * 30, out _));
                    Face.Update(owner);

                    //If we're facing close enough
                    if (Math.Abs(Helper.AngleDiff(owner.rotationDegrees, offset.Angle * 180 / Math.PI)) < 10 && (velProjection.Magnitude < offset.Magnitude/2 || velDiff.Magnitude == 0)) {

                        //Go
                        owner.SetThrusting(true);
                    }
                }

                
            }
            
        }
        public bool Active => true;
    }
    public class GuardOrder : Order, ICombatOrder {
        public SpaceObject guard;
        public SpaceObject target;
        public GuardOrder(SpaceObject guard) {
            this.guard = guard;
        }

        public bool CanTarget(SpaceObject other) => other == target;
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

                    owner.Dock = new Docking(guard);
                } else {
                    new ApproachOrbitOrder(guard).Update(owner);
                }
            }
        }
        public bool Active => guard.Active;
    }
    public class AttackAllOrder : Order {
        public SpaceObject target;
        public bool CanTarget(SpaceObject other) => other == target;
        public void Update(AIShip owner) {
            if(!(target?.Active ?? false)) {
                var weapon = owner.Devices.Weapons.FirstOrDefault();
                if(weapon == null) {
                    return;
                }
                //currentRange is variable and minRange is constant, so weapon dynamics may affect attack range
                target = owner.World.entities.all.OfType<SpaceObject>().Where(so => owner.IsEnemy(so)).GetRandomOrDefault(owner.destiny);
            } else {
                new AttackOrder(target).Update(owner);
            }
        }
        public bool Active => true;
    }
    public class AttackOrder : Order {
        public SpaceObject target;
        public Weapon weapon;
        public AttackOrder(SpaceObject target) {
            this.target = target;
        }
        public bool CanTarget(SpaceObject other) => other == target;
        public void Update(AIShip owner) {
            if (weapon == null) {
                weapon = owner.Devices.Weapons.FirstOrDefault();
                if(weapon == null) {
                    return;
                }
            }
            bool RangeCheck() => (owner.Position - target.Position).Magnitude < weapon.currentRange;
            void SetFiring() => weapon.SetFiring(true, target);

            //Remove dock
            owner.Dock = null;

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
                if (weapon.desc.omnidirectional || Math.Abs(aim.GetAngleDiff(owner)) < 30 && RangeCheck()) {
                    SetFiring();
                }
            } else {
                //Otherwise, get closer

                new ApproachOrbitOrder(target).Update(owner);

                var aim = new AimOrder(target, weapon.missileSpeed);
                //Fire if we are close enough
                if (weapon.desc.omnidirectional || Math.Abs(aim.GetAngleDiff(owner)) < 10 && RangeCheck()) {
                    SetFiring();
                }

            }
        }
        public bool Active => target.Active && weapon != null;
    }

    public class SnipeOrder : Order, ICombatOrder {
        public SpaceObject target;
        public Weapon weapon;
        public SnipeOrder(SpaceObject target) {
            this.target = target;
        }
        public bool CanTarget(SpaceObject other) => other == target;
        public void Update(AIShip owner) {
            if (weapon == null) {
                weapon = owner.Devices.Weapons.FirstOrDefault();
                if (weapon == null) {
                    return;
                }
            }
            //Aim at the target
            var aim = new AimOrder(target, weapon.missileSpeed);
            aim.Update(owner);

            //Fire if we are close enough
            if (weapon.desc.omnidirectional || Math.Abs(aim.GetAngleDiff(owner)) < 30) {
                weapon.SetFiring(true, target);
            }
        }
        public bool Active => target.Active && weapon != null;
    }
    public class ApproachOrbitOrder : Order {
        public SpaceObject target;
        public ApproachOrbitOrder(SpaceObject target) {
            this.target = target;
        }

        public void Update(AIShip owner) {
            //Remove dock
            owner.Dock = null;

            //Find the direction we need to go
            var offset = (target.Position - owner.Position);

            var randomOffset = new XY((2 * owner.destiny.NextDouble() - 1) * offset.x, (2 * owner.destiny.NextDouble() - 1) * offset.y) / 5;

            offset += randomOffset;

            var speedTowards = (owner.Velocity - target.Velocity).Dot(offset.Normal);
            if (speedTowards < 0) {
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
        public double targetRads;
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
