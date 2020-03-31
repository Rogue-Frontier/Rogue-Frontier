using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class Dockscreen : Window {
        public IDockView view;
        public Window previous;
        public Dockscreen(int Width, int Height, IDockViewDesc desc, Window previous, PlayerShip player, Dockable dock) : base(Width, Height) {
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
                Hide();
                previous.Show();
            } else {
                this.view = view;
            }
        }
        public override void Draw(TimeSpan drawTime) {
            Clear();
            view.Draw(this);
            base.Draw(drawTime);
        }
    }
}
