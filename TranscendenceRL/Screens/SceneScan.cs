
using Common;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Timers;
using Console = SadConsole.Console;

namespace TranscendenceRL {
    public class SceneScan : Console {
        Console next;
        int y;
        public SceneScan(Console next) : base(next.Width, next.Height) {
            y = 0;
            this.next = next;
            next.Render(new TimeSpan());
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if(keyboard.IsKeyPressed(Keys.Enter)) {
                y = next.Height;
            }
            return base.ProcessKeyboard(keyboard);
        }
        public override void Update(TimeSpan delta) {
            if(y < next.Height) {
                y += 1;
            } else {
                var p = Parent;
                p.Children.Remove(this);
                p.Children.Add(next);
                next.IsFocused = true;
            }
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            this.Clear();

            var last = this.y - 1;

            int y;
            for (y = 0; y < last; y++) {
                for(int x = 0; x < Width; x++) {
                    this.SetCellAppearance(x, y, next.GetCellAppearance(x, y));
                }
            }
            y = last;
            for (int x = 0; x < Width; x++) {
                this.SetCellAppearance(x, y, new ColoredGlyph(Color.Transparent, Color.White.SetAlpha(128)));
            }
            ColoredString empty = new ColoredString(new string(' ', Width), Color.Transparent, Color.Transparent);
            for(y = last + 1; y < Height; y++) {
                this.Print(0, y, empty);
            }
            base.Render(delta);

//            next.Render(delta);
        }
    }
}
