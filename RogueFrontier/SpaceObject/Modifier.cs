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
    [Opt<double>(1)]
    public double damageHPFactor = 1,
        missileSpeedFactor = 1,
        lifetimeFactor = 1;
    public Modifier() { }
    public Modifier(XElement e) {
        e.Initialize(this);
    }
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
            lifetime = (int) (d.lifetime * lifetimeFactor + lifetimeInc)
        };
    }
    public void ModifyWeapon(ref FragmentDesc d) {
        d = ModifyWeapon(d);
    }
}
