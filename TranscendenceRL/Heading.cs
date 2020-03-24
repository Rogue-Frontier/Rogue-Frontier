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
        IShip parent;
        public Heading(IShip parent) {
            this.parent = parent;
        }

        public XY position => parent.position;

        public bool Active => parent.Active;

        public ColoredGlyph tile => null;

        public void Update() {

            //ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(153, 153, 76), Color.Transparent);
            ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(153, 153, 153), Color.Transparent);
            XY point = parent.position.Truncate;
            XY inc = XY.Polar(parent.rotationDegrees * Math.PI / 180, 1);
            int length = 12;
            for(int i = 0; i < length; i++) {
                point += inc * 2;
                parent.world.AddEffect(new EffectParticle(point, pointEffect, 1));
            }
        }
    }
}
