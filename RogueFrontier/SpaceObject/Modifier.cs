using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RogueFrontier;
public record Modifier {
    public bool curse = false;

    public int damageHPInc = 0;
    public int missileSpeedInc = 0;
    public int lifetimeInc = 0;
    public Modifier() { }
    public Modifier(XElement e) {
        curse = e.TryAttributeBool(nameof(curse));
        damageHPInc=e.TryAttributeInt(nameof(damageHPInc));
        missileSpeedInc = e.TryAttributeInt(nameof(missileSpeedInc));
        lifetimeInc = e.TryAttributeInt(nameof(lifetimeInc));
    }

    public bool empty => this is Modifier {
        curse: false,
        damageHPInc: 0, missileSpeedInc:0, lifetimeInc: 0
    };
    
    public void ModifyRemoval(ref bool removable) {
        if (curse) {
            removable = false;
        }
    }
    public void ModifyWeapon(ref int damageHP, ref int missileSpeed, ref int lifetime) {
        damageHP += damageHPInc;
        missileSpeed += missileSpeedInc;
        lifetime += lifetimeInc;
    }
}
