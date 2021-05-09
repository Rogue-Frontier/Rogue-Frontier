using Common;
using SadConsole;

namespace TranscendenceRL {
    public interface SpaceObject : Entity {
        string name { get; }
        World world { get; }
        Sovereign sovereign { get; }
        XY velocity { get; }
        void Damage(SpaceObject source, int hp);
        void Destroy(SpaceObject source = null);
    }
    public interface Dockable : SpaceObject {
        Console GetScene(Console prev, PlayerShip playerShip);
    }
    public static class SSpaceObject {
        public static bool IsEqual(this SpaceObject o1, SpaceObject o2) {
            { if (o1 is AIShip s) o1 = s.ship; }
            { if (o1 is PlayerShip s) o1 = s.ship; }
            { if (o2 is AIShip s) o2 = s.ship; }
            { if(o2 is PlayerShip s) o2 = s.ship; }
            { if (o1 is Segment s) o1 = s.parent; }
            { if (o2 is Segment s) o2 = s.parent; }

            return o1 == o2;
        }

        public static bool CanTarget(this SpaceObject owner, SpaceObject target) {

            return target.active && !IsEqual(owner, target) && owner.sovereign.IsEnemy(target) && !(target is Wreck);
        }
    }
}
