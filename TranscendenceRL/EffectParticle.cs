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
            this.Tile = Tile;
            this.Lifetime = Lifetime;
        }
        public XY Position { get; private set; }

        public bool Active => Lifetime > 0;

        public ColoredGlyph Tile { get; private set; }

        public void Update() {
            Lifetime--;
        }
    }
}
