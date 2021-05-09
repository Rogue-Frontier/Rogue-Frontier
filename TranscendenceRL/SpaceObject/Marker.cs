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
        public XY position { get; set; }
        public bool active { get; set; }
        public ColoredGlyph tile => null;
        public XY Velocity { get; set; }
        public Marker(string Name, XY Position) {
            this.Name = Name;
            this.position = Position;
            this.Velocity = new XY();
            this.active = true;
        }
        public void Update() {}
    }

    class TargetingMarker : SpaceObject {
        PlayerShip Owner;
        List<SpaceObject> Nearby;
        public string name { get; private set; }
        public XY position { get; set; }
        public bool active { get; set; }
        public ColoredGlyph tile => null;
        public XY velocity { get; set; }

        public World world => throw new NotImplementedException();

        public Sovereign sovereign => throw new NotImplementedException();

        public TargetingMarker(PlayerShip Owner, string Name, XY Position) {
            this.Owner = Owner;
            this.Nearby = new List<SpaceObject>();
            this.name = Name;
            this.position = Position;
            this.velocity = new XY();
            this.active = true;
        }
        public void Update() {
            Nearby = Owner.world.entities.all.OfType<SpaceObject>().Except(new SpaceObject[] { Owner }).OrderBy(e => (e.position - position).Magnitude).ToList();
        }

        public void Damage(SpaceObject source, int hp) {
        }

        public void Destroy(SpaceObject source = null) {
        }
    }
}
