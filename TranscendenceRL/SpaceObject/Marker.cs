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
        public XY Position { get; set; }
        public bool Active { get; set; }
        public ColoredGlyph Tile => null;
        public XY Velocity { get; set; }
        public Marker(string Name, XY Position) {
            this.Name = Name;
            this.Position = Position;
            this.Velocity = new XY();
            this.Active = true;
        }
        public void Update() {}
    }

    class TargetingMarker : SpaceObject {
        PlayerShip Owner;
        List<SpaceObject> Nearby;
        public string Name { get; private set; }
        public XY Position { get; set; }
        public bool Active { get; set; }
        public ColoredGlyph Tile => null;
        public XY Velocity { get; set; }

        public World World => throw new NotImplementedException();

        public Sovereign Sovereign => throw new NotImplementedException();

        public TargetingMarker(PlayerShip Owner, string Name, XY Position) {
            this.Owner = Owner;
            this.Nearby = new List<SpaceObject>();
            this.Name = Name;
            this.Position = Position;
            this.Velocity = new XY();
            this.Active = true;
        }
        public void Update() {
            Nearby = Owner.World.entities.all.OfType<SpaceObject>().Except(new SpaceObject[] { Owner }).OrderBy(e => (e.Position - Position).Magnitude).ToList();
        }

        public void Damage(SpaceObject source, int hp) {
        }

        public void Destroy(SpaceObject source = null) {
        }
    }
}
