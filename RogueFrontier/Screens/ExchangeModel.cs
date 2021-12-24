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

public class Trader {
    public string name;
    public int? index;
    public HashSet<Item> items;
    public HashSet<(Item item, int count)> groups;
    public int count => groupMode ? groups.Count : items.Count;
    public bool groupMode = true;
    public Item currentItem => index.HasValue ? items.ElementAt(index.Value) : null;
    public (Item item, int count) currentGroup => index.HasValue ? groups.ElementAt(index.Value) : default;
    public Trader(string name, HashSet<Item> items) {
        this.name = name;
        this.items = items;
        UpdateIndex();
    }
    public void UpdateGroup() {
        var l = items.ToList();
        groups = items.GroupBy(i => i.type)
            .OrderBy(g => l.IndexOf(g.First()))
            .Select(g => (g.Last(), g.Count()))
            .ToHashSet();
    }
    public void UpdateIndex() {
        if (groupMode) UpdateGroup();
        index = count > 0 ? index = Math.Min(index ?? 0, count - 1) : null;
    }
}
public class ExchangeModel {
    public int traderIndex;
    public List<Trader> traders;
    public Trader currentTrader => traders[traderIndex];
    public Item currentItem => currentTrader.groupMode ? currentGroup.item : currentTrader.currentItem;
    public (Item item, int count) currentGroup => currentTrader.currentGroup;
    public Trader from => traders[traderIndex];
    public Trader to => traders[(traderIndex + 1)%traders.Count];
    public ref int? fromIndex => ref from.index;

    public void NextDealer() => traderIndex = (traderIndex + 1) % traders.Count;

    Action enter;
    Action exit;
    int tick;

    public ExchangeModel(Trader player, Trader station, Action enter, Action exit) {
        traders = new() { player, station };
        this.enter = enter;
        this.exit = exit;
    }
    public void ProcessKeyboard(Keyboard keyboard) {
        var fromCount = from.count;
        ref int? index = ref fromIndex;

        foreach (var key in keyboard.KeysPressed) {
            switch (key.Key) {
                case Keys.PageUp:
                    index = fromCount > 0 ?
                        (index == null ? (fromCount - 1) :
                            index == 0 ? null :
                            Math.Max(index.Value - 26, 0))
                        : null;
                    tick = 0;
                    break;
                case Keys.Up:
                    index = fromCount>0 ?
                        (index == null ?
                            fromCount - 1 :
                            Math.Max(index.Value - 1, 0)) :
                        null;
                    tick = 0;
                    break;
                case Keys.Down:
                    index = fromCount>0 ?
                        (index == null ?
                            0 :
                            Math.Min(index.Value + 1, fromCount - 1)) :
                        null;
                    tick = 0;
                    break;
                case Keys.PageDown:
                    index = fromCount>0 ?
                        (index == null ? 0 :
                            index == fromCount - 1 ? null :
                            Math.Min(index.Value + 26, fromCount - 1))
                        : null;
                    tick = 0;
                    break;
                case Keys.Left:
                    traderIndex = 0;
                    UpdateIndex();
                    break;
                case Keys.Right:
                    traderIndex = 1;
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
                        if(letterIndex == index) {
                            enter();
                            UpdateIndex();
                        } else if (letterIndex < fromCount) {
                            index = letterIndex;
                            tick = 0;
                        }
                    }
                    break;
            }
        }
    }
    public void UpdateIndex() {
        traders.ForEach(d => d.UpdateIndex());
        tick = 0;
    }
    public void Update() {
        tick++;
    }
    public void Render(Console con) {
        int x = 16;
        int y = 16;

        con.RenderBackground();

        var player = traders[0];
        var playerSide = traderIndex == 0;
        var playerCount = player.count;


        Func<int, string> NameAt = player.groupMode ?
            (i => { var g = player.groups.ElementAt(i);
                return $"{g.count}x {g.item.type.name}";
            }) :
            (i => player.items.ElementAt(i).type.name);


        Color c = playerSide ? Color.Yellow : Color.White;
        con.DrawBox(new Rectangle(x - 2, y - 3, 34, 3), new ColoredGlyph(c, Color.Black, '-'));
        con.Print(x, y - 2, traders[0].name, c, Color.Black);
        int start = 0;
        int? highlight = null;

        var playerIndex = traders[0].index;
        if (playerIndex.HasValue) {
            start = Math.Max(playerIndex.Value - 13, 0);
            if (playerSide) {
                highlight = playerIndex;
            }
        }
        int end = Math.Min(playerCount, start + 26);


        void line(Point from, Point to, int glyph) {
            con.DrawLine(from, to, '-', Color.White, null);
        }
        if (playerCount > 0) {
            int i = start;
            while (i < end) {
                var highlightColor = i == highlight ? Color.Yellow : Color.White;
                var n = NameAt(i);
                if (n.Length > 26) {
                    if (i == highlight) {
                        //((tick / 15) % (n.Length - 25));
                        int initialDelay = 60;
                        int index = tick < initialDelay ? 0 : Math.Min((tick - initialDelay) / 15, n.Length - 26);

                        n = n.Substring(index);
                        if (n.Length > 26) {
                            n = $"{n.Substring(0, 23)}...";
                        }
                    } else {
                        n = $"{n.Substring(0, 23)}...";
                    }
                }
                
                var name = new ColoredString($"{UI.indexToLetter(i - start)}. ", playerSide ? highlightColor : new Color(153, 153, 153, 255), Color.Black)
                         + new ColoredString(n, highlightColor, Color.Black);
                con.Print(x, y, name);
                i++;
                y++;
            }

            int height = 26;
            int barStart = (height * (start)) / playerCount;
            int barEnd = (height * (end)) / playerCount;
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

        var docked = traders[1];
        var dockedIndex = docked.index;
        var dockedCount = docked.count;


        NameAt = docked.groupMode ?
            (i => {
                var g = docked.groups.ElementAt(i);
                return $"{g.count}x {g.item.type.name}";
            }) :
            (i => docked.items.ElementAt(i).type.name);

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


        end = Math.Min(dockedCount, start + 26);
        if (dockedCount > 0) {
            int i = start;
            while (i < end) {
                var highlightColor = i == highlight ? Color.Yellow : Color.White;
                var n = NameAt(i);
                if (n.Length > 26) {
                    if (i == highlight) {
                        //((tick / 15) % (n.Length - 25));
                        int initialDelay = 60;
                        int index = tick < initialDelay ? 0 : Math.Min((tick - initialDelay) / 15, n.Length - 26);

                        n = n.Substring(index);
                        if (n.Length > 26) {
                            n = $"{n.Substring(0, 23)}...";
                        }
                    } else {
                        n = $"{n.Substring(0, 23)}...";
                    }
                }

                var name = new ColoredString($"{UI.indexToLetter(i - start)}. ", !playerSide ? highlightColor : new Color(153, 153, 153, 255), Color.Black)
                         + new ColoredString(n, highlightColor, Color.Black);
                con.Print(x, y, name);
                i++;
                y++;
            }

            int height = 26;
            int barStart = (height * (start)) / dockedCount;
            int barEnd = (height * (end)) / dockedCount;
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
