using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class ReinforceNearby : StationBehavior {
        int ticks = 0;
        public ReinforceNearby() { }
        public void Update(Station owner) {
            ticks++;
            if(ticks%150 == 0) {
                var world = owner.world;
                if(owner.guards.Count < 5) {
                    var gate = world.entities.all
                        .OfType<Stargate>()
                        .OrderBy(g => (g.position - owner.position).magnitude2)
                        .FirstOrDefault();
                    if(gate == null) {
                        return;
                    }
                    var generated = owner.type.guards.Generate(world.types, owner);
                    foreach(var guard in generated.Take(5 - owner.guards.Count)) {
                        guard.position = gate.position;
                        owner.guards.Add(guard);
                        world.AddEntity(guard);
                        world.AddEffect(new Heading(guard));
                    }
                } else {
                    var ent = owner.world.entities.all.OfType<Station>();
                    foreach (var nearby in ent.Where(s => s.sovereign == owner.sovereign && (s.position - owner.position).magnitude < 250)) {
                        nearby.UpdateGuardList();
                        if (nearby.guards.Count < 3) {
                            if (owner.guards.Count > 3) {
                                var g = owner.guards.Last();
                                g.controller = new GuardOrder(nearby);
                                nearby.guards.Add(g);
                                owner.guards.RemoveAt(owner.guards.Count - 1);
                            }
                        }
                    }
                }

            }
        }
    }
}
