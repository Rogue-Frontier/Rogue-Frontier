using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class Marker : SpaceObject {
        public string Name { get; private set; }
        public XY Position { get; set; }
        public bool Active { get; set; }
        public ColoredGlyph Tile => null;
        public World World => null;
        public Sovereign Sovereign => null;
        public XY Velocity { get; set; }
        public Marker(string Name, XY Position) {
            this.Name = Name;
            this.Position = Position;
            this.Velocity = new XY();
            this.Active = true;
        }
        public void Update() {}
        public void Damage(SpaceObject source, int hp) {}
        public void Destroy(SpaceObject source = null) {}
    }
}
