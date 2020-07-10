using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using Console = SadConsole.Console;
using Common;

namespace TranscendenceRL {
    class DeathTransition : Console {
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
                        y = 0,
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
        public override void Update(TimeSpan delta) {
            prev.Update(delta);
            time += delta.TotalSeconds / 2;
            if(time < 4) {
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
                SadConsole.Game.Instance.Screen = next;
                next.IsFocused = true;
            }
            base.Update(delta);
        }
        public override void Draw(TimeSpan delta) {
            prev.Draw(delta);
            base.Draw(delta);
            this.Clear();
            for(int y = 0; y < Height; y++) {
                for(int x = 0; x < Width; x++) {
                    var cell = prev.GetCellAppearance(x, y);
                    var baseValue = (Math.Clamp(time - 2, 0, 4) * 51);

                    var value = (int)(baseValue + Math.Clamp(Math.Max(time - 2, 0) / 2, 0, 3) * 51 * Math.Sin(time + Math.Sin(x) + Math.Sin(y)));
                    var shift = Math.Clamp(time - 4, 0, 4) / 4;
                    var back = cell.Background.Premultiply().Blend(new Color(255, (int)(255 - shift * 255), (int)(255 - shift*255), value));
                    var front = cell.Foreground.Premultiply().Blend(new Color(255, (int)(255 - shift * 255), (int)(255 - shift * 255), value));
                    this.SetCellAppearance(x, y, new ColoredGlyph(front, back, cell.Glyph));
                }
            }
            foreach(var p in particles) {
                this.SetCellAppearance(p.x, (int)p.y, new ColoredGlyph(Color.Black, Color.Black, ' '));
            }
        }
    }
}
