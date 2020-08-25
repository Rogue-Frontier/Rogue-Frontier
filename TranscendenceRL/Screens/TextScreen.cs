using SadConsole;
using SadConsole.Input;
using SadConsole.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Common.Main;
using static SadConsole.ColoredString;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using System.IO;

namespace TranscendenceRL {
    class TextScreen : Console {
        private Console next;
        private readonly string text;
        bool speedUp;
        int index;
        int tick;
        double delay;

        public TextScreen(int Width, int Height, string text, Console next) : base(Width, Height) {
            this.next = next;
            this.text = text;
            delay = 2;
        }
        public override void Update(TimeSpan time) {
            if (index < text.Length) {
                tick++;
                if (speedUp) {
                    index++;
                } else {
                    if (tick % 4 == 0) {
                        index++;
                    }
                }
            } else if (delay > 0) {
                delay -= time.TotalSeconds;
            } else {
                SadConsole.Game.Instance.Screen = next;
                next.IsFocused = true;
            }
        }
        public override void Render(TimeSpan drawTime) {
            base.Render(drawTime);
            this.Clear();

            int left = Width / 4,
                top = 8,
                x = left,
                y = top;

            for(int i = 0; i < index; i++) {
                if(text[i] == '\n') {
                    x = left;
                    y++;
                } else {
                    this.SetCellAppearance(x, y, new ColoredGlyph(Color.White, Color.Black, text[i]));
                    x++;
                }
            }
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(SadConsole.Input.Keys.Enter)) {
                if (speedUp) {
                    index = text.Length;
                } else {
                    speedUp = true;
                }
            }

            return base.ProcessKeyboard(info);
        }
    }
}
