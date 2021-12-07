using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier {
    public class Pirate : StationBehavior {
        int ticks = 0;
        public Pirate() { }
        public void Update(Station owner) {
            ticks++;
            if (ticks % 300 == 0) {
                //Clear any pirate attacks where the target has too many defenders
                foreach(var g in owner.guards) {
                    if (g.order is GuardOrder order
                        && order.attackOrder?.Active == true
                        && CountDefenders(order.attackOrder.target, g) > 2) {
                        order.ClearAttack();
                    }
                }

                var targets = owner.world.entities.all
                            .OfType<IShip>()
                            .Where(s => owner.IsEnemy(s))
                            .Where(s => (s.position - owner.position).magnitude < 500);
                //Handle all available guards
                foreach (var g in owner.guards) {
                    if(g.order is GuardOrder o && o.attackTime < 1) {
                        var target = targets.FirstOrDefault(
                            s => CountAttackers(s) < 5 && CountDefenders(s, g) < 3);
                        if (target != null) {
                            o.Attack(target);
                        }
                    }
                }

                //Count the number of objects that could defend this target from the attacker
                int CountDefenders(SpaceObject target, SpaceObject attacker) {
                    return target.world.entities.all
                            .OfType<SpaceObject>()
                            .Where(other => (other.position - target.position).magnitude < 150)
                            .Where(other => other.CanTarget(attacker))
                            .Count();
                }
                //Count the number of ships already attacking this target
                int CountAttackers(SpaceObject target) {
                    return target.world.entities.all
                            .OfType<AIShip>()
                            .Where(s => s.sovereign == owner.sovereign)
                            .Where(s => s.order is GuardOrder o && o.attackOrder?.target == target)
                            .Count();
                }
            }
        }
    }
}
