using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class Controller {
        public IShip Ship { get; private set; }
        public List<Order> orders { get; private set; }
        public Controller(IShip Ship) {
            this.Ship = Ship;
            orders = new List<Order>();
        }
        public void Update() {
            if(orders.Any()) {
                UpdateOrder();
            }
        }
        public void UpdateOrder() {
            orders.First().Update();
        }
    }
    public interface Order {
        bool Active { get; }
        void Update();
    }
    public class GuardOrder {
        public Ship owner;
        public SpaceObject guard;
        public GuardOrder(Ship owner, SpaceObject guard) {
            this.owner = owner;
            this.guard = guard;
        }
        public void Update() {
            //If no enemy in range of station, dock at station
            //Otherwise find enemy to attack
            //Undock and make sure we clear the Docking variable
            //Find angle to enemy
            //Travel in direction of enemy
        }
    }
    public class AttackOrder {
        public Ship owner;
        public SpaceObject target;
        public AttackOrder(Ship owner, SpaceObject target) {
            this.owner = owner;
            this.target = target;
        }
        public void Update() {
            //If we are in range, then aim and fire
            //Otherwise, approach at a lead angle
        }
    }
    public class InterceptOrder {
        //The ship flies forward and at the same time maintains a standing AimOnceOrder
    }
    public class AimOnceOrder {
        public Ship owner;
        public SpaceObject target;
        public double missileSpeed;
        public double targetAngle => Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, missileSpeed);
        public double angleDiff => Helper.AngleDiff(owner.rotationDegrees, targetAngle);
        public AimOnceOrder(Ship owner, Ship target, double missileSpeed) {
            this.owner = owner;
            this.target = target;
            this.missileSpeed = missileSpeed;
        }
        public bool Active => Math.Abs(angleDiff) > 1;
        public void Update() {
            var targetAngle = this.targetAngle;
            var targetRads = targetAngle * Math.PI / 180;
            var facingRads = owner.rotationDegrees * Math.PI / 180;

            var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            if (ccw < cw) {
                owner.SetRotating(Rotating.CCW);
            } else if (cw < ccw) {
                owner.SetRotating(Rotating.CW);
            }
        }

    }
}
