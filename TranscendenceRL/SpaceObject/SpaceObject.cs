using Common;
using SadConsole;

namespace TranscendenceRL {
    public interface MovingObject : Entity {

        System world { get; }
        XY velocity { get; }
    }
    public interface SpaceObject : MovingObject {
        string name { get; }
        //World world { get; }
        Sovereign sovereign { get; }
        //XY velocity { get; }
        void Damage(SpaceObject source, int hp);
        void Destroy(SpaceObject source = null);
    }
    public interface Dockable {
        public bool dockable => true;
        Console GetDockScene(Console prev, PlayerShip playerShip) => null;
        public XY GetDockPoint() => XY.Zero;
    }
    public interface DockableObject : SpaceObject, Dockable { }
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
