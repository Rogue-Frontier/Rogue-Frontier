using SadConsole;
using SadConsole.Input;
using SadConsole.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = SadConsole.Console;

namespace TranscendenceRL {
    class Dockscreen : Console {
        public IDockView view;
        public Console previous;
        public Dockscreen(int Width, int Height, IDockViewDesc desc, Console previous, PlayerShip player, Dockable dock) : base(Width, Height) {
            this.view = desc.Get(Navigate, player, dock);
            this.previous = previous;
        }
        public override bool ProcessKeyboard(Keyboard info) {
            view.Handle(info);
            return base.ProcessKeyboard(info);
        }
        public override void Update(TimeSpan ts) {
        }
        public void Navigate(IDockView view) {
            if (view == null) {
                SadConsole.Game.Instance.Screen = previous;
                previous.IsFocused = true;
            } else {
                this.view = view;
            }
        }
        public override void Draw(TimeSpan drawTime) {
            this.Clear();
            view.Draw(this.Surface);
            base.Draw(drawTime);
        }
    }
}
