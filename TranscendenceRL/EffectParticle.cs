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
            this.position = Position;
            this.tile = Tile;
            this.Lifetime = Lifetime;
        }
        public XY position { get; private set; }

        public bool Active => Lifetime > 0;

        public ColoredGlyph tile { get; private set; }

        public void Update() {
            Lifetime--;
        }
    }
}
