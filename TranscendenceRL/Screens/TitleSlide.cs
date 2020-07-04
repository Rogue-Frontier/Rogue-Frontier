
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using Common;

namespace TranscendenceRL.Screens {
    class TitleSlide : Console {
        public Console next;
        int x = 0;
        double time = 0;
        public TitleSlide(int width, int height, Console next) : base(width, height) {
            x = width;
            this.next = next;
        }
        public override void Update(TimeSpan delta) {
            time += delta.TotalSeconds;
            if(time > 4f / Width) {
                time = 0;
                if (x > -16) {
                    x--;
                } else {

                    SadConsole.Game.Instance.Screen = next;
                    next.IsFocused = true;
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
                    fore = fore.Blend(Color.Black.WithValues(alpha: value));

                    var back = next.GetBackground(x, y);
                    back = back.Blend(Color.Black.WithValues(alpha: value));

                    this.SetCellAppearance(x, y, new ColoredGlyph(fore, back, glyph));
                }
            }

            base.Draw(delta);
        }
    }
}
