using Common;
using Newtonsoft.Json;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class Marker : Entity {
        public int Id => -1;
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
        [JsonIgnore]
        public ColoredGlyph tile => null;
        [JsonIgnore]
        public System world => Owner.world;
        [JsonIgnore]
        public Sovereign sovereign => Owner.sovereign;
        [JsonIgnore]
        public int Id => -1;

        public PlayerShip Owner;
        //public List<SpaceObject> Nearby;
        public string name { get; set; }
        public XY position { get; set; }
        public XY velocity { get; set; }
        public bool active { get; set; }

        public TargetingMarker(PlayerShip Owner, string Name, XY Position) {
            this.Owner = Owner;
            //this.Nearby = new List<SpaceObject>();
            this.name = Name;
            this.position = Position;
            this.velocity = new XY();
            this.active = true;
        }
        public void Update() {
            //Nearby = Owner.world.entities.all.OfType<SpaceObject>().Except(new SpaceObject[] { Owner }).OrderBy(e => (e.position - position).magnitude).ToList();
        }
        public void Damage(SpaceObject source, int hp) { }
        public void Destroy(SpaceObject source = null) { }
    }
}
