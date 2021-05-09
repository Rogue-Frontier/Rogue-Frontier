using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranscendenceRL {
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
    public class Projectile : Entity {
        [JsonProperty]
        public World World { get; private set; }
        [JsonProperty] 
        public SpaceObject Source;
        [JsonProperty] 
        public XY position { get; private set; }
        [JsonProperty] 
        public XY Velocity { get; set; }
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
        public bool hitProjectile;

        public bool active => lifetime > 0;

        public Projectile(SpaceObject Source, FragmentDesc desc, XY Position, XY Velocity, Maneuver maneuver = null) {
            this.Source = Source;
            this.World = Source.world;
            this.tile = desc.effect.Glyph;
            this.position = Position;
            this.Velocity = Velocity;
            this.lifetime = desc.lifetime;
            this.desc = desc;
            this.trail = (ITrail)desc.trail ?? new SimpleTrail(desc.effect.Glyph);
            this.maneuver = maneuver;
        }
        public void Update() {
            if(lifetime > 1) {
                lifetime--;
                UpdateMove();
            } else if(lifetime == 1) {
                lifetime--;
                UpdateMove();
                Fragment();
            }


            void UpdateMove() {
                HashSet<Entity> exclude = new HashSet<Entity> { null, Source, this };
                if(Source is PlayerShip ps) {
                    exclude.Add(ps.dock?.Target);
                }
                if(Source is AIShip s) {
                    exclude.UnionWith(s.avoidHit);
                }

                maneuver?.Update(this);

                var dest = position + Velocity / Program.TICKS_PER_SECOND;
                var inc = Velocity.Normal * 0.5;
                var steps = Velocity.Magnitude * 2 / Program.TICKS_PER_SECOND;
                for (int i = 0; i < steps; i++) {
                    position += inc;

                    
                    
                    foreach(var other in World.entities[position].Except(exclude)) {
                        switch(other) {
                            case Segment seg when exclude.Contains(seg.parent):
                                continue;
                            case SpaceObject hit:
                                lifetime = 0;
                                hit.Damage(Source, desc.damageHP);

                                if(desc.disruptor != null) {
                                    if (hit is PlayerShip sh) {
                                        sh.ship.controlHijack = desc.disruptor.GetHijack();
                                    } else if(hit is AIShip ai) {
                                        ai.ship.controlHijack = desc.disruptor.GetHijack();
                                    }
                                }

                                Fragment();
                                var angle = (hit.position - position).Angle;
                                World.AddEffect(new EffectParticle(hit.position + XY.Polar(angle, -1), hit.velocity, new ColoredGlyph(Color.Yellow, Color.Transparent, 'x'), 5));
                                return;
                            case ProjectileBarrier barrier:
                                barrier.Interact(this);
                                break;
                            case Projectile p when hitProjectile:
                                p.lifetime = 0;
                                lifetime = 0;
                                break;
                        }
                    }
                    CollisionDone:
                    World.AddEffect(trail.GetTrail(position));
                }

                position = dest;
            }
        }
        public void Fragment() {
            foreach (var fragment in desc.fragments) {
                double angleInterval = fragment.spreadAngle / fragment.count;
                for (int i = 0; i < fragment.count; i++) {
                    double angle = Velocity.Angle + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
                    Projectile p = new Projectile(Source, fragment, position + XY.Polar(angle, 0.5), Velocity + XY.Polar(angle, fragment.missileSpeed));
                    World.AddEntity(p);
                }
            }
        }
    }

    public class Maneuver {
        SpaceObject target;
        double maneuver;
        public Maneuver(SpaceObject target, double maneuver) {
            this.target = target;
            this.maneuver = maneuver;
        }
        public void Update(Projectile p) {
            var vel = p.Velocity;
            var offset = target.position - p.position;
            var velLeft = vel.Rotate(maneuver);
            var velRight = vel.Rotate(-maneuver);
            var distLeft = (offset - velLeft).Magnitude;
            var distRight = (offset - velRight).Magnitude;
            if (distLeft < distRight) {
                p.Velocity = velLeft;
            } else if(distRight < distLeft) {
                p.Velocity = velRight;
            }
        }

    }

}
