using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using SadConsole;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using Color = SadRogue.Primitives.Color;
using Common;

namespace TranscendenceRL {
    class TitleTransition : Console {
        //Slide down background over prev console, then fade in next console
        public double y = 0;
        public double alpha;

        public Console prev;
        public Console next;
        public TitleTransition(int width, int height, Console prev, Console next) : base(width, height) {
            DefaultBackground = Color.Black;
            DefaultForeground = Color.Black;
            this.prev = prev;
            this.next = next;

            //Draw one frame now so that we don't cut out for one frame
            Draw(new TimeSpan());
        }
        public override void Update(TimeSpan delta) {
            if(y < Height) {
                y += delta.TotalSeconds * Math.Max(((Height - y)) * 4, 8);   
            } else if (alpha < 1) {
                alpha += delta.TotalSeconds * Math.Max((1 - alpha) * 2, 1/2f);
            } else {
                SadConsole.Game.Instance.Screen = next;
                next.IsFocused = true;
            }

            base.Update(delta);

            prev.Update(delta);
            next.Update(delta);
        }
        public override void Draw(TimeSpan delta) {

            prev.Draw(delta);
            next.Draw(delta);
            base.Draw(delta);
            this.Clear();

            var blank = new ColoredGlyph(Color.Black, Color.Black);
            if (this.y < Height) {
                var edge = Height - (int)this.y;
                for (int y = 0; y < edge; y++) {
                    for (int x = 0; x < Width; x++) {
                        this.SetCellAppearance(x, y, prev.GetCellAppearance(x, y));
                    }
                }
                for (int y = edge; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        this.SetCellAppearance(x, y, blank);
                    }
                }
                for (int y  = Math.Max(0, edge - 16); y < edge; y++) {
                    for (int x = 0; x < Width; x++) {
                        var glyph = prev.GetGlyph(x, y);
                        var value = 255 - 255 / 16 * (edge - y);

                        var fore = prev.GetForeground(x, y);
                        fore = fore.Premultiply().Blend(Color.Black.WithValues(alpha: value));

                        var back = prev.GetBackground(x, y);
                        back = back.Premultiply().Blend(Color.Black.WithValues(alpha: value));

                        this.SetCellAppearance(x, y, new ColoredGlyph(fore, back, glyph));
                    }
                }
            } else {
                for (int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        var glyph = next.GetGlyph(x, y);
                        var foreground = next.GetForeground(x, y);
                        var background = next.GetBackground(x, y);
                        foreground = foreground.WithValues(alpha: (int)(foreground.A * alpha));
                        background = background.WithValues(alpha: (int)(background.A * alpha));
                        this.SetCellAppearance(x, y, new ColoredGlyph(foreground, background, glyph));
                    }
                }
            }
            

        }
    }

}
