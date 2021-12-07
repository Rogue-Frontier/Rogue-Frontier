using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier {
    class FixedTile : Effect {
        public ColoredGlyph tile { get; private set; }
        public XY position { get; private set; }
        public bool active { get; private set; }
        public FixedTile(ColoredGlyph Tile, XY Position) {
            this.tile = Tile;
            this.position = Position;
            this.active = true;
        }
        public void Update() {
        }
    }
}
