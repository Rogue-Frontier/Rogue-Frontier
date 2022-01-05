using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RogueFrontier;
public record Modifier {
    [Opt]
    public bool curse = false;
    [Opt]
    public int damageHPInc = 0,
        missileSpeedInc = 0,
        lifetimeInc = 0;
    [Opt]
    public double damageHPFactor = 1,
        missileSpeedFactor = 1,
        lifetimeFactor = 1;
    [Opt]
    public int maxHPInc = 0;
    public Modifier() { }
    public Modifier(XElement e) {
        e.Initialize(this);
    }
    public static Modifier Sum(params Modifier[] mods) {
        Modifier result = new();
        foreach (var m in mods) result += m;
        return result;
    }
    public static Modifier operator +(Modifier x, Modifier y) =>
        y == null ? x : 
        new() {
            curse = y.curse,
            damageHPInc = x.damageHPInc + y.damageHPInc,
            missileSpeedInc = x.missileSpeedInc + y.missileSpeedInc,
            lifetimeInc = x.lifetimeInc + y.lifetimeInc,
            damageHPFactor = x.damageHPFactor * y.damageHPFactor,
            missileSpeedFactor = x.missileSpeedFactor * y.missileSpeedFactor,
            lifetimeFactor = x.lifetimeFactor * y.lifetimeFactor,
        };


    public static FragmentDesc operator *(Modifier x, FragmentDesc y) =>
        x.ModifyWeapon(y);
    public bool empty => this is Modifier {
        curse: false,
        damageHPInc: 0, missileSpeedInc: 0, lifetimeInc: 0,
        damageHPFactor: 1, missileSpeedFactor: 1, lifetimeFactor: 1
    };
    
    public void ModifyRemoval(ref bool removable) {
        if (curse) {
            removable = false;
        }
    }
    public FragmentDesc ModifyWeapon(FragmentDesc d) {
        var damageHP = d.damageHP;
        if (damageHPFactor != 1)
            damageHP = new DiceFactor(damageHP, damageHPFactor);
        if (damageHPInc != 0)
            damageHP = new DiceInc(damageHP, damageHPInc);
        return d with {
            damageHP = damageHP,
            missileSpeed = (int) (d.missileSpeed * missileSpeedFactor + missileSpeedInc),
            lifetime = (int) (d.lifetime * lifetimeFactor + lifetimeInc),
            mod = this
        };
    }
    public void ModifyWeapon(ref FragmentDesc d) {
        d = ModifyWeapon(d);
    }
}
