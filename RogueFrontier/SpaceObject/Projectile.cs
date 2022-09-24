using Common;
using SadRogue.Primitives;
using SadConsole;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System;
using ASECII;

namespace RogueFrontier;
public interface ITrail {
    Effect GetParticle(XY Position);
}
public record SimpleTrail(StaticTile Tile) : ITrail {
    public Effect GetParticle(XY Position) => new EffectParticle(Position, Tile, 3);
}
public class Projectile : MovingObject {
    [JsonProperty] public ulong id { get; set; }
    [JsonProperty] public System world { get; set; }
    [JsonProperty] public ActiveObject source;
    [JsonProperty] public XY position { get; set; }
    [JsonProperty] public XY velocity { get; set; }
    [JsonProperty] public double direction;
    [JsonProperty] public double lifetime { get; set; }
    [JsonProperty] public ColoredGlyph tile { get; private set; }
    [JsonProperty] public ITrail trail;
    [JsonProperty] public FragmentDesc desc;
    [JsonProperty] public Maneuver maneuver;
    [JsonProperty] public int damageHP;
    [JsonProperty] public int armorSkip;
    [JsonProperty] public int ricochet = 0;

    //Hit results
    [JsonProperty] public bool hitBlocked;
    [JsonProperty] public bool hitHull;
    public HashSet<Entity> exclude = new();
    //List of projectiles that were created from the same fragment
    public List<Projectile> salvo = new();
    public record OnHitActive(Projectile p, ActiveObject other);
    public Vi<OnHitActive> onHitActive=new();
    [JsonIgnore]   public bool active => _active;
    public bool _active = true;
    public Projectile() { }
    public Projectile(ActiveObject source, FragmentDesc desc, XY position, XY velocity, double? direction = null, Maneuver maneuver = null, HashSet<Entity> exclude = null) {
        this.id = source.world.nextId++;
        this.source = source;
        this.world = source.world;
        this.tile = desc.effect.Original;
        this.position = position;
        this.velocity = velocity;
        this.direction = direction ?? velocity.angleRad;
        this.lifetime = desc.lifetime;
        this.desc = desc;
        this.trail = (ITrail)desc.trail ?? new SimpleTrail(desc.effect.Original);
        this.maneuver = maneuver;
        this.damageHP = desc.damageHP.Roll();
        this.armorSkip = desc.armorSkip;
        this.ricochet = desc.ricochet;

        this.exclude = exclude;
        if(this.exclude == null) {
            this.exclude = desc.hitSource ?
                new() { null, this } :
                new() { null, source, this };
            this.exclude.UnionWith(source switch {
                PlayerShip ps => ps.avoidHit,
                AIShip ai => ai.avoidHit,
                Station st => st.guards,
                _ => new HashSet<Entity>()
            });
        }
        //exclude.UnionWith(source.world.entities.all.OfType<ActiveObject>().Where(a => a.sovereign == source.sovereign));
    }
    public void Update(double delta) {
        if (lifetime > 0) {
            lifetime -= delta * 60;
            UpdateMove();
            foreach (var f in desc.fragments) {
                if (f.fragmentInterval > 0 && lifetime % f.fragmentInterval == 0) {
                    Fragment(f);
                }
            }
        } else if(active) {
            UpdateMove();
            Detonate();
            _active = false;
        }
        void UpdateMove() {
            maneuver?.Update(delta, this);

            var dest = position + velocity * delta;
            var inc = velocity.normal * 0.5;
            var steps = velocity.magnitude * 2 * delta;

            if(desc.detonateRadius > 0) {
                var r = desc.detonateRadius * desc.detonateRadius;
                if(world.entities.FilterKey(p => (position - p).magnitude2 < r).Select(e => e is ISegment s ? s.parent : e)
                    .Distinct().Except(exclude).Any(e => e switch {
                        ActiveObject a when a.active => true,
                        Projectile p when !exclude.Contains(p.source) && desc.hitProjectile => true,
                        _ => false
                    })) {
                    lifetime = 0;
                    Detonate();
                    return;
                }
            }
            for (int i = 0; i < steps; i++) {
                position += inc;

                bool destroyed = false;
                bool stop = false;

                var entities = world.entities[position].Select(e => e is ISegment s ? s.parent : e).Distinct().Except(exclude);
                foreach (var other in entities) {
                    switch (other) {
                        case Asteroid a:
                            lifetime = 0;
                            destroyed = true;
                            break;
                        case ActiveObject hit when !destroyed && hit.active:
                            hit.Damage(this);
                            var angle = (position - hit.position).angleRad;
                            var cg = new ColoredGlyph(hitHull ? Color.Yellow : Color.LimeGreen, Color.Transparent, 'x');
                            world.AddEffect(new EffectParticle(hit.position + XY.Polar(angle), hit.velocity, cg, 10));
                            if(ricochet > 0) {
                                ricochet--;
                                velocity = -velocity;
                                //velocity += (hit.velocity - velocity) / 2;
                                //stop = true;
                            } else {
                                onHitActive.Observe(new(this, hit));
                                Detonate();
                                
                                lifetime = 0;
                                destroyed = true;
                                //new FlashDesc() { intensity = 5000 }.Create(world, position);
                            }
                            break;
                        case Projectile p when !exclude.Contains(p.source) && desc.hitProjectile && !destroyed:
                            /*
                            var delta = Math.Min(p.damageHP, damageHP);
                            if((p.damageHP -= delta) <= 0) {
                                p.lifetime = 0;
                            }
                            if((damageHP -= delta) == 0) {
                                lifetime = 0;
                                destroyed = true;
                            }
                            */
                            p.lifetime = 0;
                            lifetime = 0;
                            destroyed = true;
                            break;
                        case ProjectileBarrier barrier when desc.hitBarrier:
                            barrier.Interact(this);
                            stop = true;
                            //Keep interacting with all the barriers
                            break;
                    }
                }
                if (stop || destroyed) {
                    return;
                }
            CollisionDone:
                world.AddEffect(trail.GetParticle(position));
            }
            position = dest;
        }
    }
    public record Detonated(Projectile source) { };
    public Vi<Detonated> onDetonated = new();
    public void Detonate() {

        var d = new Detonated(this);
        onDetonated.Observe(d);

        if (desc.flash is FlashDesc fl) {
            fl.Create(world, position);
        }
        if (desc.fragments == null) return;


        foreach (var f in desc.fragments) {
            Fragment(f);
        }
    }
    public void Fragment(FragmentDesc fragment) {
        if (fragment.targetLocked != null
            && fragment.targetLocked != (maneuver?.target != null)) {
            return;
        }
        double angleInterval = fragment.spreadAngle / fragment.count;
        double fragmentAngle;

        if (fragment.omnidirectional
            && maneuver?.target is ActiveObject target
            && target.active == true
            && (target.position - position) is XY offset
            && offset.magnitude < fragment.range) {
            fragmentAngle = Main.CalcFireAngle((target.position - position), (target.velocity - velocity), fragment.missileSpeed, out var _);
        } else {
            fragmentAngle = direction;
        }
        HashSet<Entity> exclude = desc.hitSource ?
            new() { null, this } :
            new() { null, source, this };
        List<Projectile> salvo = new();
        for (int i = 0; i < fragment.count; i++) {
            double angle = fragmentAngle + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
            var p = new Projectile(source,
                fragment,
                position + XY.Polar(angle, 0.5),
                velocity + XY.Polar(angle, fragment.missileSpeed),
                angle,
                fragment.GetManeuver(maneuver?.target),
                exclude
                ) { salvo = salvo };
            salvo.Add(p);
            world.AddEntity(p);
        }
        
    }
}

public class Maneuver {
    public ActiveObject target;
    public double maneuver;
    public double maneuverDistance;
    public Maneuver(ActiveObject target, double maneuver, double maneuverDistance) {
        this.target = target;
        this.maneuver = maneuver;
        this.maneuverDistance = maneuverDistance;
    }
    public void Update(double delta, Projectile p) {
        if (target == null || maneuver == 0) {
            return;
        }
        var vel = p.velocity;
        var offset = target.position - p.position;
        var velLeft = vel.Rotate(maneuver * delta * Program.TICKS_PER_SECOND);
        var velRight = vel.Rotate(-maneuver * delta * Program.TICKS_PER_SECOND);
        var distLeft = (offset - velLeft).magnitude;
        var distRight = (offset - velRight).magnitude;

        if (maneuverDistance == 0) {
            if (distLeft < distRight) {
                p.velocity = velLeft;
            } else if (distRight < distLeft) {
                p.velocity = velRight;
            }
        } else {
            (var closer, var farther) = distLeft < distRight ? (velLeft, velRight) : (velRight, velLeft);
            if (offset.magnitude > maneuverDistance) {
                p.velocity = closer;
            } else {
                p.velocity = farther;
            }
        }
    }
}
