using ASECII;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using System.Linq;
using SadConsole;
using SadRogue.Primitives;
namespace TranscendenceRL {
    public class ColorImage {
        public Dictionary<(int x, int y), ColoredGlyph> Sprite;
        public Point Size;
        public ColorImage(Dictionary<(int x, int y), TileValue> sprite) {
            int left = sprite.Keys.Min(p => p.x);
            int top = sprite.Keys.Min(p => p.y);
            int right = sprite.Keys.Max(p => p.x);
            int bottom = sprite.Keys.Max(p => p.y);

            Size = new Point(right - left, bottom - top);

            var origin = new Point(left, top);
            this.Sprite = new Dictionary<(int x, int y), ColoredGlyph>();
            foreach ((var p, var t) in sprite) {
                this.Sprite[p - origin] = t;
            }
        }
    }
}
