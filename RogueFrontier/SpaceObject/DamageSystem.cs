using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueFrontier;

public static class SDamageSystem {
    public static void React(this SpaceObject owner, SpaceObject source) {
        if (source is PlayerShip ps && !owner.sovereign.IsEnemy(ps) && !owner.CanTarget(ps)) {
            ps.AddMessage(new Transmission(owner, $@"""Watch your targets!"" - {owner.name}", 1));
        }
    }
    public static void DestroyCheck(this PlayerShip ps, Projectile pr) =>
        ps.powers.ForEach(p => p.OnDestroyCheck(ps, pr));
}
public interface HullSystem {
    void Restore();
    int GetHP();
    int GetMaxHP();
    void Damage(SpaceObject owner, Projectile p);
}
public class HPSystem : HullSystem {
    public int maxHP;
    public int hp;
    public int lastDamageTick;
    public HPSystem(int maxHP) {
        this.maxHP = maxHP;
        this.hp = maxHP;
    }
    public void Damage(SpaceObject owner, Projectile p) {
        owner.React(p.source);

        var ps = owner as PlayerShip;
        Handle:
        var absorbed = Math.Min(hp, p.damageHP);
        hp -= absorbed;
        p.damageHP -= absorbed;
        lastDamageTick = owner.world.tick;

        if (ps != null) {
            ps = null;
            ps.DestroyCheck(p);
            goto Handle;
        }

        if (hp < 1) {
            owner.Destroy(p.source);
        }
    }
    public int GetHP() => hp;
    public int GetMaxHP() => maxHP;
    public void Restore() => hp = maxHP;
}
//WMD would allow the attacker to hit multiple layers at a time, multiplying the damage
public class LayeredArmorSystem : HullSystem {
    public List<Armor> layers;
    public int tick;
    public LayeredArmorSystem(List<Armor> layers) {
        this.layers = layers;
    }
    public void Damage(SpaceObject owner, Projectile p) {
        owner.React(p.source);
        ref int hp = ref p.damageHP;
        var tick = owner.world.tick;

        var ps = owner as PlayerShip;

        Handle:
        foreach (var i in Enumerable.Range(0, layers.Count).Reverse()) {
            var layer = layers[i];
            if (layer == null || layer.hp == 0)
                continue;
            int absorbed = Math.Min(layer.hp, hp);
            layer.hp -= absorbed;
            hp -= absorbed;
            layer.lastDamageTick = tick;

            int depth = 2;
            foreach (var j in Enumerable.Range(i - p.fragment.shock, p.fragment.shock).Reverse().Where(j => j > -1)) {
                var nextLayer = layers[j];
                int nextAbsorbed = Math.Min(nextLayer.hp, absorbed / depth);
                nextLayer.hp -= nextAbsorbed;
                nextLayer.lastDamageTick = tick;
                depth++;
            }
            if (hp == 0)
                return;
        }
        if (ps != null) {
            ps = null;
            ps.DestroyCheck(p);
            goto Handle;
        }
        owner.Destroy(p.source);
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
