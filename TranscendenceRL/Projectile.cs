using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class Projectile : Entity {
        public World World { get; private set; }
        SpaceObject Source;
        public XY Position { get; private set; }
        public XY Velocity { get; private set; }
        public bool Active => lifetime > 0;
        public ColoredGlyph Tile { get; private set; }
        public int damage;
        public int lifetime;
        public HashSet<FragmentDesc> fragments;
        public Projectile(SpaceObject Source, World World, ColoredGlyph Tile, XY Position, XY Velocity, int damage, int lifetime, HashSet<FragmentDesc> fragments) {
            this.Source = Source;
            this.World = World;
            this.Tile = Tile;
            this.Position = Position;
            this.Velocity = Velocity;
            this.damage = damage;
            this.lifetime = lifetime;
            this.fragments = fragments;
        }
        public void Update() {
            if(lifetime > 1) {
                lifetime--;

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

                    if (i >= trailPoint) {
                        World.AddEffect(new EffectParticle(Position, Tile, 1));
                    }

                }

                Position = dest;
            } else if(lifetime == 1) {
                Fragment();
                lifetime--;
            }


            
        }
        public void Fragment() {
            foreach (var fragment in fragments) {
                double angleInterval = fragment.spreadAngle / fragment.count;
                for (int i = 0; i < fragment.count; i++) {
                    double angle = Velocity.Angle + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
                    World.AddEntity(new Projectile(Source, World, fragment.effect.Glyph, Position + XY.Polar(angle, 0.5), Velocity + XY.Polar(angle, fragment.missileSpeed), fragment.damageHP, fragment.lifetime, fragment.fragments));
                }
            }
        }
    }

}
