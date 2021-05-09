using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class Heading : Effect {
        public IShip parent;
        public Heading(IShip parent) {
            this.parent = parent;
        }

        public XY position => parent.position;

        public bool active => parent.active;

        public ColoredGlyph tile => null;

        public void Update() {
            if(parent.dock?.docked == true) {
                return;
            }
            //ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(153, 153, 76), Color.Transparent);
            //ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(153, 153, 153), Color.Transparent);

            //Idea: Highlight a segment of the aimline based on the firetime left on the weapon
            XY point = parent.position;
            int step = 2;
            XY inc = XY.Polar(parent.rotationDegrees * Math.PI / 180, 1)  * step;
            int length = 20;
            for(int i = 0; i < length; i += step) {
                point += inc;
                var value = 153 - Math.Max(1, i / 2) * 153/length;
                ColoredGlyph pointEffect = new ColoredGlyph(new Color(value, value, value), Color.Transparent, '.');
                parent.world.AddEffect(new EffectParticle(point, pointEffect, 1));
            }
        }
        public static void AimLine(World World, XY start, double angle) {
            //ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(153, 153, 76), Color.Transparent);
            ColoredGlyph pointEffect = new ColoredGlyph(new Color(255, 255, 0, 204), Color.Transparent, '.');
            XY point = start;
            XY inc = XY.Polar(angle);
            int length = 20;
            int interval = 4;
            for (int i = 0; i < length / interval; i++) {
                point += inc * interval;
                World.AddEffect(new EffectParticle(point, pointEffect, 1));
            }
        }
        public static void AimLine(SpaceObject owner, double angle, Weapon w) {
            //Idea: Highlight a segment of the aimline based on the firetime left on the weapon

            var start = owner.position;
            var World = owner.world;

            //ColoredGlyph pointEffect = new ColoredGlyph('.', new Color(153, 153, 76), Color.Transparent);
            //ColoredGlyph dark = new ColoredGlyph(new Color(255, 255, 0, 102), Color.Transparent, '.');
            ColoredGlyph bright = new ColoredGlyph(new Color(255, 255, 0, 204), Color.Transparent, '.');
            XY point = start;
            XY inc = XY.Polar(angle);
            //var length = w.target == null ? 20 : (w.target.Position - owner.Position).Magnitude;
            var length = 20;
            int interval = 4;

            var points = length / interval;
            //var highlights = points * (1 - w.fireTime / w.desc.fireCooldown);

            for (int i = 0; i < points; i++) {
                point += inc * interval;
                //World.AddEffect(new EffectParticle(point, i < highlights ? bright : dark, 1));
                World.AddEffect(new EffectParticle(point, bright, 1));
            }
        }
        public static void Crosshair(World World, XY point) {
            //Color foreground = new Color(153, 153, 153);
            Color foreground = new Color(204, 204, 0);
            Color background = Color.Transparent;
            World.AddEffect(new EffectParticle(point + new XY(1, 0), new ColoredGlyph(foreground, background, '-'), 1));
            World.AddEffect(new EffectParticle(point + new XY(-1, 0), new ColoredGlyph(foreground, background, '-'), 1));
            World.AddEffect(new EffectParticle(point + new XY(0, 1), new ColoredGlyph(foreground, background, '|'), 1));
            World.AddEffect(new EffectParticle(point + new XY(0, -1), new ColoredGlyph(foreground, background, '|'), 1));
        }
    }
}
