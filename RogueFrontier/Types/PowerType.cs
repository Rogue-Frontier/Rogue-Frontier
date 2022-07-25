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
            "Reveal" => new PowerReveal(),
            "ProjectileBarrier" => new PowerBarrier(e),
            "Jump" => new PowerJump(e),
            "Storm" => new PowerStorm(e),
            "Clonewall" => new Clonewall(e),
            "RechargeWeapon" => new PowerRechargeWeapon(collection, e),
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
public record PowerRechargeWeapon() : PowerEffect {
    [Req] public int maxCharges;
    WeaponDesc weaponType;
    public PowerRechargeWeapon(TypeCollection tc, XElement e) : this() {
        e.Initialize(this);
        weaponType = tc.Lookup<ItemType>(e.ExpectAtt("codename")).weapon
                        ?? throw new Exception();
    }
    public void Invoke(PlayerShip player) {
        if(player.devices.Weapon.FirstOrDefault(w => w.desc == weaponType) is Weapon w) {
            ref int c = ref ((ChargeAmmo)w.ammo).charges;
            c = Math.Max(c, maxCharges);
        }
    }
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
                var f = w.projectileDesc;
                var p = new Projectile(owner, f,
                    owner.position + XY.Polar(0, 50),
                    owner.velocity + XY.Polar(0, -50),
                    maneuver:f.GetManeuver(w.target));
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
        private bool busy = false;
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
            //Don't clone any Power attacks
            if(w.desc == null) {
                return;
            }
            if(busy) {
                return;
            }
            var fragment = w.projectileDesc;
            int i = 0;
            var target = w.target;
            busy = true;
            offsets.ForEach(o => {
                var l = w.CreateProjectiles(owner, target, directions[i++]);
                l.ForEach(p => p.position += o);
                l.ForEach(owner.world.AddEntity);
                w.ammo?.OnFire();

                w.onFire.ForEach(f => f(w, l));
            });
            busy = false;
        };
        public void Update() {
            ticks++;
            if(owner.GetPrimary() is Weapon w) {
                if (w.delay == 0) {
                    ready = w.projectileDesc;
                }
                const int interval = 6;
                if (ticks % interval != 0) {
                    return;
                }
                int i = 0;
                UpdateOffsets();
                var tile = owner.tile;
                var cg = new ColoredGlyph(Color.Transparent, Color.White.Blend(Color.Red.SetAlpha(204)).SetAlpha(204), ' ');
                offsets.ForEach(o => {
                    var p = owner.position + o;
                    owner.world.AddEffect(new EffectParticle(p, tile, interval + 2));
                    if (ready != null) {
                        var t = w.target ?? owner.GetTarget();
                        directions[i] = t == null ? owner.rotationRad :
                            Main.CalcFireAngle(t.position - p, t.velocity - owner.velocity, ready.missileSpeed, out var _);
                    }
                    Heading.AimLine(owner.world, p, directions[i], interval);
                    i++;
                    //Bug: friendly fire issues
                });
            }
        }
    }
}
//Power that generates a weapon effect
public record PowerProjectile() : PowerEffect {
    public FragmentDesc desc;
    public PowerProjectile(XElement e) : this() {
        desc = new FragmentDesc(e);
    }
    //public void Invoke(PlayerMain main) => Invoke(main.playerShip);
    public void Invoke(PlayerShip player) =>
        new Weapon() { projectileDesc = desc}.Fire(player, player.rotationRad);
}
public record PowerHeal() : PowerEffect {
    public PowerHeal(XElement e) : this() { }
    //public void Invoke(PlayerMain main) => Invoke(main.playerShip);
    public void Invoke(PlayerShip player) {
        player.hull.Restore();
        player.devices.Shield.ForEach(s => s.hp = s.desc.maxHP);
        player.devices.Reactor.ForEach(r => r.energy = r.desc.capacity);
    }
}
public record PowerReveal() : PowerEffect {
    public PowerReveal(XElement e) : this(){ }
    //public void Invoke(PlayerMain main) => Invoke(main.playerShip);
    public void Invoke(PlayerShip player) {
        var enemies = player.world.entities.all
            .OfType<ActiveObject>()
            .Where(a => (a.position - player.position).magnitude2 < 360 * 360
                        && a.CanTarget(player)
                        && SStealth.GetStealth(a) > 0);
        foreach (var e in enemies) {
            var time = 1800;
            if(player.tracking.TryGetValue(e, out var t)) {
                time = Math.Max(time, t);
            }
            player.tracking[e] = time;
        }
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
        if (type.onDestroyCheck && ready) {
            cooldownLeft = cooldownPeriod;
            p.damageHP = 0;
            player.ship.damageSystem.Restore();
            type.Effect.ForEach(e=>e.Invoke(player));
            if (type.message != null) {
                player.AddMessage(new Message(type.message));
            }
        }
    }
}
