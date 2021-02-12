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
                ps.AddMessage(new Transmission(owner, $@"""Watch your targets!"" - {owner.Name}", 1));
            }
        }
    }
    public interface DamageSystem {
        void Restore();
        int GetHP();
        int GetMaxHP();
        void Damage(SpaceObject owner, SpaceObject source, int hp);
    }
    public class HPSystem : DamageSystem {
        public int maxHP;
        public int hp;
        public HPSystem(int maxHP) {
            this.maxHP = maxHP;
            this.hp = maxHP;
        }
        public void Damage(SpaceObject owner, SpaceObject source, int hp) {
            this.React(owner, source);
            this.hp -= hp;
            if(this.hp < 1) {
                owner.Destroy(source);
            }
        }
        public int GetHP() => hp;
        public int GetMaxHP() => maxHP;
        public void Restore() => hp = maxHP;
    }
    //WMD would allow the attacker to hit multiple layers at a time, multiplying the damage
    public class LayeredArmorSystem : DamageSystem {
        public List<Armor> layers;
        public LayeredArmorSystem(List<Armor> layers) {
            this.layers = layers;
        }
        public void Damage(SpaceObject owner, SpaceObject source, int hp) {
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

        public int GetHP() => layers.Sum(l => l.hp);
        public int GetMaxHP() => layers.Sum(l => l.source.armor.desc.maxHP);
        public void Restore() {
            layers.ForEach(l => l.hp = l.source.armor.desc.maxHP);
        }
    }
}
