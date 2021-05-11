using Common;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    //Surrounds the playership, reflects their projectiles back so that they bounce around
    class EchoBarrier : ProjectileBarrier {
        public PlayerShip owner;
        public XY offset;
        public int lifetime;
        public XY position { get; set; }

        public bool active => lifetime > 0;
        public ColoredGlyph tile => new ColoredGlyph(Color.Yellow, Color.Black, '*');
        public EchoBarrier(PlayerShip owner, XY offset, int lifetime) {
            this.owner = owner;
            this.offset = offset;
            this.lifetime = lifetime;
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
            if(other.Source == owner) {
                return;
            }
            other.velocity = new XY() -other.velocity;
        }
    }
}
