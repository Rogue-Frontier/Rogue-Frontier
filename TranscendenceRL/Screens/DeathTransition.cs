using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using Console = SadConsole.Console;
using Common;
using SadConsole.Input;

namespace TranscendenceRL {

    public class DeathPause : Console {
        PlayerMain prev;
        DeathTransition next;

        public double time;
        public bool done;
        Viewport view;
        public DeathPause(PlayerMain prev, DeathTransition next) : base(prev.Width,prev.Height) {
            this.prev = prev;
            this.next = next;
            view = new Viewport(prev, prev.camera, new Dictionary<(int, int), ColoredGlyph>(prev.tiles));
        }
        public override void Update(TimeSpan delta) {
            time += delta.TotalSeconds / 4;
            if(time < 2 && !done) {
                return;
            }
            SadConsole.Game.Instance.Screen = next;
            next.IsFocused = true;

            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            view.Render(delta);
            base.Render(delta);
        }
    }
    public class DeathTransition : Console {
        Console prev, next;
        public class Particle {
            public int x, destY;
            public double y, delay;
        }
        HashSet<Particle> particles;
        double time;
        public DeathTransition(Console prev, Console next) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.next = next;
            particles = new HashSet<Particle>();
            for(int y = 0; y < Height/2; y++) {
                for(int x = 0; x < Width; x++) {
                    particles.Add(new Particle() {
                        x = x,
                        y = -1,
                        destY = y,
                        delay = (1 + Math.Sin(Math.Sin(x) + Math.Sin(y))) * 3 / 2
                    });
                }
            }
            for (int y = Height/2; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    particles.Add(new Particle() {
                        x = x,
                        y = Height,
                        destY = y,
                        delay = (1 + Math.Sin(Math.Sin(x) + Math.Sin(y))) * 3 / 2
                    });
                }
            }
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if (keyboard.IsKeyPressed(Keys.Enter)) {
                Transition();
            }
            return base.ProcessKeyboard(keyboard);
        }
        public void Transition () {
            SadConsole.Game.Instance.Screen = next;
            next.IsFocused = true;
        }
        public override void Update(TimeSpan delta) {
            time += delta.TotalSeconds / 2;
            prev.Update(delta);
            if (time < 4) {
                return;
            } else if(time < 9) {
                foreach (var p in particles) {
                    if(p.delay > 0) {
                        p.delay -= delta.TotalSeconds/2;
                    } else {
                        var offset = (p.destY - p.y);
                        p.y += Math.MinMagnitude(offset, Math.MaxMagnitude(Math.Sign(offset), offset * delta.TotalSeconds/2));
                    }
                }
            } else {
                Transition();
            }
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            prev.Render(delta);
            base.Render(delta);
            this.Clear();
            foreach(var p in particles) {
                this.SetCellAppearance(p.x, (int)p.y, new ColoredGlyph(Color.Black, Color.Black, ' '));
            }
        }
    }
}
