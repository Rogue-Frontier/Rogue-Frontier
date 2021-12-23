using Common;
using SadRogue.Primitives;
using SadConsole;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using static RogueFrontier.Weapon;

namespace RogueFrontier;

public interface ITrail {
    Effect GetTrail(XY Position);
}
public class SimpleTrail : ITrail {
    public StaticTile Tile;
    public SimpleTrail(StaticTile Tile) {
        this.Tile = Tile;
    }
    public Effect GetTrail(XY Position) => new EffectParticle(Position, Tile, 3);
}
public record DamageDesc(int damageHP, int damageType) { }
public class Projectile : MovingObject {
    [JsonProperty]
    public int id { get; set; }
    [JsonProperty]
    public System world { get; set; }
    [JsonProperty]
    public SpaceObject source;
    [JsonProperty]
    public XY position { get; set; }
    [JsonProperty]
    public XY velocity { get; set; }
    [JsonProperty]
    public int lifetime { get; set; }
    [JsonProperty]
    public ColoredGlyph tile { get; private set; }
    [JsonProperty]
    public ITrail trail;
    [JsonProperty]
    public FragmentDesc desc;
    [JsonProperty]
    public Maneuver maneuver;

    [JsonProperty]
    public DamageDesc damage;
    [JsonProperty]
    public bool hitProjectile;

    [JsonIgnore]
    public bool active => lifetime > 0;
    public Projectile() { }
    public Projectile(SpaceObject source, FragmentDesc desc, XY position, XY velocity, Maneuver maneuver = null) {
        this.id = source.world.nextId++;
        this.source = source;
        this.world = source.world;
        this.tile = desc.effect.Original;
        this.position = position;
        this.velocity = velocity;
        this.lifetime = desc.lifetime;
        this.desc = desc;
        this.trail = (ITrail)desc.trail ?? new SimpleTrail(desc.effect.Original);
        this.maneuver = maneuver;
        this.hitProjectile = false;
    }
    public Projectile(SpaceObject source, Weapon weapon, XY position, XY velocity) {
        var world = source.world;
        var f = weapon.GetFragmentDesc();

        this.id = world.nextId++;
        this.source = source;
        this.world = world;
        this.tile = f.effect.Original;
        this.position = position;
        this.velocity = velocity;
        this.lifetime = f.lifetime;
        this.desc = f;
        this.trail = (ITrail)f.trail ?? new SimpleTrail(f.effect.Original);
        this.maneuver = new Maneuver(weapon.aiming?.target, f.maneuver, f.maneuverRadius);
        this.hitProjectile = weapon.desc.targetProjectile;
    }

    public void Update() {
        if (lifetime > 1) {
            lifetime--;
            UpdateMove();
            foreach (var f in desc.fragments) {
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
            if (source is PlayerShip ps) exclude.UnionWith(ps.avoidHit);
            else if (source is AIShip a) exclude.UnionWith(a.avoidHit);

            maneuver?.Update(this);

            var dest = position + velocity / Program.TICKS_PER_SECOND;
            var inc = velocity.normal * 0.5;
            var steps = velocity.magnitude * 2 / Program.TICKS_PER_SECOND;
            for (int i = 0; i < steps; i++) {
                position += inc;

                bool destroyed = false;
                bool stop = false;

                foreach (var other in world.entities[position].Except(exclude)) {
                    switch (other) {
                        //Skip excluded segments
                        case Segment seg when exclude.Contains(seg.parent):
                            continue;
                        case SpaceObject hit when !destroyed:
                            lifetime = 0;
                            hit.Damage(source, desc.damageHP.Roll());
                            var angle = (hit.position - position).angleRad;
                            world.AddEffect(new EffectParticle(hit.position + XY.Polar(angle, -1), hit.velocity, new ColoredGlyph(Color.Yellow, Color.Transparent, 'x'), 10));

                            if (desc.disruptor != null) {
                                if (hit is PlayerShip sh) {
                                    sh.ship.controlHijack = desc.disruptor.GetHijack();
                                } else if (hit is AIShip ai) {
                                    ai.ship.controlHijack = desc.disruptor.GetHijack();
                                }
                            }
                            Fragment();
                            destroyed = true;
                            break;
                        case Projectile p when hitProjectile && !destroyed:
                            p.lifetime = 0;
                            lifetime = 0;
                            destroyed = true;
                            break;
                        case ProjectileBarrier barrier:
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
                world.AddEffect(trail.GetTrail(position));
            }
            position = dest;
        }
    }
    public void Fragment() {
        foreach (var f in desc.fragments) {
            Fragment(f);
        }
    }
    public void Fragment(FragmentDesc fragment) {
        if (fragment.targetLocked != null
            && fragment.targetLocked != (maneuver.target != null)) {
            return;
        }


        double angleInterval = fragment.spreadAngle / fragment.count;
        double centerAngle;

        if (fragment.omnidirectional
            && maneuver?.target?.active == true
            && Aiming.CalcFireAngle(this, maneuver.target, fragment.missileSpeed, out var result)) {
            centerAngle = result;
        } else {
            centerAngle = velocity.angleRad;
        }
        for (int i = 0; i < fragment.count; i++) {
            double angle = centerAngle + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
            Projectile p = new Projectile(source, fragment, position + XY.Polar(angle, 0.5), velocity + XY.Polar(angle, fragment.missileSpeed));
            world.AddEntity(p);
        }
    }
}

public class Maneuver {
    public SpaceObject target;
    public double maneuver;
    public double maneuverDistance;
    public Maneuver(SpaceObject target, double maneuver, double maneuverDistance) {
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
