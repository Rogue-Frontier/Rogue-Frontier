using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public static class SDamageSystem {
        public static void React(this DamageSystem ds, SpaceObject owner, SpaceObject source) {
            if (source is PlayerShip ps && !owner.Sovereign.IsEnemy(ps)) {
                ps.AddMessage(new Transmission(owner, new ColoredString($@"""Watch your targets!"" - {owner.Name}", Color.White, Color.Transparent), 1));
            }
        }
    }
    public interface DamageSystem {
        void Damage(SpaceObject source, int hp);
    }
    public class HPSystem : DamageSystem {
        SpaceObject owner;
        public int hp;
        public HPSystem(SpaceObject owner, int hp) {
            this.owner = owner;
            this.hp = hp;
        }
        public void Damage(SpaceObject source, int hp) {
            this.React(owner, source);
            this.hp -= hp;
            if(this.hp < 1) {
                owner.Destroy(source);
            }
        }
    }
    //WMD would allow the attacker to hit multiple layers at a time, multiplying the damage
    public class LayeredArmorSystem : DamageSystem {
        SpaceObject owner;
        public List<Armor> layers;
        public LayeredArmorSystem(SpaceObject owner, List<Armor> layers) {
            this.owner = owner;
            this.layers = layers;
        }
        public void Damage(SpaceObject source, int hp) {
            for(int i = layers.Count-1; i > -1; i--) {
                var layer = layers[i];
                if(layer == null) {
                    continue;
                } else {
                    int absorbed = Math.Min(layer.hp, hp);
                    layer.hp -= absorbed;
                    hp -= absorbed;
                    if(hp == 0) {
                        return;
                    }
                }
            }
            owner.Destroy(source);
        }
        public List<ColoredString> GetDesc() {
            return new List<ColoredString>(layers.GroupBy(l => l.source.type).Select(l => new ColoredString(l.First().source.type.name + $" (x{l.Count()})")));
        }
    }
}
