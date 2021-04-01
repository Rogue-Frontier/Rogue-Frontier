using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SadConsole;
using SadConsole.Input;
using static UI;
using Console = SadConsole.Console;
using SadRogue.Primitives;

namespace TranscendenceRL {
    public class ArmorRepairScreen : Console {
        Console prev;
        PlayerShip playerShip;
        List<Armor> armor;
        ArmorRepair item;

        int? index;
        public ArmorRepairScreen(Console prev, PlayerShip playerShip, List<Armor> armor, ArmorRepair item) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.playerShip = playerShip;
            this.armor = armor;
            this.item = item;

            if (playerShip.Cargo.Any()) {
                index = 0;
            }
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            var from = armor;

            foreach (var key in keyboard.KeysPressed) {
                switch (key.Key) {
                    case Keys.Up:
                        if (from.Any()) {
                            if (index == null) {
                                index = from.Count - 1;
                            } else {
                                index = Math.Max(index.Value - 1, 0);
                            }
                        } else {
                            index = null;
                        }
                        break;
                    case Keys.Down:
                        
                        index = from.Any() ? (index == null ? 0 : Math.Min(index.Value + 1, from.Count - 1)) : (int?)null;
                        break;
                    case Keys.Enter:
                        if (index != null) {
                            var item = from.ElementAt(index.Value);
                            Apply(item);

                            index = from.Any() ? Math.Min(index ?? 0, from.Count - 1) : (int?)null;
                        }
                        break;
                    case Keys.Escape:
                        Parent.Children.Remove(this);
                        prev.IsFocused = true;
                        break;
                    default:
                        var ch = char.ToLower(key.Character);
                        if (ch >= 'a' && ch <= 'z') {
                            var letterIndex = letterToIndex(ch);
                            if (letterIndex < from.Count) {
                                var item = from.ElementAt(letterIndex);
                                Apply(item);
                                index = from.Any() ? Math.Min(index.Value, from.Count - 1) : (int?)null;
                            }
                        }
                        break;
                }
            }

            return base.ProcessKeyboard(keyboard);
        }
        public void Apply(Armor target) {

        }
        public override void Render(TimeSpan delta) {

            int x = 16;
            int y = 16;

            this.Clear();
            this.RenderBackground();
            foreach (var point in new Rectangle(x, y, 32, 26).Positions()) {
                this.SetCellAppearance(point.X, point.Y, new ColoredGlyph(Color.Gray, Color.Transparent, '.'));
            }
            this.Print(x, y, playerShip.Name, Color.White, Color.Black);
            y++;
            int i = 0;
            int? highlight = null;

            if (index != null) {
                i = Math.Max(index.Value - 16, 0);
                highlight = index;
            }

            if (armor.Any()) {
                while (i < armor.Count) {

                    var highlightColor = i == highlight ? Color.Yellow : Color.White;
                    var name = new ColoredString($"{UI.indexToLetter(i)}. ", highlightColor, Color.Transparent)
                             + new ColoredString(armor[i].source.type.name, highlightColor, Color.Transparent);
                    this.Print(x, y, name);

                    i++;
                    y++;
                }
            } else {
                var highlightColor = Color.Yellow;
                var name = new ColoredString("<Empty>", highlightColor, Color.Transparent);
                this.Print(x, y, name);
            }
            base.Render(delta);
        }
    }
}
