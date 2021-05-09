using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SadRogue.Primitives;
using System;
using SadConsole;
using static UI;
using Console = SadConsole.Console;

namespace TranscendenceRL {
    public class WreckScene : Console {
        Console prev;
        PlayerShip playerShip;
        Wreck docked;
        HashSet<Item> playerItems => playerShip.cargo;
        HashSet<Item> dockedItems => docked.cargo;
        bool playerSide;
        int? playerIndex;
        int? dockedIndex;
        public WreckScene(Console prev, PlayerShip player, Wreck docked) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.playerShip = player;
            this.docked = docked;
            this.playerSide = false;

            if(player.cargo.Any()) {
                playerIndex = 0;
            }
            if(this.docked.cargo.Any()) {
                dockedIndex = 0;
            }
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            var from = playerSide ? playerItems : dockedItems;
            var to = playerSide ? dockedItems : playerItems;
            ref int? index = ref (playerSide ? ref playerIndex : ref dockedIndex);
            
            foreach(var key in keyboard.KeysPressed) {
                switch(key.Key) {
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
                        if (from.Any()) {
                            if (index == null) {
                                index = 0;
                            } else {
                                index = Math.Min(index.Value + 1, from.Count - 1);
                            }
                        } else {
                            index = null;
                        }
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
                        if (index != null) {

                            var item = from.ElementAt(index.Value);
                            from.Remove(item);
                            to.Add(item);

                            if (from.Any()) {
                                index = Math.Min(index ?? 0, from.Count - 1);
                            } else {
                                index = null;
                            }
                        }
                        break;
                    case Keys.Escape:
                        Parent.Children.Remove(this);
                        prev.IsFocused = true;
                        break;
                    default:
                        var ch = char.ToLower(key.Character);
                        if(ch >= 'a' && ch <= 'z') {

                            int start = Math.Max(index.Value - 13, 0);
                            var letterIndex = start + letterToIndex(ch);
                            if(letterIndex < from.Count) {
                                var item = from.ElementAt(letterIndex);
                                from.Remove(item);
                                to.Add(item);

                                if(from.Any()) {
                                    index = Math.Min(index.Value, from.Count - 1);
                                } else {
                                    index = null;
                                }
                            }
                        }
                        break;
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
            } else {
                var highlightColor = playerSide ? Color.Yellow : Color.White;
                var name = new ColoredString("<Empty>", highlightColor, Color.Black);
                this.Print(x, y, name);
            }
            x += 32;
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

                if (!playerSide) {
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
            } else {
                var highlightColor = !playerSide ? Color.Yellow : Color.White;
                var name = new ColoredString("<Empty>", highlightColor, Color.Black);
                this.Print(x, y, name);
            }
            base.Render(delta);
        }
    }
}
