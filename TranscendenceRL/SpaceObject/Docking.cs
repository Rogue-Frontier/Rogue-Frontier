using Common;

namespace TranscendenceRL {
    public class Docking {
        public SpaceObject Target;
        public bool docked;
        public bool justDocked;
        public Docking(SpaceObject target) {
            this.Target = target;
        }
        public void Update(IShip owner) {
            if(!docked) {
                docked = UpdateDocking(owner);
                if(docked) {
                    justDocked = true;
                }
            } else {
                owner.position = Target.position;
                owner.velocity = Target.velocity;
            }
        }
        public bool UpdateDocking(IShip ship) {
            double decel = ship.shipClass.thrust / 2 * Program.TICKS_PER_SECOND;
            double stoppingTime = (ship.velocity - Target.velocity).Magnitude / decel;
            double stoppingDistance = ship.velocity.Magnitude * stoppingTime - (decel * stoppingTime * stoppingTime) / 2;
            var stoppingPoint = ship.position;
            if (!ship.velocity.IsZero) {
                stoppingPoint += ship.velocity.Normal * stoppingDistance;
            }
            var offset = Target.position + (Target.velocity * stoppingTime) - stoppingPoint;

            if (offset.Magnitude > 0.25) {
                ship.velocity += XY.Polar(offset.Angle, ship.shipClass.thrust);
            } else if ((ship.position - Target.position).Magnitude < 1) {
                ship.velocity = Target.velocity;
                return true;
            }
            return false;
        }
    }
}
