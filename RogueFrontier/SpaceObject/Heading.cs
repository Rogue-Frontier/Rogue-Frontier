using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier {
    class Heading : Effect {
        public IShip parent;
        public Heading(IShip parent) {
            this.parent = parent;
        }

        public XY position => parent.position;

        public bool active => parent.active;

        public ColoredGlyph tile => null;
        int ticks;
        public EffectParticle[] particles;
        public void Update() {
            if(parent.dock?.docked == true) {
                ticks = 0;
                return;
            }
            const int interval = 30;
            XY start = parent.position;
            int step = 2;
            XY inc = XY.Polar(parent.rotationDeg * Math.PI / 180, 1) * step;
            if (ticks == 0) {

                //Idea: Highlight a segment of the aimline based on the firetime left on the weapon
                int length = 20;
                int count = length / step;
                particles = new EffectParticle[count];
                for (int i = 0; i < count; i++) {
                    var point = start + inc * i;
                    var value = 153 - Math.Max(1, i) * 153 / length;
                    var cg = new ColoredGlyph(new Color(value, value, value), Color.Transparent, '.');
                    var particle = new EffectParticle(point, cg, interval + 1);
                    particles[i] = particle;
                    parent.world.AddEffect(particle);
                }
            } else {
                for (int i = 0; i < particles.Length; i++) {
                    var p = particles[i];
                    p.position = start + inc * i;
                    p.lifetime = interval + 1 + (interval * (i - particles.Length)) / particles.Length;
                }
            }
            ticks++;
            
        }
        public static void AimLine(System World, XY start, double angle) {
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
        public static void Crosshair(System World, XY point) {
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
