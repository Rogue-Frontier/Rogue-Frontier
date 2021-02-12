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
        public XY Position { get; private set; }
        [JsonProperty] 
        public XY Velocity { get; private set; }
        public bool Active => lifetime > 0;
        [JsonProperty] 
        public ColoredGlyph Tile { get; private set; }
        public ITrail trail;

        public int damage;
        public int lifetime;
        public HashSet<FragmentDesc> fragments;
        public Projectile(SpaceObject Source, World World, ColoredGlyph Tile, ITrail GetTrail, XY Position, XY Velocity, int damage, int lifetime, HashSet<FragmentDesc> fragments) {
            this.Source = Source;
            this.World = World;
            this.Tile = Tile;
            this.trail = GetTrail ?? new SimpleTrail(Tile);
            this.Position = Position;
            this.Velocity = Velocity;
            this.damage = damage;
            this.lifetime = lifetime;
            this.fragments = fragments;
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
                var dest = Position + Velocity / TranscendenceRL.TICKS_PER_SECOND;
                var inc = Velocity.Normal * 0.5;
                var steps = Velocity.Magnitude * 2 / TranscendenceRL.TICKS_PER_SECOND;
                var maxTrailLength = Velocity.Magnitude;
                var trailPoint = steps - maxTrailLength * 2;
                for (int i = 0; i < steps; i++) {
                    Position += inc;


                    var hit = World.entities[Position].OfType<SpaceObject>().FirstOrDefault(o => !SSpaceObject.IsEqual(o, Source));
                    if (hit != null) {
                        lifetime = 0;
                        hit.Damage(Source, damage);
                        Fragment();
                        var angle = (hit.Position - Position).Angle;
                        World.AddEffect(new EffectParticle(hit.Position + XY.Polar(angle, -1), hit.Velocity, new ColoredGlyph(Color.Yellow, Color.Transparent, 'x'), 5));
                        return;
                    }

                    //if (i >= trailPoint) {
                    World.AddEffect(trail.GetTrail(Position));
                    //}

                }

                Position = dest;
            }
        }
        public void Fragment() {
            foreach (var fragment in fragments) {
                double angleInterval = fragment.spreadAngle / fragment.count;
                for (int i = 0; i < fragment.count; i++) {
                    double angle = Velocity.Angle + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
                    var trail = fragment.trail;
                    Projectile p = null;
                    p = new Projectile(Source, World, fragment.effect.Glyph, trail, Position + XY.Polar(angle, 0.5), Velocity + XY.Polar(angle, fragment.missileSpeed), fragment.damageHP, fragment.lifetime, fragment.fragments);
                    World.AddEntity(p);
                }
            }
        }
    }

}
