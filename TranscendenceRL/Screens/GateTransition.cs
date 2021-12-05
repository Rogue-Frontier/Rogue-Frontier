using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = SadConsole.Console;
namespace TranscendenceRL.Screens {
    public class GateTransition : Console {
        Viewport prev, next;
        double amount;
        Rectangle rect;
        public Action Transition;
        public GateTransition(Viewport prev, Viewport next, Action Transition) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.next = next;
            DefaultBackground = Color.Transparent;
            rect = new(new(Width / 2, Height / 2), 0, 0);
            this.Transition = Transition;
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if (keyboard.IsKeyPressed(Keys.Enter)) {
                Transition();
            }
            return base.ProcessKeyboard(keyboard);
        }
        public override void Update(TimeSpan delta) {
            prev.Update(delta);
            //next.Update(delta);
            base.Update(delta);
            amount += delta.TotalSeconds * 2;

            rect = new Rectangle(new(Width / 2, Height / 2), (int)(amount*Width /2), (int)(amount*Height/2));
            if (amount > 1) {
                Transition();
            }
        }
        public override void Render(TimeSpan delta) {
            this.Clear();
            HashSet<Point> edge = new(rect.PerimeterPositions());

            Console back = new Console(Width, Height);

            BackdropConsole prevBack = new(prev);
            BackdropConsole nextBack = new(next);

            foreach (var y in Enumerable.Range(0, Height)) {
                foreach (var x in Enumerable.Range(0, Width)) {
                    Point p = new(x, y);
                    if(edge.Contains(p)) {
                        this.SetCellAppearance(x, y, new ColoredGlyph(Color.White, Color.Black, '#'));
                        continue;
                    }
                    (var v, var b) = rect.Contains(p) ? (next, nextBack) : (prev, prevBack);
                    back.SetCellAppearance(x, y, b.GetTile(x, y));
                    ColoredGlyph g = v.GetTile(x, y);
                    //var g = (rect.Contains(p) ? next : prev).GetCellAppearance(x, y);
                    this.SetCellAppearance(x, Height-y, g);
                    
                }
            }
            back.Render(delta);
            base.Render(delta);
        }
    }
}
