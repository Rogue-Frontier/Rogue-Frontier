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
            { o1 = (o1 is AIShip s) ? s.Ship : o1; }
            { o1 = (o1 is PlayerShip s) ? s.Ship : o1; }
            { o2 = (o2 is AIShip s) ? s.Ship : o2; }
            { o2 = (o2 is PlayerShip s) ? s.Ship : o2; }
            return o1 == o2;
        }
        public static bool CanTarget(this SpaceObject owner, SpaceObject target) {

            return !IsEqual(owner, target) && owner.Sovereign.IsEnemy(target);
        }
    }
}
