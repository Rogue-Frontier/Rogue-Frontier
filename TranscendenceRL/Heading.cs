using Common;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class Heading : Effect {
        Ship parent;
        int ticks;
        public Heading(Ship parent) {
            this.parent = parent;
            ticks = 0;
        }

        public XY position => parent.position;

        public bool Active => parent.Active;

        public ColoredGlyph tile => null;

        public void Update() {
            ticks++;
            if(ticks%6 < 3) {
                return;
            }

            ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(153, 153, 76), Color.Transparent);
            XY point = parent.position;
            XY inc = XY.Polar(parent.rotationDegrees * Math.PI / 180, 1);
            int length = 12;
            for(int i = 0; i < length; i++) {
                if(i%4 == 0) {
                    point += inc * 4;
                } else {
                    point += inc;
                }
                parent.world.AddEffect(new EffectParticle(point, pointEffect, 1));
            }
        }
    }
}
