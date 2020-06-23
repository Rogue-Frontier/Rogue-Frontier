using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
    class Segment : Entity {
        public Entity parent { get; }

        public Island World => parent.World;

        public XYZ Velocity { get => parent.Velocity; set => parent.Velocity = value; }

        public ColoredString Name => parent.Name;

        public XYZ Position { get => parent.Position + offset; set => parent.Position = value - offset; }

        public ColoredGlyph SymbolCenter => parent.SymbolCenter;

        public bool Active => parent.Active;

        private XYZ offset;
        public Segment(Entity parent, XYZ offset) {
            this.parent = parent;
            this.offset = offset;
        }

        public void OnRemoved() {
        }

        public void UpdateRealtime(TimeSpan delta) {
        }

        public void UpdateStep() {
        }
    }
}
