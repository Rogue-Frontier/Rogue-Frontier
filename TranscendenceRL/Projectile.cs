using Common;
using Microsoft.Xna.Framework;
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
        public Projectile(SpaceObject Source, World World, ColoredGlyph Tile, XY Position, XY Velocity, int damage, int lifetime) {
            this.Source = Source;
            this.World = World;
            this.Tile = Tile;
            this.Position = Position;
            this.Velocity = Velocity;
            this.damage = damage;
            this.lifetime = lifetime;
        }
        public void Update() {
            if(lifetime > 0) {
                lifetime--;
            }


            var dest = Position + Velocity / 30;
            var inc = Velocity.Normal * 0.5;
            var steps = Velocity.Magnitude * 2 / 30;
            var maxTrailLength = Velocity.Magnitude;
            var trailPoint = steps - maxTrailLength * 2;
            for (int i = 0; i < steps; i++) {
                Position += inc;


                var hit = World.entities[Position].OfType<SpaceObject>().FirstOrDefault(o => !SSpaceObject.IsEqual(o, Source));
                if (hit != null) {
                    lifetime = 0;
                    hit.Damage(Source, damage);

                    var angle = (hit.Position - Position).Angle;
                    World.AddEffect(new EffectParticle(hit.Position + XY.Polar(angle, -1), hit.Velocity, new ColoredGlyph('x', Color.Yellow, Color.Transparent), 5));
                    return;
                }

                if(i >= trailPoint) {
                    World.AddEffect(new EffectParticle(Position, Tile, 1));
                }
            }

            Position = dest;
        }
    }
}
