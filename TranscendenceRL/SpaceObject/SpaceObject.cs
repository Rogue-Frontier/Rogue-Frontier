using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public interface SpaceObject : Entity {
        string Name { get; }
        World World { get; }
        Sovereign Sovereign { get; }
        XY Velocity { get; }
        void Damage(SpaceObject source, int hp);
        void Destroy();
    }
    public static class SSpaceObject {
        public static bool IsEqual(this SpaceObject o1, SpaceObject o2) {
            { if (o1 is AIShip s) o1 = s.Ship; }
            { if (o1 is PlayerShip s) o1 = s.Ship; }
            { if (o2 is AIShip s) o2 = s.Ship; }
            { if(o2 is PlayerShip s) o2 = s.Ship; }
            { if (o1 is Segment s) o1 = s.Parent; }
            { if (o2 is Segment s) o2 = s.Parent; }

            return o1 == o2;
        }

        public static bool CanTarget(this SpaceObject owner, SpaceObject target) {

            return target.Active && !IsEqual(owner, target) && owner.Sovereign.IsEnemy(target) && !(target is Wreck);
        }
    }
}
