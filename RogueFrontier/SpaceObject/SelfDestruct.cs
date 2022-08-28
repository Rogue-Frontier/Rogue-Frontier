using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;
public class SelfDestruct : Event {
    public bool active => target.active;
    public PlayerShip target;
    public Message message;
    public int ticks;

    public SelfDestruct(PlayerShip target, int ticks) {
        this.target = target;
        this.ticks = ticks;
        message = new("");
        target.AddMessage(message);
    }
    public void Update(double delta) {
        if (ticks-- > 0) {
            message.message.String = $"Self destructing in {ticks / 60}s.";
            target.AddMessage(message);
        } else {
            message.message.String = $"Self destructed completed.";
            target.AddMessage(message);

            target.Destroy(null);
            //target.Damage(new(target, new() { armorDrill = 1, shieldDrill = 1, damageHP = new Constant(int.MaxValue), effect=new() }, target.position, new()));
        }
        
    }
}
