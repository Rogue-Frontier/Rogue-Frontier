using ASECII;
using SadConsole.Input;
using System;
using SadRogue.Primitives;
using SadConsole;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadConsole.UI.Themes;

class LabeledControl : ControlsConsole {
    public string label;
    public TextBox textBox;
    public LabeledControl(string label, string text = "", Action<string> TextChanged = null) : base((label.Length/8 + 1)*8 + 16, 1) {
        (DefaultBackground, DefaultForeground) = (Color.Black, Color.White);
        this.label = label;
        this.textBox = new TextBox(16) {
            Text = text,
            Position = new Point((label.Length / 8 + 1) * 8, 0),
            ThemeColors = new Colors() {
                Appearance_ControlMouseDown = new ColoredGlyph(Color.Black, Color.White),
                Appearance_ControlOver = new ColoredGlyph(Color.White, Color.Gray),
                Appearance_ControlNormal = new ColoredGlyph(Color.White, Color.Black),
                Appearance_ControlFocused = new ColoredGlyph(Color.White, Color.Black),
                Text = Color.White
            }
        };
        if(TextChanged != null)
            this.textBox.TextChanged += (e, s) => TextChanged.Invoke(this.textBox.Text);
        this.ControlHostComponent.Add(textBox);
        this.FocusOnMouseClick = true;
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        if(keyboard.IsKeyPressed(Keys.Enter)) {
            this.IsFocused = false;
            textBox.IsFocused = false;
            this.Parent.IsFocused = true;
        }
        return base.ProcessKeyboard(keyboard);
    }
    public override void Draw(TimeSpan delta) {
        this.Clear();
        this.Print(0, 0, label, Color.White, Color.Black);
        base.Draw(delta);
    }
}
class Label : SadConsole.Console {
    public ColoredString text {
        set {
            _text = value;
            Resize(_text.Count, 1, _text.Count, 1, false);
        }
        get {
            return _text;
        }
    }
    private ColoredString _text;
    public Label(string text) : base(text.Length, 1) {
        this.text = new ColoredString(text);
    }
    public override void Draw(TimeSpan delta) {
        this.Print(0, 0, text);
        base.Draw(delta);
    }
}
class LabelButton : SadConsole.Console {
    public string text { set {
            _text = value;
            Resize(_text.Length, 1, _text.Length, 1, false);
        } get { return _text; } }
    private string _text;
    Action click;
    MouseWatch mouse;

    public LabelButton(string text, Action click) : base(1, 1) {
        this.text = text;
        this.click = click;
        this.mouse = new MouseWatch();
    }
    public override bool ProcessMouse(MouseScreenObjectState state) {
        mouse.Update(state, IsMouseOver);
        if (IsMouseOver) {
            if (mouse.leftPressedOnScreen && mouse.left == MouseState.Released) {
                click();
            }

        }
        return base.ProcessMouse(state);
    }
    public override void Draw(TimeSpan timeElapsed) {
        if (IsMouseOver && mouse.nowLeft && mouse.leftPressedOnScreen) {
            this.Print(0, 0, text, Color.Black, Color.White);
        } else {
            this.Print(0, 0, text, Color.White, IsMouseOver ? Color.Gray : Color.Black);
        }


        base.Draw(timeElapsed);
    }
}