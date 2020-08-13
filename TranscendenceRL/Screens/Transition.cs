
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using Common;
using SadConsole.Input;

namespace TranscendenceRL {
    public class TitleSlideOpening : Console {
        public Console next;
        int x = 0;
        double time = 0;
        double interval;
        bool fast;
        public TitleSlideOpening(Console next) : base(next.Width, next.Height) {
            x = next.Width;
            this.next = next;
            interval = 4f / Width;

            //Draw one frame now so that we don't cut out for one frame
            next.Update(new TimeSpan());
            Render(new TimeSpan());
        }
        public override void Update(TimeSpan delta) {
            next.Update(delta);
            base.Update(delta);
            if (fast) {
                x -= (int)(Width * delta.TotalSeconds);
            }
            time += delta.TotalSeconds;
            while(time > interval) {
                time -= interval;
                if (x > -16) {
                    x--;
                } else {
                    SadConsole.Game.Instance.Screen = next;
                    next.IsFocused = true;
                    return;
                }
            }
        }
        public override void Render(TimeSpan delta) {
            next.Render(delta);
            base.Render(delta);
            this.Clear();
            var blank = new ColoredGlyph(Color.Black, Color.Black);
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < this.x; x++) {
                    this.SetCellAppearance(x, y, blank);
                }
                for(int x = Math.Max(0, this.x); x < Math.Min(Width, this.x + 16); x++) {
                    
                    var glyph = next.GetGlyph(x, y);
                    var value = 255 - 255 / 16 * (x - this.x);

                    var fore = next.GetForeground(x, y);
                    fore = fore.Premultiply().Blend(Color.Black.WithValues(alpha: value));

                    var back = next.GetBackground(x, y);
                    back = back.Premultiply().Blend(Color.Black.WithValues(alpha: value));

                    this.SetCellAppearance(x, y, new ColoredGlyph(fore, back, glyph));
                }
            }
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if(keyboard.IsKeyPressed(Keys.Enter)) {
                fast = true;
            }
            return base.ProcessKeyboard(keyboard);
        }
    }
    public class TitleSlideOut : Console {
        public Console prev, next;
        int x = 0;
        double time = 0;
        double interval;
        bool fast;
        public TitleSlideOut(Console prev, Console next) : base(next.Width, next.Height) {
            x = next.Width;
            this.prev = prev;
            this.next = next;
            interval = 4f / Width;

            //Draw one frame now so that we don't cut out for one frame
            next.Update(new TimeSpan());
            Render(new TimeSpan());
        }
        public override void Update(TimeSpan delta) {
            next.Update(delta);
            base.Update(delta);
            if(x > -16) {
                x -= (int)(Width * delta.TotalSeconds);
            } else {
                SadConsole.Game.Instance.Screen = next;
                next.IsFocused = true;
                return;
            }
        }
        public override void Render(TimeSpan delta) {
            next.Render(delta);
            base.Render(delta);
            this.Clear();
            var blank = new ColoredGlyph(Color.Black, Color.Black);
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < this.x; x++) {
                    this.SetCellAppearance(x, y, prev.GetCellAppearance(x,y));
                }
                for (int x = Math.Max(0, this.x); x < Math.Min(Width, this.x + 16); x++) {

                    var glyph = next.GetGlyph(x, y);
                    var value = 255 - 255 / 16 * (x - this.x);

                    var fore = next.GetForeground(x, y);
                    fore = fore.Premultiply().Blend(prev.GetForeground(x, y).Premultiply().WithValues(alpha: value));

                    var back = next.GetBackground(x, y);
                    back = back.Premultiply().Blend(prev.GetBackground(x,y).Premultiply().WithValues(alpha: value));

                    this.SetCellAppearance(x, y, new ColoredGlyph(fore, back, glyph));
                }
            }
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if (keyboard.IsKeyPressed(Keys.Enter)) {
                fast = true;
            }
            return base.ProcessKeyboard(keyboard);
        }
    }
    public class TitleSlideIn : Console {
        public Console prev;
        public Console next;
        int x = -16;
        public TitleSlideIn(Console prev, Console next) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.next = next;
            //Draw one frame now so that we don't cut out for one frame
            Render(new TimeSpan());
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if(keyboard.IsKeyPressed(Keys.Enter)) {
                Next();
            }
            return base.ProcessKeyboard(keyboard);
        }
        public override void Update(TimeSpan delta) {
            prev.Update(delta);
            base.Update(delta);
            if (x < Width) {
                x += (int)(Width * delta.TotalSeconds);
            } else {
                Next();
            }
        }
        public void Next() {
            SadConsole.Game.Instance.Screen = next;
            next.IsFocused = true;
        }
        public override void Render(TimeSpan delta) {
            next.Render(delta);
            prev.Render(delta);
            base.Render(delta);
            var blank = new ColoredGlyph(Color.Black, Color.Black);
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < this.x; x++) {
                    //this.SetCellAppearance(x, y, blank);
                    this.SetCellAppearance(x, y, next.GetCellAppearance(x, y));
                }
                //Fading opacity edge
                for (int x = Math.Max(0, this.x); x < Math.Min(Width, this.x + 16); x++) {

                    var glyph = prev.GetGlyph(x, y);
                    var value = 255 - 255 / 16 * (x - this.x);

                    var fore = prev.GetForeground(x, y);
                    fore = fore.Premultiply().Blend(next.GetForeground(x,y).Premultiply().WithValues(alpha: value));

                    var back = prev.GetBackground(x, y);
                    back = back.Premultiply().Blend(next.GetBackground(x, y).Premultiply().WithValues(alpha: value));

                    this.SetCellAppearance(x, y, new ColoredGlyph(fore, back, glyph));
                }
            }
        }
    }
    public class FadeIn : Console {
        Console next;
        float alpha;
        public FadeIn(Console next) : base(next.Width, next.Height) {
            this.next = next;
            DefaultBackground = Color.Transparent;
            Render(new TimeSpan());
        }
        public override void Update(TimeSpan delta) {
            base.Update(delta);
            if (alpha < 1) {
                alpha += (float)(delta.TotalSeconds / 4);
            } else {
                SadConsole.Game.Instance.Screen = next;
                next.IsFocused = true;
            }
        }
        public override void Render(TimeSpan delta) {
            this.Clear();
            var g = new ColoredGlyph(Color.Black, new Color(0, 0, 0, 1 - alpha));
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    this.SetCellAppearance(x, y, g);
                }
            }
            next.Render(delta);
            base.Render(delta);
        }
    }

    public class Pause : Console {
        Console next;
        double time;
        public Pause(double time, Console next) : base(next.Width, next.Height) {
            this.time = 5;
            this.next = next;
            Render(new TimeSpan());
        }
        public Pause(Console next) : base(next.Width, next.Height) {
            this.next = next;
            time = 5;
            Render(new TimeSpan());
        }
        public override void Update(TimeSpan delta) {
            base.Update(delta);
            if (time > 0) {
                time -= delta.TotalSeconds;
            } else {
                SadConsole.Game.Instance.Screen = next;
                next.IsFocused = true;
            }
        }
        public override void Render(TimeSpan delta) {
            next.Render(delta);
        }
    }
}
