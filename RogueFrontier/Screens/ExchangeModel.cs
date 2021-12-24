using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = SadConsole.Console;

namespace RogueFrontier;

public class Dealer {
    public string name;
    public int? index;
    public HashSet<Item> items;
    public List<(Item item, int count)> grouped;

    public Item currentItem => index.HasValue ? items.ElementAt(index.Value) : null;
    public (Item item, int count) currentGroup => index.HasValue ? grouped[index.Value] : default;
    public Dealer(string name, HashSet<Item> items) {
        this.name = name;
        this.items = items;
        this.grouped = items.GroupBy(i => i).Select(g => (g.Key, g.Count())).ToList();
        index = items.Any() ? 0 : null;
    }
}
public class ExchangeModel {
    public int dealerIndex;
    public List<Dealer> dealers;
    public Dealer currentDealer => dealers[dealerIndex];
    public Item currentItem => currentDealer.currentItem;
    public (Item item, int count) currentGroup => currentDealer.currentGroup;
    public Dealer from => dealers[dealerIndex];
    public Dealer to => dealers[(dealerIndex + 1)%dealers.Count];
    public ref int? fromIndex => ref from.index;

    public void NextDealer() => dealerIndex = (dealerIndex + 1) % dealers.Count;

    Action enter;
    Action exit;

    public ExchangeModel(Dealer player, Dealer station, Action enter, Action exit) {
        dealers = new() { player, station };
        this.enter = enter;
        this.exit = exit;
    }
    public void ProcessKeyboard(Keyboard keyboard) {
        var from = this.from.items;
        var to = this.to.items;
        ref int? index = ref fromIndex;

        foreach (var key in keyboard.KeysPressed) {
            switch (key.Key) {
                case Keys.PageUp:
                    index = from.Any() ?
                        (index == null ? (from.Count() - 1) :
                            index == 0 ? null :
                            Math.Max(index.Value - 26, 0))
                        : null;
                    break;
                case Keys.Up:
                    index = from.Any() ?
                        (index == null ?
                            from.Count - 1 :
                            Math.Max(index.Value - 1, 0)) :
                        null;
                    break;
                case Keys.Down:
                    index = from.Any() ?
                        (index == null ?
                            0 :
                            Math.Min(index.Value + 1, from.Count - 1)) :
                        null;
                    break;
                case Keys.PageDown:
                    index = from.Any() ?
                        (index == null ? 0 :
                            index == from.Count() - 1 ? null :
                            Math.Min(index.Value + 26, from.Count() - 1))
                        : null;
                    break;
                case Keys.Left:
                    dealerIndex = 0;
                    UpdateIndex();
                    break;
                case Keys.Right:
                    dealerIndex = 1;
                    UpdateIndex();
                    break;
                case Keys.Enter:
                    if (index == null)
                        break;
                    enter();
                    UpdateIndex();
                    break;
                case Keys.Escape:
                    exit();
                    break;
                default:
                    var ch = char.ToLower(key.Character);
                    if (ch >= 'a' && ch <= 'z') {
                        int start = Math.Max((index ?? 0) - 13, 0);
                        var letterIndex = start + UI.letterToIndex(ch);
                        if (letterIndex < from.Count) {
                            index = letterIndex;
                            enter();
                            UpdateIndex();
                        }
                    }
                    break;
            }
        }
    }
    public void UpdateIndex() {
        var items = from.items;
        ref var index = ref from.index;
        index = items.Any() ? index = Math.Min(index ?? 0, items.Count - 1) : null;
    }

    public void Render(Console con) {
        int x = 16;
        int y = 16;

        con.RenderBackground();

        var player = dealers[0];
        var playerSide = dealerIndex == 0;
        var playerItems = player.items;

        Color c = playerSide ? Color.Yellow : Color.White;
        con.DrawBox(new Rectangle(x - 2, y - 3, 34, 3), new ColoredGlyph(c, Color.Black, '-'));
        con.Print(x, y - 2, dealers[0].name, c, Color.Black);
        int start = 0;
        int? highlight = null;

        var playerIndex = dealers[0].index;
        if (playerIndex.HasValue) {
            start = Math.Max(playerIndex.Value - 13, 0);
            if (playerSide) {
                highlight = playerIndex;
            }
        }
        int end = Math.Min(playerItems.Count, start + 26);


        void line(Point from, Point to, int glyph) {
            con.DrawLine(from, to, '-', Color.White, null);
        }
        if (playerItems.Any()) {
            int i = start;
            while (i < end) {
                var highlightColor = i == highlight ? Color.Yellow : Color.White;
                var name = new ColoredString($"{UI.indexToLetter(i - start)}. ", playerSide ? highlightColor : new Color(153, 153, 153, 255), Color.Black)
                         + new ColoredString(playerItems.ElementAt(i).type.name, highlightColor, Color.Black);
                con.Print(x, y, name);
                i++;
                y++;
            }

            int height = 26;
            int barStart = (height * (start)) / playerItems.Count;
            int barEnd = (height * (end)) / playerItems.Count;
            int barX = x - 2;
            for (i = 0; i < height; i++) {
                ColoredGlyph cg = i < barStart || i > barEnd ?
                    new ColoredGlyph(Color.LightGray, Color.Black, '|') :
                    new ColoredGlyph(Color.White, Color.Black, '#');
                con.SetCellAppearance(barX, 16 + i, cg);
            }
            line(new Point(barX, 16 + 26), new Point(barX + 33, 16 + 26), '-');
            barX += 33;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
        } else {
            var highlightColor = playerSide ? Color.Yellow : Color.White;
            var name = new ColoredString("<Empty>", highlightColor, Color.Black);
            con.Print(x, y, name);

            int barX = x - 2;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
            line(new Point(barX, 16 + 26), new Point(barX + 33, 16 + 26), '-');
            barX += 33;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
        }

        x += 32 + 4;
        y = 16;

        var docked = dealers[1];
        var dockedIndex = docked.index;
        var dockedItems = docked.items;

        c = !playerSide ? Color.Yellow : Color.White;
        con.DrawBox(new Rectangle(x - 2, y - 3, 34, 3), new ColoredGlyph(c, Color.Black, '-'));
        con.Print(x, y - 2, docked.name, c, Color.Black);

        start = 0;
        highlight = null;
        if (dockedIndex.HasValue) {
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
                con.Print(x, y, name);
                i++;
                y++;
            }

            int height = 26;
            int barStart = (height * (start)) / dockedItems.Count;
            int barEnd = (height * (end)) / dockedItems.Count;
            int barX = x - 2;
            for (i = 0; i < height; i++) {
                ColoredGlyph cg = i < barStart || i > barEnd ?
                    new ColoredGlyph(Color.LightGray, Color.Black, '|') :
                    new ColoredGlyph(Color.White, Color.Black, '#');
                con.SetCellAppearance(barX, 16 + i, cg);
            }
            con.DrawLine(new Point(barX, 16 + 26), new Point(barX + 33, 16 + 26), '-', Color.White, null);
            barX += 33;
            con.DrawLine(new Point(barX, 16), new Point(barX, 16 + 25), '|', Color.White, null);
        } else {
            var highlightColor = !playerSide ? Color.Yellow : Color.White;
            var name = new ColoredString("<Empty>", highlightColor, Color.Black);
            con.Print(x, y, name);


            int barX = x - 2;
            con.DrawLine(new Point(barX, 16), new Point(barX, 16 + 25), '|', Color.White, null);
            con.DrawLine(new Point(barX, 16 + 26), new Point(barX + 33, 16 + 26), '-', Color.White, null);
            barX += 33;
            con.DrawLine(new Point(barX, 16), new Point(barX, 16 + 25), '|', Color.White, null);
        }
    }
}
