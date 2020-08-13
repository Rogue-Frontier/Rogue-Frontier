using ASECII;
using Common;
using SadConsole;
using SadConsole.Input;
using SadConsole.Renderers;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Console = SadConsole.Console;

namespace TranscendenceRL.Screens {
    class ConfigScreen : Console {
        TitleScreen prev;
        Settings settings;
        MouseWatch mouse;

        ControlKeys? currentSet;
        Dictionary<ControlKeys, LabelButton> buttons;

        public ConfigScreen(TitleScreen prev, Settings settings, World World) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.settings = settings;
            mouse = new MouseWatch();

            UseKeyboard = true;
            FocusOnMouseClick = true;

            currentSet = null;
            buttons = new Dictionary<ControlKeys, LabelButton>();
            var controls = settings.controls;

            int x = 3;
            int y = 16;
            foreach (var control in settings.controls.Keys) {
                var c = control;
                string label = GetLabel(c);
                LabelButton b = null;
                b = new LabelButton(label, () => {
                    ResetLabel();
                    currentSet = c;
                    b.text = $"{control.ToString(),-16} [Press Key]";
                }) { Position = new Point(x, y++) };
                
                buttons[control] = b;
                Children.Add(b);
            }
        }
        string GetLabel(ControlKeys control) => $"{control.ToString(),-16} {settings.controls[control].ToString()}";
        public void ResetLabel() {
            if (currentSet.HasValue) {
                buttons[currentSet.Value].text = GetLabel(currentSet.Value);
            }
        }
        public override void Update(TimeSpan timeSpan) {
            prev.Update(timeSpan);
            base.Update(timeSpan);
        }
        public override void Render(TimeSpan drawTime) {
            this.Clear();
            prev.Render(drawTime);
            base.Render(drawTime);
        }

        public override bool ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(Keys.Escape)) {
                if (currentSet.HasValue) {
                    buttons[currentSet.Value].text = GetLabel(currentSet.Value);
                    currentSet = null;
                } else {
                    SadConsole.Game.Instance.Screen = prev;
                    prev.IsFocused = true;
                }
            } else if(info.KeysPressed.Any()) {
                if(currentSet.HasValue) {
                    settings.controls[currentSet.Value] = info.KeysPressed.First().Key;
                    ResetLabel();
                    currentSet = null;
                }
            }


            return base.ProcessKeyboard(info);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
    }
}
