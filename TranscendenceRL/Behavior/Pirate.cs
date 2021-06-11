using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class Pirate : StationBehavior {
        int ticks = 0;
        public Pirate() { }
        public void Update(Station owner) {
            ticks++;
            if (ticks % 300 == 0) {

                foreach(var g in owner.guards) {
                    if (g.controller is GuardOrder order
                        && order.attackTime == -1
                        && CountHelpers(order.attackOrder.target, g) > 2) {
                        order.ClearAttack();
                    }
                }

                AIShip guard = null;
                GuardOrder guardOrder = null;
                foreach(var g in owner.guards) {
                    if(g.controller is GuardOrder order
                        && order.attackTime == 0) {
                        guard = g;
                        guardOrder = order;
                    }
                }
                if(guard == null) {
                    return;
                }
                var target = owner.world.entities.all
                    .OfType<IShip>()
                    .Where(s => owner.IsEnemy(s))
                    .Where(s => (s.position - owner.position).magnitude < 500)
                    .Where(s => CountHelpers(s, guard) < 3)
                    .Where(s => CountPirates(s) < 5)
                    .FirstOrDefault();
                if(target == null) {
                    return;
                }
                guardOrder.Attack(target, -1);
                
            }
            int CountHelpers(SpaceObject target, SpaceObject guard) {
                return target.world.entities.all
                        .OfType<SpaceObject>()
                        .Where(other => (other.position - target.position).magnitude < 150)
                        .Where(other => other.CanTarget(guard))
                        .Count();
            }

            int CountPirates(SpaceObject target) {
                return target.world.entities.all
                        .OfType<AIShip>()
                        .Where(s => s.sovereign == owner.sovereign)
                        .Where(s => s.controller is GuardOrder order && order.attackOrder?.target == target)
                        .Count();
            }
        }
    }
}
