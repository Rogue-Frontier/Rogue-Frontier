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
        public XY Position { get; private set; }
        public XY Velocity { get; private set; }
        public bool Active => lifetime > 0;
        public ColoredGlyph Tile { get; private set; }
        public int lifetime;
        public Projectile(World World, ColoredGlyph Tile, XY Position, XY Velocity, int lifetime) {
            this.World = World;
            this.Tile = Tile;
            this.Position = Position;
            this.Velocity = Velocity;
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
                if(i >= trailPoint) {
                    World.AddEffect(new EffectParticle(Position, Tile, 1));
                }
            }

            Position = dest;
        }
    }
}
