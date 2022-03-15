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
    public PirateStation() { }
    public void Update(Station owner) {
        if (owner.world.tick % 300 == 0) {
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
    public ReinforceNearby() { }
    public void Update(Station owner) {
        if (owner.world.tick % 150 == 0) {
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



public class OrionWarlordsStation : StationBehavior, IContainer<Station.Destroyed>, IContainer<GuardOrder.OnDocked> {

    private HashSet<ActiveObject> turretsDeployed = new();

    public Station.Destroyed Value => (station, destroyer, wreck) => {
        if (destroyer?.active != true) {
            return;
        }
        if (station.world.entities.all
            .OfType<Station>()
            .Where(s => s.active && s.sovereign == station.sovereign && s.UpdateGuardList().Any())
            .OrderBy(s => (s.position - destroyer.position).magnitude2)
            .FirstOrDefault() is Station s) {
            s.guards.ForEach(g => g.behavior = new GuardOrder(s, destroyer));
        }

        if(station.world.entities.all.OfType<Stargate>().FirstOrDefault(g => g.destGate != null) is Stargate g) {
            var nextGate = g.destGate;
            var nextSystem = nextGate.world;
            var pos = nextGate.position + XY.Polar(station.world.karma.NextDouble() * 2 * Math.PI, 20);
            foreach(var st in nextSystem.entities.all.OfType<Station>().Where(s => s.type.codename == station.type.codename)) {
                if(st.guards.FirstOrDefault() is AIShip ai) {
                    ai.behavior = new GuardOrder(new ActiveMarker(nextSystem, station.sovereign, nextGate.position));
                }
            }
        }
    };

    GuardOrder.OnDocked IContainer<GuardOrder.OnDocked>.Value => (ship, home) => {
        if (turretsDeployed.Contains(home)) {
            return;
        }
        turretsDeployed.Add(home);

        var w = ship.world;
        var turret = new Station(w, turretType, home.position);
        w.AddEntity(turret);
        turret.CreateSegments();
    };
    StationType turretType;
    public OrionWarlordsStation(Station owner) {
        owner.onDestroyed += this;
        turretType = owner.world.types.Lookup<StationType>("station_orion_turret");
    }
    public void Update(Station owner) {
        if(owner.world.tick%1200 == 0) {
            if(owner.guards.Count > 5) {
                var g = owner.guards.Take(1).ToList();
                
                var k = owner.world.karma;

                var enemies = owner.world.entities.all.OfType<ActiveObject>().Where(a => owner.CanTarget(a));

                foreach(var enemy in enemies) {
                    XY p = owner.position + XY.Polar(k.NextDouble() * Math.PI * 2, k.NextInteger(80, 400));
                    if (enemies.All(a => (a.position - p).magnitude > 50) && enemies.Any(a => (a.position - p).magnitude < 150)) {
                        var ambushPoint = new ActiveMarker(owner.world, owner.sovereign, p);
                        var o = new GuardOrder(ambushPoint);
                        o.onDocked += this;
                        g.ForEach(g => g.behavior = new CompoundOrder(o, new GuardOrder(owner)));
                        break;
                    }
                }
            }
            
        }
    }
}