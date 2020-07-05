
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using Common;
using SadConsole.Input;

namespace TranscendenceRL {
    //To do: Press Enter to speed up slide
    class TitleSlide : Console {
        public Console next;
        int x = 0;
        double time = 0;
        double interval;
        bool fast;
        public TitleSlide(int width, int height, Console next) : base(width, height) {
            x = width;
            this.next = next;
            interval = 4f / Width;
        }
        public override void Update(TimeSpan delta) {
            if(fast)
                x -= (int)(4 * (x + 16) * delta.TotalSeconds);

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
            
            next.Update(delta);
            base.Update(delta);
        }
        public override void Draw(TimeSpan delta) {
            this.Clear();
            next.Draw(delta);
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

            base.Draw(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if(keyboard.IsKeyPressed(Keys.Enter)) {
                fast = true;
            }
            return base.ProcessKeyboard(keyboard);
        }
    }
}
