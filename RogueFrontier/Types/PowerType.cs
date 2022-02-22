using Common;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
namespace RogueFrontier;
public record PowerType() : IDesignType {
    [Req] public string codename;
    [Req] public string name;
    [Req] public int cooldownTime;
    [Req] public int invokeDelay;
    [Opt] public bool onDestroyCheck = false;
    [Opt] public string message = null;
    public List<PowerEffect> Effect;
    public void Initialize(TypeCollection collection, XElement e) {
        var parent = e.TryAtt("inherit", out var inherit) ? collection.Lookup<PowerType>(inherit) : null;
        e.Initialize(this, parent);
        Effect = new(e.Elements().Select(e => (PowerEffect)(e.Name.LocalName switch {
            "Projectile" => new PowerProjectile(e),
            "Heal" => new PowerHeal(),
            "ProjectileBarrier" => new PowerBarrier(e),
            "Jump" => new PowerJump(e),
            "Storm" => new PowerStorm(e),
            "Clonewall" => new Clonewall(e),
            _ => throw new Exception($"Unknown PowerEffect type: {e.Name.LocalName} ### {e} ### {e.Parent}")
        })));
        if(Effect.Count == 0) {
            throw new Exception($"Power must have effect: {codename} ### {e} ### {e.Parent}");
        }
    }
    public void Invoke(PlayerShip player) => Effect.ForEach(e => e.Invoke(player));
}
public interface PowerEffect {
    void Invoke(PlayerShip player);
}
public record PowerJump() : PowerEffect {
    [Opt] public int distance = 100;
    public PowerJump(XElement e) : this() => e.Initialize(this);
    public void Invoke(PlayerMain main) {
        main.Jump();
        Invoke(main.playerShip);
    }
    public void Invoke(PlayerShip player) =>
        player.position += XY.Polar(player.rotationRad, distance);
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
        public StormOverlay(PlayerShip owner) =>
            this.owner = owner;
        public void Update() {
            var w = owner.GetPrimary();
            if(w != null) {
                var f = w.fragmentDesc;
                var p = new Projectile(owner, f,
                    owner.position + XY.Polar(0, 50),
                    owner.velocity + XY.Polar(0, -50),
                    f.GetManeuver(w.target));
                owner.world.AddEntity(p);
            }
        }
    }
}
public record Clonewall() : PowerEffect {
    [Opt] public int lifetime = 60 * 60;
    public Clonewall(XElement e) : this() => e.Initialize(this);
    public void Invoke(PlayerShip player) {
        var o = new Overlay(player);
        player.world.AddEffect(o);
        player.onWeaponFire += o;
    }
    public class Overlay : Effect, IContainer<PlayerShip.WeaponFired> {
        int ticks;
        PlayerShip owner;
        public XY position => owner.position;
        public bool active => owner.active && owner.world.effects.Contains(this);
        public ColoredGlyph tile => null;


        private List<XY> offsets;
        private double[] directions;
        private FragmentDesc ready;
        public Overlay(PlayerShip owner) {
            this.owner = owner;
            UpdateOffsets();
            directions = new double[offsets.Count];
        }
        private void UpdateOffsets() =>
            offsets = new() {
                        XY.Polar(owner.rotationRad - Math.PI / 2, 6),
                        XY.Polar(owner.rotationRad - Math.PI / 2, 4),
                        XY.Polar(owner.rotationRad - Math.PI / 2, 2),
                        XY.Polar(owner.rotationRad + Math.PI / 2, 2),
                        XY.Polar(owner.rotationRad + Math.PI / 2, 4),
                        XY.Polar(owner.rotationRad + Math.PI / 2, 6),
                    };
        PlayerShip.WeaponFired IContainer<PlayerShip.WeaponFired>.Value => (p, w, pr) => {
            if (!active) {
                p.onWeaponFire -= this;
                return;
            }
            foreach(var projectile in pr) {
                var fragment = projectile.fragment;
                int i = 0;
                var target = w.target;
                offsets.ForEach(o => {
                    var l = fragment.GetProjectiles(owner, target, directions[i++]);
                    l.ForEach(p => p.position += o);
                    l.ForEach(owner.world.AddEntity);
                    w.ammo?.OnFire();
                });
            }
        };
        public void Update() {
            ticks++;
            if(owner.GetPrimary() is Weapon w) {
                if (w.delay == 0) {
                    ready = w.fragmentDesc;
                }
                const int interval = 6;
                if (ticks % interval == 0) {
                    int i = 0;
                    UpdateOffsets();
                    var tile = owner.tile;
                    var cg = new ColoredGlyph(Color.Transparent, Color.White.Blend(Color.Red.SetAlpha(204)).SetAlpha(204), ' ');
                    offsets.ForEach(o => {
                        var p = owner.position + o;

                        //owner.world.AddEffect(new EffectParticle(p, cg, interval * 5));

                        owner.world.AddEffect(new EffectParticle(p, tile, interval + 2));
                        if (ready == null) {
                            Heading.AimLine(owner.world, p, directions[i++], interval);
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
                            Heading.AimLine(owner.world, p, d, interval);
                        }
                    });
                }
            }
        }
    }
}
//Power that generates a weapon effect
public class PowerProjectile : PowerEffect {
    public FragmentDesc desc;
    public PowerProjectile() { }
    public PowerProjectile(XElement e) {
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
        shield, bubble, bounce, multiplyAttack
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
            case BarrierType.shield: {
                    HashSet<Projectile> blocked = new HashSet<Projectile>();
                    construct = (position, lifetime) => new ShieldBarrier(player, position, lifetime, blocked);
                    break;
                }
            case BarrierType.bubble: {
                    HashSet<Projectile> blocked = new HashSet<Projectile>();
                    construct = (position, lifetime) => new BubbleBarrier(player, position, lifetime, blocked);
                    break;
                }
            case BarrierType.multiplyAttack: {
                    HashSet<Projectile> cloned = new HashSet<Projectile>();
                    construct = (position, lifetime) => new CloneBarrier(player, position, lifetime, cloned);
                    break;
                }
            case BarrierType.bounce: {
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
    public List<PowerEffect> Effect { get; }
}
public class Power : IPower {
    [JsonProperty]
    public PowerType type;
    [JsonIgnore]
    public int cooldownPeriod => type.cooldownTime;
    [JsonIgnore]
    public int invokeDelay => type.invokeDelay;
    [JsonIgnore]
    public bool fullyCharged => invokeCharge >= invokeDelay;
    public int cooldownLeft { get; set; }
    [JsonIgnore]
    public bool ready => cooldownLeft == 0;
    public int invokeCharge { get; set; }
    public bool charging { get; set; }
    [JsonIgnore]
    public List<PowerEffect> Effect => type.Effect;

    public Power(PowerType type) {
        this.type = type;
    }
    public void OnDestroyCheck(PlayerShip player, Projectile p) {
        if (type.onDestroyCheck) {
            p.damageHP = 0;
            type.Effect.ForEach(e=>e.Invoke(player));
        }
    }
}
