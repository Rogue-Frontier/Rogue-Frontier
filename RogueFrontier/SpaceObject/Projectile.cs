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
    [JsonProperty] public int id { get; set; }
    [JsonProperty] public System world { get; set; }
    [JsonProperty] public ActiveObject source;
    [JsonProperty] public XY position { get; set; }
    [JsonProperty] public XY velocity { get; set; }
    [JsonProperty] public double direction;
    [JsonProperty] public int lifetime { get; set; }
    [JsonProperty] public ColoredGlyph tile { get; private set; }
    [JsonProperty] public ITrail trail;
    [JsonProperty] public FragmentDesc fragment;
    [JsonProperty] public Maneuver maneuver;
    [JsonProperty] public int damageHP;
    [JsonProperty] public int ricochet = 0;
    [JsonProperty] public bool hitHull;

    //List of projectiles that were created from the same fragment
    public List<Projectile> siblings = new();

    public delegate void OnHitActive(Projectile p, ActiveObject other);
    public FuncSet<IContainer<OnHitActive>> onHitActive=new();

    [JsonIgnore]   public bool active => lifetime > 0;
    
    public Projectile() { }
    public Projectile(ActiveObject source, FragmentDesc fragment, XY position, XY velocity, double? direction = null, Maneuver maneuver = null) {
        this.id = source.world.nextId++;
        this.source = source;
        this.world = source.world;
        this.tile = fragment.effect.Original;
        this.position = position;
        this.velocity = velocity;
        this.direction = direction ?? velocity.angleRad;
        this.lifetime = fragment.lifetime;
        this.fragment = fragment;
        this.trail = (ITrail)fragment.trail ?? new SimpleTrail(fragment.effect.Original);
        this.maneuver = maneuver;
        this.damageHP = fragment.damageHP.Roll();
        this.ricochet = fragment.ricochet;
    }
    public void Update() {
        if (lifetime > 1) {
            lifetime--;
            UpdateMove();
            foreach (var f in fragment.fragments) {
                if (f.fragmentInterval > 0 && lifetime % f.fragmentInterval == 0) {
                    Fragment(f);
                }
            }
        } else if (lifetime == 1) {
            lifetime--;
            UpdateMove();
            Fragment();
        }
        void UpdateMove() {
            HashSet<Entity> exclude = new HashSet<Entity> { null, source, this };
            exclude.UnionWith(source switch {
                PlayerShip ps => ps.avoidHit,
                AIShip ai => ai.avoidHit,
                Station st => st.guards,
                _ => new HashSet<Entity>()
            });
            maneuver?.Update(this);

            var dest = position + velocity / Program.TICKS_PER_SECOND;
            var inc = velocity.normal * 0.5;
            var steps = velocity.magnitude * 2 / Program.TICKS_PER_SECOND;
            for (int i = 0; i < steps; i++) {
                position += inc;

                bool destroyed = false;
                bool stop = false;

                foreach (var other in world.entities[position].Select(e => e is ISegment s ? s.parent : e).Distinct().Except(exclude)) {

                    switch (other) {
                        case ActiveObject hit when !destroyed:
                            hit.Damage(this);
                            var angle = (hit.position - position).angleRad;
                            world.AddEffect(new EffectParticle(hit.position + XY.Polar(angle, -1), hit.velocity, new ColoredGlyph(Color.Yellow, Color.Transparent, 'x'), 10));

                            if(ricochet > 0) {
                                ricochet--;
                                velocity = -velocity;
                                //velocity += (hit.velocity - velocity) / 2;
                                //stop = true;
                            } else {
                                onHitActive.ForEach(f => f(this, hit));
                                Fragment();
                                if (fragment.hook) {
                                    world.AddEntity(new Hook(hit, source));
                                }


                                lifetime = 0;
                                destroyed = true;


                                //new FlashDesc() { intensity = 5000 }.Create(world, position);
                            }
                            break;
                        case Projectile p when !exclude.Contains(p.source) && fragment.hitProjectile && !destroyed:
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
                        case ProjectileBarrier barrier when fragment.hitBarrier:
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
    public void Fragment() {
        if (fragment.fragments == null) return;
        foreach (var f in fragment.fragments) {
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
        for (int i = 0; i < fragment.count; i++) {
            double angle = fragmentAngle + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
            var p = new Projectile(source,
                fragment,
                position + XY.Polar(angle, 0.5),
                velocity + XY.Polar(angle, fragment.missileSpeed),
                angle,
                fragment.GetManeuver(maneuver?.target)
                );
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
    public void Update(Projectile p) {
        if (target == null || maneuver == 0) {
            return;
        }
        var vel = p.velocity;
        var offset = target.position - p.position;
        var velLeft = vel.Rotate(maneuver);
        var velRight = vel.Rotate(-maneuver);
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
