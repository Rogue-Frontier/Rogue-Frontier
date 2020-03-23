using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class CrawlScreen : Window {
        private readonly string text;
        private int lines;
        private int index;
        int tick;
        public CrawlScreen(int width ,int height) : base(width, height) {
            text = Properties.Resources.Crawl.Replace("\r\n", "\n");
            lines = text.Count(c => c == '\n') + 1;
            index = 0;
            tick = 0;
        }
        public override void Update(TimeSpan time) {
            if(index < text.Length) {
                tick++;
                if(tick%5 == 0) {
                    index++;
                }
            }
        }
        public override void Draw(TimeSpan drawTime) {
            int ViewWidth = Width;
            int ViewHeight = Height;

            int leftMargin = (ViewWidth) / 2;
            int topMargin = (ViewHeight / 2) - lines / 2;
            int x = leftMargin;
            int y = topMargin;
            Print(x, y, " ");
            for (int i = 0; i < index; i++) {
                char c = text[i];
                if(c == '\n') {
                    x = leftMargin;
                    y++;
                } else {
                    Print(x, y, "" + c, Color.White, Color.Black);
                    x++;
                }
            }
            base.Draw(drawTime);
        }
    }
}
