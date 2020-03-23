using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class Projectile : Entity {
        public XY position { get; private set; }
        public bool Active => lifetime > 0;
        public ColoredGlyph tile { get; private set; }
        public uint lifetime;
        public Projectile(ColoredGlyph tile, XY position, uint lifetime) {
            this.tile = tile;
            this.position = position;
            this.lifetime = lifetime;
        }
        public void Update() {
            if(lifetime > 0) {
                lifetime--;
            }
        }
    }
}
