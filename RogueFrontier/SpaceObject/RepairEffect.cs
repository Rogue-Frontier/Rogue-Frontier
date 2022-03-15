using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;

public class RepairEffect : Event {
    public bool active { get; set; } = true;
    public int ticks;

    PlayerShip player; 
    public Armor armor; 
    int interval; 
    int costPerHp; 
    Action<RepairEffect> done;
    public bool terminated;

    public RepairEffect(PlayerShip player, Armor armor, int interval, int costPerHp, Action<RepairEffect> done) {
        this.player = player;
        this.armor = armor;
        this.interval = interval;
        this.costPerHp = costPerHp;
        this.done = done;
    }
    public void Update() {
        ticks++;
        if(ticks%interval != 0) {
            return;
        }
        if(armor.hp < armor.desc.maxHP) {
            var next = player.person.money - costPerHp;
            if (next < 0) {
                terminated = true;
                Kill();
            } else {
                armor.hp++;
                player.person.money -= costPerHp;
            }
        } else {
            Kill();
        }
        void Kill() {
            active = false;
            done?.Invoke(this);
        }
    }
}
