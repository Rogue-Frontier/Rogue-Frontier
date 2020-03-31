using Common;

namespace TranscendenceRL {
    public class Docking {
        public Ship ship;
        public SpaceObject target;
        public bool done;
        public Docking(Ship ship, SpaceObject target) {
            this.ship = ship;
            this.target = target;
        }
        public bool Update() {
            if(!done) {
                done = UpdateDocking();
                if(done) {
                    return true;
                }
            }
            return false;
        }
        public bool UpdateDocking() {

            double decel = 10f / TranscendenceRL.TICKS_PER_SECOND;
            double stoppingTime = ship.Velocity.Magnitude / decel;

            double stoppingDistance = ship.Velocity.Magnitude * stoppingTime - (decel * stoppingTime * stoppingTime) / 2;
            var stoppingPoint = ship.Position;
            if (!ship.Velocity.IsZero) {
                ship.Velocity -= XY.Polar(ship.Velocity.Angle, decel);
                stoppingPoint += ship.Velocity.Normal * stoppingDistance;
            }
            var offset = target.Position - stoppingPoint;

            if (offset.Magnitude > 0.25) {
                ship.Velocity += XY.Polar(offset.Angle, decel * 6);
            } else if ((ship.Position - target.Position).Magnitude < 1) {
                ship.Velocity = new XY(0, 0);
                return true;
            }
            return false;
        }
    }
}
