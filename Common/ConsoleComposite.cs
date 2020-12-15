using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using SadConsole;
using Console = SadConsole.Console;

namespace Common {
    public class ConsoleComposite {
        List<Console> consoles;
        public ColoredGlyph this[int x, int y] { get {

                List<CellDecorator> d = new List<CellDecorator>();
                Color f = Color.Transparent;
                Color b = Color.Transparent;
                int g = 0;
                foreach(var c in consoles) {
                    var cg = c.GetCellAppearance(x, y);
                    if(cg.Glyph != 0 && cg.Glyph != ' ' && cg.Foreground.A != 0) {
                        d.Add(new CellDecorator(f, g, Mirror.None));

                        f = cg.Foreground;
                        g = cg.Glyph;
                    }
                    b = b.Premultiply().Blend(cg.Background);
                }
                return new ColoredGlyph(f, b, g);
        } }
        public ConsoleComposite(params Console[] consoles) => this.consoles = new List<Console>(consoles);
        public ConsoleComposite(IEnumerable<Console> consoles) => this.consoles = new List<Console>(consoles);
    }
}
