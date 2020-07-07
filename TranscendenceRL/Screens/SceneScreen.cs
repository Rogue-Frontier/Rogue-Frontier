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
    class SceneScreen : Console {
        public IScene view;
        public SceneScreen(int Width, int Height, ISceneDesc desc, PlayerShip player, Dockable dock) : base(Width, Height) {
            this.view = desc.Get(Navigate, player, dock);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            view.Handle(info);
            return base.ProcessKeyboard(info);
        }
        public override void Update(TimeSpan ts) {
        }
        public void Navigate(IScene view) {
            if (view == null) {
                var p = Parent;
                Parent.Children.Remove(this);
                p.IsFocused = true;
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
