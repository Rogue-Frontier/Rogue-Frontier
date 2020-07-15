using ASECII;
using SadConsole.Input;
using System;
using SadRogue.Primitives;
using SadConsole;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadConsole.UI.Themes;
using Console = SadConsole.Console;
using System.Linq;
using System.Collections.Generic;

static class UI {
    public class ListItem<T> {
        public string name;
        public T item;
        public ListItem(string name, T item) {
            this.name = name;
            this.item = item;
        }
        public static implicit operator T(ListItem<T> i) => i.item;
    }
    public class TextField : Console {
        public int index {
            get => _index;
            set {
                _index = Math.Clamp(value, 0, text.Length);
                UpdateTextStart();
            }
        }
        private int _index;
        private int textStart;
        public string text;
        public string placeholder;
        private double time;
        private MouseWatch mouse;

        public delegate void TextChange(TextField source);
        public event TextChange TextChanged;
        public TextField(int Width) : base(Width, 1) {
            _index = 0;
            text = "";
            placeholder = new string('.', Width);
            time = 0;
            mouse = new MouseWatch();
            FocusOnMouseClick = true;
        }
        public void UpdateTextStart() {
            textStart = Math.Max(Math.Min(text.Length, _index) - Width + 1, 0);
        }
        public override void Update(TimeSpan delta) {
            time += delta.TotalSeconds;
            base.Update(delta);
        }
        public override void Draw(TimeSpan delta) {
            this.Clear();


            var text = this.text;
            var showPlaceholder = this.text.Length == 0 && !IsFocused;
            if (showPlaceholder) {
                text = placeholder;
            }
            int x2 = Math.Min(text.Length - textStart, Width);

            bool showCursor = time % 2 < 1;

            Color foreground = IsMouseOver ? Color.Yellow : Color.White;
            Color background = IsFocused ? new Color(51, 51, 51, 255) : Color.Black;

            if(mouse.left == MouseState.Held) {
                (foreground, background) = (background, foreground);
            }
            for (int x = 0; x < Width; x++) {
                this.SetBackground(x, 0, background);
            }
            Func<int, ColoredGlyph> getGlyph = (i) => new ColoredGlyph(foreground, background, text[i]);
            if(showCursor && IsFocused) {
                if (_index < text.Length) {
                    getGlyph = i =>
                               i == _index ? new ColoredGlyph(background, foreground, text[i])
                                           : new ColoredGlyph(foreground, background, text[i]);
                } else {
                    this.SetBackground(x2, 0, foreground);
                }
            }
            for (int x = 0; x < x2; x++) {
                var i = textStart + x;
                this.SetCellAppearance(x, 0, getGlyph(i));
            }
            base.Draw(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if (keyboard.KeysPressed.Any()) {
                //bool moved = false;
                bool changed = false;
                foreach (var key in keyboard.KeysPressed) {
                    switch (key.Key) {
                        case Keys.Up:
                            _index = 0;
                            time = 0;
                            UpdateTextStart();
                            break;
                        case Keys.Down:
                            _index = text.Length;
                            time = 0;
                            UpdateTextStart();
                            break;
                        case Keys.Right:
                            _index = Math.Min(_index + 1, text.Length);
                            time = 0;
                            UpdateTextStart();
                            break;
                        case Keys.Left:
                            _index = Math.Max(_index - 1, 0);
                            time = 0;
                            UpdateTextStart();
                            break;
                        case Keys.Back:
                            if (text.Length > 0) {
                                if (_index == text.Length) {
                                    text = text.Substring(0, text.Length - 1);
                                } else if (_index > 0) {
                                    text = text.Substring(0, _index) + text.Substring(_index + 1);
                                }
                                _index--;
                                time = 0;
                                UpdateTextStart();
                                changed = true;
                            }

                            break;
                        default:
                            if (key.Character != 0) {
                                if (_index == text.Length) {
                                    text += key.Character;
                                    _index++;
                                } else if (_index > 0) {
                                    text = text.Substring(0, index) + key.Character + text.Substring(index, 0);
                                    _index++;
                                } else {
                                    text = (key.Character) + text;
                                }
                                time = 0;
                                UpdateTextStart();
                                changed = true;
                            }
                            break;
                    }
                }
                if(changed) {
                    TextChanged?.Invoke(this);
                }
            }
            return base.ProcessKeyboard(keyboard);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
    }

    public class ButtonList {
        public Console Parent;
        public Point Position;
        public List<LabelButton> buttons;
        public ButtonList(Console Parent, Point Position) {
            this.Parent = Parent;
            this.Position = Position;
            buttons = new List<LabelButton>();
        }
        public void Add(string label, Action clicked) {
            var b = new LabelButton(label, clicked) {
                Position = Position + new Point(0, buttons.Count),
            };
            buttons.Add(b);
            Parent.Children.Add(b);
        }
        public void Clear() {
            foreach(var b in buttons) {
                Parent.Children.Remove(b);
            }
            buttons.Clear();
        }

    }

    public static char indexToLetter(int index) {
        if (index < 26) {
            return (char)('a' + index);
        } else {
            return '\0';
        }
    }
    public static int letterToIndex(char ch) {
        ch = char.ToLower(ch);
        if (ch >= 'a' && ch <= 'z') {
            return (ch - 'a');
        } else {
            return -1;
        }
    }


    public static char indexToKey(int index) {
        //0 is the last key; 1 is the first
        if (index < 10) {
            return (char)('0' + (index + 1) % 10);
        } else {
            index -= 10;
            if (index < 26) {
                return (char)('a' + index);
            } else {
                return '\0';
            }
        }
    }
    public static int keyToIndex(char ch) {
        //0 is the last key; 1 is the first
        if (ch >= '0' && ch <= '9') {
            return (ch - '0' + 9) % 10;
        } else {
            ch = char.ToLower(ch);
            if (ch >= 'a' && ch <= 'z') {
                return (ch - 'a') + 10;
            } else {
                return -1;
            }
        }
    }
}

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