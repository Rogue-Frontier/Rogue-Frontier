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

        public XY Position => parent.Position;

        public bool Active => parent.Active;

        public ColoredGlyph Tile => null;

        public void Update() {
            if(parent.docking?.docked == true) {
                return;
            }
            //ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(153, 153, 76), Color.Transparent);
            ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(255, 255, 255, 153), Color.Transparent);
            XY point = parent.Position.Truncate;
            XY inc = XY.Polar(parent.rotationDegrees * Math.PI / 180, 1);
            int length = 20;
            int interval = 2;
            for(int i = 0; i < length / interval; i++) {
                point += inc * interval;
                parent.World.AddEffect(new EffectParticle(point, pointEffect, 1));
            }
        }

        public static void AimLine(World World, XY start, double angle) {
            //ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(153, 153, 76), Color.Transparent);
            ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(255, 255, 0, 153), Color.Transparent);
            XY point = start;
            XY inc = XY.Polar(angle);
            int length = 20;
            int interval = 4;
            for (int i = 0; i < length / interval; i++) {
                point += inc * interval;
                World.AddEffect(new EffectParticle(point, pointEffect, 1));
            }
        }
        public static void Crosshair(World World, XY point) {
            //Color foreground = new Color(153, 153, 153);
            Color foreground = new Color(204, 204, 0);
            Color background = Color.Transparent;
            World.AddEffect(new EffectParticle(point + new XY(1, 0), new ColoredGlyph('-', foreground, background), 1));
            World.AddEffect(new EffectParticle(point + new XY(-1, 0), new ColoredGlyph('-', foreground, background), 1));
            World.AddEffect(new EffectParticle(point + new XY(0, 1), new ColoredGlyph('|', foreground, background), 1));
            World.AddEffect(new EffectParticle(point + new XY(0, -1), new ColoredGlyph('|', foreground, background), 1));
        }
    }
}
