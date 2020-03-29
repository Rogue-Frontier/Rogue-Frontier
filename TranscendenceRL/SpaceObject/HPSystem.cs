using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class HPSystem {
        SpaceObject owner;
        public int hp;
        public HPSystem(SpaceObject owner, int hp) {
            this.owner = owner;
            this.hp = hp;
        }
        public void Damage(SpaceObject source, int hp) {
            if(source is PlayerShip ps) {
                ps.AddMessage(new PlayerMessage(new ColoredString($@"""Watch your targets!"" - {owner.Name}", Color.White, Color.Transparent), 1));
            }
            this.hp -= hp;
            if(this.hp < 1) {
                owner.Destroy();
            }
        }
    }
}
