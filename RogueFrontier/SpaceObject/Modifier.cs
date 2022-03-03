using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RogueFrontier;
public record Modifier() {
    [Opt] public bool
        curse = false;
    [Opt] public int
        damageHPInc = 0,
        missileSpeedInc = 0,
        lifetimeInc = 0;
    [Opt] public double
        damageHPFactor = 1,
        missileSpeedFactor = 1,
        lifetimeFactor = 1;
    [Opt] public int
        maxHPInc = 0;
    [Opt] public double
        maxHPFactor = 1;
    public Modifier(XElement e) : this() {
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
            maxHPInc = x.maxHPInc + y.maxHPInc,
            maxHPFactor = x.maxHPFactor * y.maxHPFactor
        };
    public static FragmentDesc operator *(Modifier x, FragmentDesc y) =>
        y with {
            damageHP = IDice.Apply(y.damageHP, x.damageHPFactor, x.damageHPInc),
            missileSpeed = (int)((y.missileSpeed * x.missileSpeedFactor) + x.missileSpeedInc),
            lifetime = (int)((y.lifetime * x.lifetimeFactor) + x.lifetimeFactor),
        };
    public static ArmorDesc operator *(Modifier x, ArmorDesc y) =>
        y with {
            maxHP = (int)((y.maxHP * x.maxHPFactor) + x.maxHPInc),
        };
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
}
