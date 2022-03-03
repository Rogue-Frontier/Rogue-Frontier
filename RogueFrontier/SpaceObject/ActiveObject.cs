using Common;
using SadConsole;

namespace RogueFrontier;
public interface IDockable : StructureObject {
    XY GetDockPoint();
    Console GetDockScene(Console prev, PlayerShip player);
}
public interface MovingObject : Entity {

    System world { get; }
    XY velocity { get; }
}
public interface StructureObject : MovingObject {
    string name { get; }
    //World world { get; }
    XY velocity { get; set; }
    void Damage(Projectile p);
    void Destroy(ActiveObject source = null);
}
public interface ActiveObject : StructureObject {
    Sovereign sovereign { get; }
}
public static class SSpaceObject {
    public static bool IsEqual(this Entity o1, Entity o2) {
        { if (o1 is ISegment s) o1 = s.parent; }
        { if (o2 is ISegment s) o2 = s.parent; }

        return o1 == o2;
    }

    public static bool CanTarget(this ActiveObject owner, ActiveObject target) {
        if(owner is TargetingMarker t) {
            owner = t.Owner;
        }
        return target.active && !IsEqual(owner, target) && target is not Wreck
            && (owner.sovereign.IsEnemy(target.sovereign)
                || (owner is AIShip s && s.behavior.GetOrder().CanTarget(target)));
    }
}
