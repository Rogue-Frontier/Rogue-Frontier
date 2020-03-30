using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class FixedTile : Effect {
        public ColoredGlyph Tile { get; private set; }
        public XY Position { get; private set; }

        public bool Active { get; private set; }
        public FixedTile(ColoredGlyph Tile, XY Position) {
            this.Tile = Tile;
            this.Position = Position;
            this.Active = true;
        }
        public void Update() {
        }
    }
}
