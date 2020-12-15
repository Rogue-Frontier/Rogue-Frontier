using ArchConsole;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Console = SadConsole.Console;

namespace TranscendenceRL.Screens {
    class ConfigMenu : Console {
        Settings settings;
        MouseWatch mouse;

        ControlKeys? currentSet;
        Dictionary<ControlKeys, LabelButton> buttons;

        public ConfigMenu(int Width, int Height, Settings settings) : base(Width, Height) {
            this.settings = settings;
            mouse = new MouseWatch();

            UseKeyboard = true;
            FocusOnMouseClick = true;

            currentSet = null;
            buttons = new Dictionary<ControlKeys, LabelButton>();

            Init();
        }
        public void Reset() {
            Children.Clear();
            Init();
        }
        public void Init() {
            int x = 2;
            int y = 0;

            var controls = settings.controls;
            foreach (var control in controls.Keys) {
                var c = control;
                string label = GetLabel(c);
                LabelButton b = null;
                b = new LabelButton(label, () => {
                    ResetLabel();
                    currentSet = c;
                    b.text = $"{control.ToString(),-16} {"[Press Key]",-12}";
                }) { Position = new Point(x, y++), FontSize = FontSize };

                buttons[control] = b;
                Children.Add(b);
            }
        }
        string GetLabel(ControlKeys control) => $"{control.ToString(),-16} {settings.controls[control].ToString(), -12}";
        public void ResetLabel() {
            if (currentSet.HasValue) {
                buttons[currentSet.Value].text = GetLabel(currentSet.Value);
            }
        }

        public override bool ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(Keys.Escape)) {
                if (currentSet.HasValue) {
                    buttons[currentSet.Value].text = GetLabel(currentSet.Value);
                    currentSet = null;
                } else {
                    Parent.Children.Remove(this);
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
