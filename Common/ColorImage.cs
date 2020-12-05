using ASECII;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using System.Linq;
using SadConsole;
using SadRogue.Primitives;
using System.IO;
using ASECII;
using Console = SadConsole.Console;

namespace Common {
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
        public void Render(Console onto, Point pos) {
            foreach((var p, var t) in Sprite) {
                (var x, var y) = pos + p;
                onto.SetCellAppearance(x, y, t);
            }
        }
        public static ColorImage FromFile(string file) => new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText(file)));
    }

}
