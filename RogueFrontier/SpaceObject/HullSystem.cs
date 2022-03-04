﻿using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueFrontier;

public static class SDamageSystem {
    public static void React(this ActiveObject owner, ActiveObject source) {
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
    void Damage(int tick, Projectile p, Action Destroy);
}
public class HP : HullSystem {
    public int maxHP;
    public int hp;
    public int lastDamageTick;
    public HP(int maxHP) {
        this.maxHP = maxHP;
        this.hp = maxHP;
    }
    public void Damage(int tick, Projectile p, Action Destroy) {
        if (p.damageHP == 0)
            return;
        p.hitHull = true;
        var absorbed = Math.Min(hp, p.damageHP);
        hp -= absorbed;
        p.damageHP -= absorbed;
        lastDamageTick = tick;
        if (hp < 1) {
            Destroy();
        }
    }
    public int GetHP() => hp;
    public int GetMaxHP() => maxHP;
    public void Restore() => hp = maxHP;
}
//WMD would allow the attacker to hit multiple layers at a time, multiplying the damage
public class LayeredArmor : HullSystem {
    public List<Armor> layers;
    public int tick;
    public LayeredArmor(List<Armor> layers) {
        this.layers = layers;
    }
    public void Damage(int tick, Projectile p, Action Destroy) {
        if (p.damageHP == 0)
            return;
        p.hitHull = true;
        foreach (var i in Enumerable.Range(0, layers.Count).Reverse()) {
            var layer = layers[i];
            if (layer == null)
                continue;
            int absorbed = layer.Absorb(p);
            if (absorbed == 0)
                goto CheckDamage;
            layer.lastDamageTick = tick;

            int factor = p.fragment.shock;
            if (factor > 1) {

                //Factor:5
                //Depth Damage
                //0     100%
                //1     80%
                //2     60%
                //3     40%
                //4     20%
                factor--;
                foreach (var j in Enumerable.Range(i - factor, factor).Reverse().TakeWhile(j => j > -1)) {
                    var below = layers[j];
                    below.Absorb((absorbed * factor) / p.fragment.shock);
                    below.lastDamageTick = tick;
                    factor--;
                }
            }

            CheckDamage:
            if (p.damageHP == 0)
                return;
        }
        Destroy();
    }
    public List<ColoredString> GetDesc() =>
        new List<ColoredString>(layers.GroupBy(l => l.source.type).Select(l => new ColoredString(l.First().source.type.name + $" (x{l.Count()})")));

    public int GetHP() => layers.Sum(l => l.hp);
    public int GetMaxHP() => layers.Sum(l => l.source.armor.desc.maxHP);
    public void Restore() {
        layers.ForEach(l => l.hp = l.source.armor.desc.maxHP);
    }
}
