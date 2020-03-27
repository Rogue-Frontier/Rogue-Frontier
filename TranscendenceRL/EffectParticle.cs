using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class EffectParticle : Effect {
        private int Lifetime;
        public EffectParticle(XY Position, ColoredGlyph Tile, int Lifetime) {
            this.Position = Position;
            this.Velocity = new XY();
            this.Tile = Tile;
            this.Lifetime = Lifetime;
        }
        public EffectParticle(XY Position, XY Velocity, ColoredGlyph Tile, int Lifetime) {
            this.Position = Position;
            this.Velocity = Velocity;
            this.Tile = Tile;
            this.Lifetime = Lifetime;
        }
        public XY Position { get; private set; }
        public XY Velocity { get; private set; }

        public bool Active => Lifetime > 0;

        public ColoredGlyph Tile { get; private set; }

        public void Update() {
            Position += Velocity / 30;
            Lifetime--;
        }
    }
}
