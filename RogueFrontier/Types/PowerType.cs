using Common;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace RogueFrontier;

public class PowerType : DesignType {
    public string codename;
    public string name;
    public int cooldownTime;
    public int invokeDelay;
    public string message;

    public PowerEffect Effect;
    public void Initialize(TypeCollection collection, XElement e) {
        codename = e.ExpectAtt(nameof(codename));
        name = e.ExpectAtt(nameof(name));
        cooldownTime = e.ExpectAttInt(nameof(cooldownTime));
        invokeDelay = e.ExpectAttInt(nameof(invokeDelay));
        message = e.TryAtt(nameof(message), null);

        if (e.HasElement("Weapon", out var xmlWeapon)) {
            Effect = new PowerWeapon(xmlWeapon);
        } else if (e.HasElement("Heal", out var xmlHeal)) {
            Effect = new PowerHeal();
        } else if (e.HasElement("ProjectileBarrier", out var xmlProjectileBarrier)) {
            Effect = new PowerBarrier(xmlProjectileBarrier);
        } else if (e.HasElement("Jump", out var xmlJump)) {
            Effect = new PowerJump(e);
        } else {
            throw new Exception($"Power must have effect: {codename} ### {e} ### {e.Parent}");
        }
    }
    public void Invoke(PlayerShip player) => Effect.Invoke(player);
}
//Interface for invokable powers
public interface PowerEffect {
    //void Invoke(PlayerMain main);
    void Invoke(PlayerShip player);
    void OnDestroyCheck(PlayerShip player) { }
}
public record PowerJump() : PowerEffect {
    [Opt] public int distance;
    public PowerJump(XElement e) : this() => e.Initialize(this);
    public void Invoke(PlayerMain main) {
        main.Jump();
        Invoke(main.playerShip);
    }
    public void Invoke(PlayerShip player) =>
        player.position += XY.Polar(player.rotationRad, 100);
}
//Power that generates a weapon effect
public class PowerWeapon : PowerEffect {
    public FragmentDesc desc;
    public PowerWeapon() { }
    public PowerWeapon(XElement e) {
        desc = new FragmentDesc(e);
    }
    //public void Invoke(PlayerMain main) => Invoke(main.playerShip);
    public void Invoke(PlayerShip player) =>
        SWeapon.CreateShot(desc, player, player.rotationDeg * Math.PI / 180);
}
public class PowerHeal : PowerEffect {
    public PowerHeal() { }
    public PowerHeal(XElement e) {}
    //public void Invoke(PlayerMain main) => Invoke(main.playerShip);
    public void Invoke(PlayerShip player) {
        player.hull.Restore();
        player.devices.Shields.ForEach(s => s.hp = s.desc.maxHP);
        player.devices.Reactors.ForEach(r => r.energy = r.desc.capacity);
    }
}
public class PowerBarrier : PowerEffect {
    public enum BarrierType {
        block, echo, accuse
    }
    public BarrierType barrierType;
    [Req] public int radius;
    [Req] public int lifetime;
    public PowerBarrier() { }
    public PowerBarrier(XElement e) {
        e.Initialize(this);
        barrierType = e.ExpectAttEnum<BarrierType>(nameof(barrierType));
    }
    //public void Invoke(PlayerMain main) => Invoke(main.playerShip);
    public void Invoke(PlayerShip player) {
        var world = player.world;
        var end = 2 * Math.PI;
        Func<XY, int, ProjectileBarrier> construct = null;
        switch (barrierType) {
            case BarrierType.block: {
                    HashSet<Projectile> blocked = new HashSet<Projectile>();
                    construct = (position, lifetime) => new BlockBarrier(player, position, lifetime, blocked);
                    break;
                }
            case BarrierType.accuse: {
                    HashSet<Projectile> cloned = new HashSet<Projectile>();
                    construct = (position, lifetime) => new CloneBarrier(player, position, lifetime, cloned);
                    break;
                }
            case BarrierType.echo: {
                    HashSet<Projectile> reflected = new HashSet<Projectile>();
                    construct = (position, lifetime) => new ReflectBarrier(player, position, lifetime, reflected);
                    break;
                }
        }

        //HashSet<(int, int)> covered = new();
        for (double r = radius; r < radius + 2; r++) {
            double step = 1f / (r * 2);
            for (double angle = 0; angle < end; angle += step) {
                var p = XY.Polar(angle, r);
                /*
                if(covered.Contains(p)) {
                    continue;
                }
                covered.Add(p);
                */
                var barrier = construct(p, lifetime);
                world.AddEntity(barrier);
            }
        }
    }
}
public interface IPower {
    public int cooldownPeriod { get; }
    public int invokeDelay { get; }
    public bool ready => cooldownLeft == 0;
    public int cooldownLeft { get; set; }
    public int invokeCharge { get; set; }
    public bool charging { get; set; }
    public PowerEffect Effect { get; }
}
public class Power : IPower {
    public PowerType type;

    public int cooldownPeriod => type.cooldownTime;
    public int invokeDelay => type.invokeDelay;
    public bool fullyCharged => invokeCharge >= invokeDelay;

    public int cooldownLeft { get; set; }
    public bool ready => cooldownLeft == 0;
    public int invokeCharge { get; set; }
    public bool charging { get; set; }

    public PowerEffect Effect => type.Effect;
    public Power(PowerType type) {
        this.type = type;
    }
}
