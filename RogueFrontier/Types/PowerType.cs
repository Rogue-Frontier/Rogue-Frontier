using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
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

        var xmlPower = e.Elements().FirstOrDefault();
        switch (xmlPower.Name) {

        }

        Effect = xmlPower?.Name.LocalName switch {
            "Weapon" => new PowerWeapon(xmlPower),
            "Heal" => new PowerHeal(),
            "ProjectileBarrier" => new PowerBarrier(xmlPower),
            "Jump" => new PowerJump(xmlPower),
            "Storm" => new PowerStorm(xmlPower),
            "FastFire" => new Stonewall(xmlPower),
            _ => throw new Exception($"Power must have effect: {codename} ### {e} ### {e.Parent}")
        };
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
public record PowerStorm() : PowerEffect {
    public PowerStorm(XElement e) : this() => e.Initialize(this);
    public void Invoke(PlayerShip player) =>
        player.world.AddEffect(new StormOverlay(player));

    public class StormOverlay : Effect {
        PlayerShip owner;
        public XY position => owner.position;

        public bool active => owner.active;

        public ColoredGlyph tile => null;
        public StormOverlay(PlayerShip owner) {
            this.owner = owner;
        }
        public void Update() {
            var w = owner.GetPrimary();
            if(w != null) {
                var f = w.GetFragmentDesc();
                var p = new Projectile(owner, f,
                    owner.position + XY.Polar(0, 50),
                    owner.velocity + XY.Polar(0, -50),
                    f.GetManeuver(w.target));
                owner.world.AddEntity(p);
            }
        }
    }
}

public record Stonewall() : PowerEffect {
    public Stonewall(XElement e) : this() => e.Initialize(this);
    public void Invoke(PlayerShip player) =>
        player.world.AddEffect(new Overlay(player));

    public class Overlay : Effect {
        int ticks;
        PlayerShip owner;
        public XY position => owner.position;
        public bool active => owner.active;
        public ColoredGlyph tile => null;
        public Overlay(PlayerShip owner) {
            this.owner = owner;
            UpdateOffsets();
            directions = new double[offsets.Count];
        }
        private void UpdateOffsets() {
            offsets = new List<XY> {
                        XY.Polar(owner.rotationRad - Math.PI / 2, 6),
                        XY.Polar(owner.rotationRad - Math.PI / 2, 4),
                        XY.Polar(owner.rotationRad - Math.PI / 2, 2),
                        XY.Polar(owner.rotationRad + Math.PI / 2, 2),
                        XY.Polar(owner.rotationRad + Math.PI / 2, 4),
                        XY.Polar(owner.rotationRad + Math.PI / 2, 6),
                    };
        }
        private List<XY> offsets;
        private double[] directions;
        private FragmentDesc ready;
        private bool firing;
        public void Update() {
            if(owner.GetPrimary() is Weapon w) {
                if (w.delay == 0) {
                    ready = w.GetFragmentDesc();
                }
                firing |= w.delay == w.desc.fireCooldown;
                if (ticks++ % 5 == 0) {
                    int i = 0;

                    UpdateOffsets();
                    offsets.ForEach(o => {
                        var p = owner.position + o;
                        owner.world.AddEffect(new EffectParticle(p, owner.tile, 5));
                        if (ready == null) {
                            Heading.AimLine(owner.world, p, directions[i++], 5);
                        } else {
                            double d;
                            var t = w.target ?? owner.GetTarget();
                            if (t != null) {
                                d = Aiming.CalcFireAngle(t.position - (owner.position + o),
                                    t.velocity - owner.velocity,
                                    ready) ?? owner.rotationRad;
                            } else {
                                d = owner.rotationRad;
                            }
                            directions[i++] = d;
                            Heading.AimLine(owner.world, p, d, 5);
                        }
                    });
                    if (ready != null && firing) {
                        i = 0;
                        offsets.ForEach(o => {
                            var l = ready.GetProjectile(owner, w, directions[i++]);
                            l.ForEach(p => p.position += o);
                            l.ForEach(owner.world.AddEntity);
                            w.ammo?.OnFire();
                        });

                        //w.delay = 0;
                        //w.Fire(owner, w.aiming?.GetFireAngle() ?? owner.rotationRad);
                        ready = null;
                        firing = false;
                    }
                }
            }
        }
    }
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
        player.devices.Shield.ForEach(s => s.hp = s.desc.maxHP);
        player.devices.Reactor.ForEach(r => r.energy = r.desc.capacity);
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
