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
        public int y = 0;
        public double alpha;

        public Console prev;
        public Console next;
        public TitleTransition(int width, int height, Console prev, Console next) : base(width, height) {
            DefaultBackground = Color.Black;
            DefaultForeground = Color.Black;
            this.prev = prev;
            this.next = next;

        }
        public override void Update(TimeSpan delta) {
            if(y < Height) {
                y += (int)((Height - y) * 0.1 / 30);   
            } else if (alpha < 255) {
                alpha += delta.TotalSeconds * 5;
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
            if (this.y < Height) {
                this.Clear();
                for (int y = 0; y < Height - this.y; y++) {
                    for (int x = 0; x < Width; x++) {
                        this.SetCellAppearance(x, y, prev.GetCellAppearance(x, y));
                    }
                }
                for (int y = Height - this.y; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        this.SetBackground(x, y, Color.Black);
                    }
                }
            } else {
                this.Clear();
                for(int y = 0; y < Height; y++) {
                    for(int x = 0; x < Width; x++) {
                        var glyph = next.GetGlyph(x, y);
                        var foreground = next.GetForeground(x, y);
                        var background = next.GetBackground(x, y);
                        foreground = foreground.WithValues(alpha: (int)alpha);
                        background = background.WithValues(alpha: (int)alpha);
                        this.SetCellAppearance(x, y, new ColoredGlyph(foreground, background, glyph));
                    }
                }
            }
            

        }
    }

}
