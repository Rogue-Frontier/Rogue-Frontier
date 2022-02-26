using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using static RogueFrontier.StationType;
using Console = SadConsole.Console;
using Newtonsoft.Json;
using static RogueFrontier.Weapon;

namespace RogueFrontier;
public interface StationBehavior {
    void Update(Station owner);
    public void RegisterGuard(AIShip guard) { }
}

public class PirateStation : StationBehavior {
    int ticks = 0;
    public PirateStation() { }
    public void Update(Station owner) {
        ticks++;
        if (ticks % 300 == 0) {
            //Clear any pirate attacks where the target has too many defenders
            foreach (var g in owner.guards) {
                if (g.behavior.GetOrder() is GuardOrder o
                    && o.attackOrder.Active == true
                    && CountDefenders(o.attackOrder.target, g) > 2) {
                    o.ClearAttack();
                }
            }

            var targets = owner.world.entities.all
                        .OfType<IShip>()
                        .Where(s => owner.IsEnemy(s))
                        .Where(s => (s.position - owner.position).magnitude < 500)
                        .ToList();
            //Handle all available guards
            foreach (var g in owner.guards) {
                if (g.behavior.GetOrder() is GuardOrder { attackTime: < 1 } o) {
                    var target = targets.FirstOrDefault(
                        s => {
                            int attackers = CountAttackers(s), defenders = CountDefenders(s, g);
                            return attackers < 5 && defenders < 3;
                        });
                    if (target != null) {
                        o.SetAttack(target);
                    }
                }
            }

            //Count the number of objects that could defend this target from the attacker
            int CountDefenders(ActiveObject target, ActiveObject attacker) {
                return target.world.entities.all
                        .OfType<ActiveObject>()
                        .Where(other => (other.position - target.position).magnitude < 150)
                        .Where(other => other.CanTarget(attacker))
                        .Count();
            }
            //Count the number of ships already attacking this target
            int CountAttackers(ActiveObject target) {
                return target.world.entities.all
                        .OfType<AIShip>()
                        .Where(s => s.sovereign == owner.sovereign)
                        .Where(s => s.behavior.GetOrder().CanTarget(target))
                        .Count();
            }
        }
    }
}

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
                var generated = owner.type.ships.Generate(world.types, owner);
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
                            ((GuardOrder)g.behavior.GetOrder()).SetHome(nearby);
                            owner.guards.RemoveAt(owner.guards.Count - 1);
                            nearby.guards.Add(g);
                        }
                    }
                }
            }
        }
    }
}