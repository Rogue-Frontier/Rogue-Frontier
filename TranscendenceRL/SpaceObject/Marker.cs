using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class Marker : Entity {
        public string Name { get; private set; }

        public XY Position { get; private set; }

        public bool Active { get; private set; }

        public ColoredGlyph Tile => null;

        public Marker(string Name, XY Position) {
            this.Name = Name;
            this.Position = Position;
            this.Active = true;
        }
        public void Update() {}
    }
}
