using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Console = SadConsole.Console;
using static UI;
using SadRogue.Primitives;

namespace TranscendenceRL {


    interface ITrader {
        string name { get; }
        HashSet<Item> cargo { get; }
    }
    class TradeScene : Console {
        Console prev;
        Player player;
        PlayerShip playerShip;
        ITrader docked;
        HashSet<Item> playerItems => playerShip.cargo;
        HashSet<Item> dockedItems => docked.cargo;
        bool playerSide;
        int? playerIndex;
        int? dockedIndex;
        public TradeScene(Console prev, PlayerShip playerShip, ITrader docked) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.player = playerShip.player;
            this.playerShip = playerShip;
            this.docked = docked;
            this.playerSide = false;

            if (playerShip.cargo.Any()) {
                playerIndex = 0;
            }
            if (this.docked.cargo.Any()) {
                dockedIndex = 0;
            }
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            var from = playerSide ? playerItems : dockedItems;
            var to = playerSide ? dockedItems : playerItems;
            ref int? index = ref (playerSide ? ref playerIndex : ref dockedIndex);

            foreach (var key in keyboard.KeysPressed) {
                switch (key.Key) {
                    case Keys.Up:
                        index = from.Any()
                            ? (index == null ? (from.Count - 1) : Math.Max(index.Value - 1, 0))
                            : null;
                        break;
                    case Keys.PageUp:
                        index = from.Any()
                            ? (index == null ? (from.Count - 1) : Math.Max(index.Value - 26, 0))
                            : null;
                        break;
                    case Keys.Down:
                        index = from.Any()
                            ? (index == null ? 0 : Math.Min(index.Value + 1, from.Count - 1))
                            : null;
                        break;
                    case Keys.PageDown:
                        index = from.Any()
                            ? (index == null ? 0 : Math.Min(index.Value + 26, from.Count - 1))
                            : null;
                        break;
                    case Keys.Left:
                        playerSide = true;

                        from = playerItems;
                        index = ref playerIndex;

                        if (from.Any()) {
                            index = Math.Min(index ?? 0, from.Count - 1);
                        } else {
                            index = null;
                        }
                        break;
                    case Keys.Right:
                        playerSide = false;

                        from = dockedItems;
                        index = ref dockedIndex;

                        if (from.Any()) {
                            index = Math.Min(index ?? 0, from.Count - 1);
                        } else {
                            index = null;
                        }
                        break;
                    case Keys.Enter:
                        Transact();
                        break;
                    case Keys.Escape:
                        var p = Parent;
                        p.Children.Remove(this);
                        p.Children.Add(prev);
                        prev.IsFocused = true;
                        break;
                    default:
                        var ch = char.ToLower(key.Character);
                        if (ch >= 'a' && ch <= 'z') {
                            int start = Math.Max(index.Value - 13, 0);
                            var letterIndex = start + letterToIndex(ch);
                            if (letterIndex < from.Count) {
                                index = letterIndex;
                                Transact();
                            }
                        }
                        break;
                }
            }
            void Transact() {

                ref int? index = ref (playerSide ? ref playerIndex : ref dockedIndex);
                if (index != null) {
                    var item = from.ElementAt(index.Value);
                    if (playerSide) {
                        player.money += item.type.value;
                    } else {
                        if (player.money < item.type.value) {
                            return;
                        }
                        player.money -= item.type.value;
                    }
                    from.Remove(item);
                    to.Add(item);
                    if (from.Any()) {
                        index = Math.Min(index ?? 0, from.Count - 1);
                    } else {
                        index = null;
                    }
                }
            }
            return base.ProcessKeyboard(keyboard);
        }
        public override void Render(TimeSpan delta) {
            int x = 16;
            int y = 16;

            this.Clear();
            this.RenderBackground();
            foreach (var point in new Rectangle(x, y, 32, 26).Positions()) {
                this.SetCellAppearance(point.X, point.Y, new ColoredGlyph(Color.Gray, Color.Transparent, '.'));
            }
            this.Print(x, y, playerShip.name, playerSide ? Color.Yellow : Color.White, Color.Black);
            y++;
            int start = 0;
            int? highlight = null;

            if (playerIndex != null) {
                start = Math.Max(playerIndex.Value - 13, 0);
                if (playerSide) {
                    highlight = playerIndex;
                }
            }
            int end = Math.Min(playerItems.Count, start + 26);

            if (playerItems.Any()) {
                int i = start;
                while (i < end) {
                    var highlightColor = i == highlight ? Color.Yellow : Color.White;
                    var name = new ColoredString($"{UI.indexToLetter(i - start)}. ", playerSide ? highlightColor : new Color(153, 153, 153, 255), Color.Black)
                             + new ColoredString(playerItems.ElementAt(i).type.name, highlightColor, Color.Black);
                    this.Print(x, y, name);
                    i++;
                    y++;
                }


                var height = 26;
                var barStart = (height * (start)) / playerItems.Count;
                var barEnd = (height * (end)) / playerItems.Count;

                for(i = 0; i < height; i++) {
                    if(i < barStart || i > barEnd) {
                        this.SetCellAppearance(x - 1, 16 + i,
                            new ColoredGlyph(Color.LightGray, Color.Black, '|'));
                    } else {
                        this.SetCellAppearance(x - 1, 16 + i,
    new ColoredGlyph(Color.White, Color.Black, '#'));
                    }
                }
            } else {
                var highlightColor = playerSide ? Color.Yellow : Color.White;
                var name = new ColoredString("<Empty>", highlightColor, Color.Black);
                this.Print(x, y, name);
            }
            y = 16 + 26 + 2;
            var f = Color.White;
            var b = Color.Black;
            this.Print(x, y++, $"Money: {$"{player.money}".PadLeft(8)}", f, b);
            ref int? index = ref (playerSide ? ref playerIndex : ref dockedIndex);
            if (index != null) {
                f = Color.Yellow;
                if (playerSide) {
                    var item = playerItems.ElementAt(index.Value);
                    this.Print(x, y++, $"      +{$"{item.type.value}".PadLeft(8)}", f, b);
                } else {
                    var item = dockedItems.ElementAt(index.Value);
                    this.Print(x, y++, $"      -{$"{item.type.value}".PadLeft(8)}", f, b);
                }
            }
            x += 32 + 1;
            y = 16;
            foreach (var point in new Rectangle(x, y, 32, 26).Positions()) {
                this.SetCellAppearance(point.X, point.Y, new ColoredGlyph(Color.Gray, Color.Transparent, '.'));
            }

            this.Print(x, y, docked.name, !playerSide ? Color.Yellow : Color.White, Color.Black);
            y++;
            start = 0;
            highlight = null;
            if (dockedIndex != null) {
                start = Math.Max(dockedIndex.Value - 13, 0);
                
                if(!playerSide) {
                    highlight = dockedIndex;
                }
                
            }

            end = Math.Min(dockedItems.Count, start + 26);
            if (dockedItems.Any()) {
                int i = start;
                while (i < end) {
                    var highlightColor = (i == highlight ? Color.Yellow : Color.White);
                    var name = new ColoredString($"{UI.indexToLetter(i - start)}. ", !playerSide ? highlightColor : new Color(153, 153, 153, 255), Color.Black)
                             + new ColoredString(dockedItems.ElementAt(i).type.name, highlightColor, Color.Black);
                    this.Print(x, y, name);
                    i++;
                    y++;
                }

                var height = 26;
                var barStart = (height * (start)) / dockedItems.Count;
                var barEnd = (height * (end)) / dockedItems.Count;

                for (i = 0; i < height; i++) {
                    if (i < barStart || i > barEnd) {
                        this.SetCellAppearance(x - 1, 16 + i,
                            new ColoredGlyph(Color.LightGray, Color.Black, '|'));
                    } else {
                        this.SetCellAppearance(x - 1, 16 + i,
    new ColoredGlyph(Color.White, Color.Black, '#'));
                    }
                }
            } else {
                var highlightColor = !playerSide ? Color.Yellow : Color.White;
                var name = new ColoredString("<Empty>", highlightColor, Color.Black);
                this.Print(x, y, name);
            }
            base.Render(delta);
        }
    }
}
