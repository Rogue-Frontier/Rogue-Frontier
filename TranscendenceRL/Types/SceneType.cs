using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SadRogue.Primitives;
using System;
using SadConsole;
using static SadConsole.Input.Keys;
using static UI;
using Console = SadConsole.Console;
using Common;

namespace TranscendenceRL {
    public class SceneType : DesignType {
        public void Initialize(TypeCollection collection, XElement e) {
        }
    }
    public class SceneDesc : ISceneDesc {
        public SceneDesc(XElement e) {

        }

        public Console Get(Console prev, PlayerShip player) {
            //Read thru the desc, generate all the consoles, and return the main one here
            throw new NotImplementedException();
        }
    }
    public interface ISceneDesc {
        Console Get(Console prev, PlayerShip player);
    }
    public class StationScene : Console {
        Console prev;
        PlayerShip player;
        Station station;

        double time;
        public StationScene(Console prev, PlayerShip player, Station station) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.player = player;
            this.station = station;


        }
        public override void Update(TimeSpan delta) {
            time += delta.TotalSeconds;
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            var heroImage = station.StationType.heroImage ?? new string[] { "" };
            int width = heroImage.Max(line => line.Length);
            int height = heroImage.Length;
            int x = 8;
            int y = (Height - height * 2)/2;

            var tint = station.StationType.heroImageTint;
            byte GetAlpha(int x, int y) {
                return (byte)(Math.Sin(time * 1.5 + Math.Sin(x) * 5 + Math.Sin(y) * 5) * 25 + 230);
            }
            int lineY = 0;
            foreach(var line in heroImage) {
                void DrawLine() {
                    for (int lineX = 0; lineX < line.Length; lineX++) {
                        var color = tint.SetAlpha(GetAlpha(lineX, lineY));
                        this.SetCellAppearance(x + lineX, y + lineY, new ColoredGlyph(color, Color.Black, line[lineX]));
                    }
                    lineY++;
                }
                DrawLine();
                DrawLine();
            }

            base.Render(delta);
        }

    }
    public class SceneOption {
        bool escape;
        bool enter;
        char key;
        string name;
        Console next;
    }
    public static class SScene {
        public static void RenderBackground(this Console c) {
            foreach (var point in new Rectangle(0, 0, c.Width, c.Height).Positions()) {

                var h = point.X % 4 == 0;
                var v = point.Y % 4 == 0;

                var f = new Color(255, 255, 255, 255 * 4 / 8);

                if (h && v) {
                    f = new Color(255, 255, 255, 255 * 6 / 8);
                } else if (h || v) {
                    f = new Color(255, 255, 255, 255 * 5 / 8);
                }


                c.SetCellAppearance(point.X, point.Y, new ColoredGlyph(f, Color.Transparent, '.'));
            }
        }
    }
    class TextScene : Console {
        Console prev;
        public string desc;
        public int index;
        public int ticks;
        List<SceneOption> navigation;
        public TextScene(Console prev, string desc, List<SceneOption> navigation) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.desc = desc;
            this.navigation = navigation;
            index = 0;
            ticks = 0;
        }
        public int MeasureWord(int i) {
            int length = 0;
            while(i < desc.Length - 1 && desc[i + 1] != ' ') {
                i++;
                length++;
            }
            return length;
        }
        public override void Update(TimeSpan delta) {
            ticks++;
            if(ticks%3 == 0) {
                if (index < desc.Length) {

                    index++;
                }

            }
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            base.Render(delta);
            this.RenderBackground();


            int left = 16;
            int top = 16;
            int bottom = 24;
            int right = 96;
            this.Fill(new Rectangle(left, top, right - left + 1, bottom - top + 1), new Color(25, 0, 51, 255), Color.Transparent, '=');

            int y = top;
            int x = left;
            for (int i = 0; i < index; i++) {
                int wordLength = MeasureWord(i);

                if(x + wordLength < right - 4) {
                    x++;
                } else {
                    x = left + 4;
                    y++;
                }
                this.SetCellAppearance(x, y, new ColoredGlyph(Color.LightBlue, Color.Black, desc[i]));
            }
        }
    }
    class WreckScene : Console {
        Console prev;
        PlayerShip player;
        Wreck docked;
        HashSet<Item> playerItems => player.Items;
        HashSet<Item> dockedItems => docked.Items;
        bool playerSide;
        int? playerIndex;
        int? dockedIndex;
        public WreckScene(Console prev, PlayerShip player, Wreck docked) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.player = player;
            this.docked = docked;
            this.playerSide = false;

            if(player.Items.Any()) {
                playerIndex = 0;
            }
            if(this.docked.Items.Any()) {
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
                            var letterIndex = letterToIndex(ch);
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
            this.Print(x, y, player.Name, playerSide ? Color.Yellow : Color.White, Color.Black);
            y++;
            int i = 0;
            int? highlight = null;

            if (playerSide && playerIndex != null) {
                i = Math.Max(playerIndex.Value - 16, 0);
                highlight = playerIndex;
            }

            if(playerItems.Any()) {
                while (i < playerItems.Count) {

                    var highlightColor = i == highlight ? Color.Yellow : Color.White;
                    var name = new ColoredString($"{UI.indexToLetter(i)}. ", playerSide ? highlightColor : new Color(153, 153, 153, 255), Color.Transparent)
                             + new ColoredString(playerItems.ElementAt(i).type.name, highlightColor, Color.Transparent);
                    this.Print(x, y, name);

                    i++;
                    y++;
                }
            } else {
                var highlightColor = playerSide ? Color.Yellow : Color.White;
                var name = new ColoredString("<Empty>", highlightColor, Color.Transparent);
                this.Print(x, y, name);
            }
            

            x += 32;
            y = 16;
            foreach(var point in new Rectangle(x, y, 32, 26).Positions()) {
                this.SetCellAppearance(point.X, point.Y, new ColoredGlyph(Color.Gray, Color.Transparent, '.'));
            }

            this.Print(x, y, docked.Name, !playerSide ? Color.Yellow : Color.White, Color.Black);
            y++;
            i = 0;
            highlight = null;
            if (!playerSide && dockedIndex != null) {
                i = Math.Max(dockedIndex.Value - 16, 0);
                highlight = dockedIndex;
            }
            if (dockedItems.Any()) {

                while (i < dockedItems.Count) {

                    var highlightColor = (i == highlight ? Color.Yellow : Color.White);
                    var name = new ColoredString($"{UI.indexToLetter(i)}. ", !playerSide ? highlightColor : new Color(153, 153, 153, 255), Color.Transparent)
                             + new ColoredString(dockedItems.ElementAt(i).type.name, highlightColor, Color.Transparent);
                    this.Print(x, y, name);

                    i++;
                    y++;
                }
            } else {
                var highlightColor = !playerSide ? Color.Yellow : Color.White;
                var name = new ColoredString("<Empty>", highlightColor, Color.Transparent);
                this.Print(x, y, name);
            }
            base.Render(delta);
        }
        
    }
}
