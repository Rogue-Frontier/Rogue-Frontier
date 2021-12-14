using System.Linq;

namespace RogueFrontier;

public class ReinforceNearby : StationBehavior {
    int ticks = 0;
    public ReinforceNearby() { }
    public void Update(Station owner) {
        ticks++;
        if (ticks % 150 == 0) {
            owner.UpdateGuardList();
            if (owner.guards.Count < 5) {
                var world = owner.world;
                var gate = world.entities.all
                    .OfType<Stargate>()
                    .OrderBy(g => (g.position - owner.position).magnitude2)
                    .FirstOrDefault();
                if (gate == null) {
                    return;
                }
                var generated = owner.type.guards.Generate(world.types, owner);
                foreach (var guard in generated.Take(5 - owner.guards.Count)) {
                    guard.position = gate.position;
                    owner.guards.Add(guard);
                    world.AddEntity(guard);
                    world.AddEffect(new Heading(guard));
                }
            } else {
                var nearbyFriendly = owner.world.entities.all.OfType<Station>()
                    .Where(s => s.sovereign == owner.sovereign
                    && (s.position - owner.position).magnitude < 250);

                foreach (var nearby in nearbyFriendly) {
                    nearby.UpdateGuardList();
                    if (nearby.guards.Count < 3) {
                        if (owner.guards.Count > 3) {
                            var g = owner.guards.Last();
                            g.order = new GuardOrder(nearby);
                            owner.guards.RemoveAt(owner.guards.Count - 1);
                            nearby.guards.Add(g);
                        }
                    }
                }
            }
        }
    }
}
