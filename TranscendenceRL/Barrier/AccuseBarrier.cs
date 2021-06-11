using Common;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    //Surrounds the playership, any projectile that hits this barrier accelerates to extreme speed
    class AccuseBarrier : ProjectileBarrier {
        public PlayerShip owner;
        public XY offset;
        public int lifetime;
        public HashSet<Projectile> cloneList;
        public XY position { get; set; }

        [JsonIgnore]
        public bool active => lifetime > 0;
        [JsonIgnore]
        public ColoredGlyph tile => new ColoredGlyph(Color.Yellow, Color.Black, '*');
        public AccuseBarrier() { }
        public AccuseBarrier(PlayerShip owner, XY offset, int lifetime, HashSet<Projectile> cloneList) {
            this.owner = owner;
            this.offset = offset;
            this.lifetime = lifetime;
            this.cloneList = cloneList;
            UpdatePosition();
        }
        public void Update() {
            lifetime--;
            UpdatePosition();
        }
        public void UpdatePosition() {    
            this.position = owner.position + offset;
        }
        public void Interact(Projectile other) {
            if (other.source == owner) {
                if (cloneList.Contains(other)) {
                    return;
                }

                cloneList.Add(other);

                //other.velocity = other.velocity.WithMagnitude(400);
                var world = owner.world;

                Clone(offset.angleRad + Math.PI / 8);
                Clone(offset.angleRad - Math.PI / 8);
                Clone(offset.angleRad + Math.PI * 2 / 8);
                Clone(offset.angleRad - Math.PI * 2 / 8);
                Clone(offset.angleRad + Math.PI * 3 / 8);
                Clone(offset.angleRad - Math.PI * 3 / 8);

                /*
                for(double angle = offset.angleRad - Math.PI / 2; angle = offset.angleRad + Math.PI / 2; angle++) {

                }
                */


                void Clone(double angle) {
                    var velocity = other.velocity + XY.Polar(angle, other.velocity.magnitude / 2);
                    var p = new Projectile(other.source, other.world, other.desc, other.position, velocity, other.maneuver);
                    cloneList.Add(p);
                    world.AddEntity(p);
                }
                return;
            }
        }
    }
}
