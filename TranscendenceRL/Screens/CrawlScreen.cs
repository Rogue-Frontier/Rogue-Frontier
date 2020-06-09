using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Common.Helper;

namespace TranscendenceRL {
    class CrawlScreen : Window {
        World World;
        ShipClass playerClass;

        private readonly string text;
        private int lines;
        private int index;
        int tick;

        bool speedUp;
        LoadingSymbol loading;
        int loadingTicks = 150 * 0;

        ColoredString[] effect;
        public CrawlScreen(int width ,int height, World World, ShipClass playerClass) : base(width, height) {
            this.World = World;
            this.playerClass = playerClass;

            text = Properties.Resources.Crawl.Replace("\r\n", "\n");
            lines = text.Count(c => c == '\n') + 1;
            index = 0;
            tick = 0;

            int effectWidth = Width * 3 / 5;
            int effectHeight = Height * 3 / 5;

            Random r = new Random();
            effect = new ColoredString[effectHeight];
            for(int y = 0; y < effectHeight; y++) {
                effect[y] = new ColoredString(effectWidth);
                for(int x = 0; x < effectWidth; x++) {
                    effect[y][x] = GetGlyph(x, y);
                }
            }

            

            Color Front(int value) {
                return new Color(255 - value / 2, 255 - value, 255, 255 - value / 4);
                //return new Color(128 + value / 2, 128 + value/4, 255);
            }
            Color Back(int value) {
                //return new Color(255 - value, 255 - value, 255 - value).Noise(r, 0.3).Round(17).Subtract(25);
                return new Color(204 - value, 204 - value, 255 - value).Noise(r, 0.3).Round(17).Subtract(25);
            }
            ColoredGlyph GetGlyph(int x, int y) {
                Color front = Front(255 * x / effectWidth);
                Color back = Back(255 * x / effectWidth);
                char c;
                if (r.Next(x) < 5
                    || (effect[y][x-1].GlyphCharacter != ' ' && r.Next(x) < 10)
                    ) {
                    const string vwls = "?&%~=+;";
                    c = vwls[r.Next(vwls.Length)];
                } else {
                    c = ' ';
                }
                
                
                return new ColoredGlyph(c, front, back);
            }
        }
        public override void Update(TimeSpan time) {
            if(index < text.Length) {
                tick++;
                if(speedUp) {
                    index++;
                } else {
                    if (tick % 4 == 0) {
                        index++;
                    }
                }
            } else if (loading == null) {
                loading = new LoadingSymbol(16);
            } else if(loadingTicks > 0) {
                loading.Update();
                loadingTicks--;
            } else {
                Hide();
                new GameConsole(Width, Height, World, playerClass).Show(true);
            }
        }
        public override void Draw(TimeSpan drawTime) {
            Clear();
            int effectY = Height / 5;
            foreach (var line in effect) {
                Print(0, effectY, line);
                effectY++;
            }

            int ViewWidth = Width;
            int ViewHeight = Height;

            //int leftMargin = (ViewWidth) / 2;
            int leftMargin = Width * 2 / 5;
            int topMargin = (ViewHeight / 2) - lines / 2;
            int textX = leftMargin;
            int textY = topMargin;
            for (int i = 0; i < index; i++) {
                char c = text[i];
                if(c == '\n') {
                    textX = leftMargin;
                    textY++;
                } else {
                    if (c != ' ') {
                        Print(textX, textY, "" + c, Color.White, GetBackground(textX, textY));
                    }
                    textX++;
                }
            }

            if(loading != null) {
                var symbol = loading.Draw();
                int symbolX = Width - symbol[0].Count;
                int symbolY = Height - symbol.Length;
                foreach (var line in symbol) {
                    Print(symbolX, symbolY, line);
                    symbolY++;
                }

                Print(0, Height - 1, "[Creating Game...]");
            } else {
                if(speedUp) {
                    Print(0, Height - 1, "[Press Enter again to skip intro]");
                } else {
                    Print(0, Height - 1, "[Press Enter to speed up intro]");
                }
                
            }
            

            base.Draw(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Enter)) {
                if(speedUp) {
                    index = text.Length;
                } else {
                    speedUp = true;
                }
            }

            return base.ProcessKeyboard(info);
        }
    }
}
