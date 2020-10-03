using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SadRogue.Primitives;
using System;
using SadConsole;
using static UI;
using Console = SadConsole.Console;
using ArchConsole;

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
    public class HeroImageScene : Console {
        double time;
        string[] heroImage;
        Color tint;
        public HeroImageScene(Console prev, string[] heroImage, Color tint) : base(prev.Width, prev.Height) {
            this.heroImage = heroImage;
            this.tint = tint;
        }
        public override void Update(TimeSpan delta) {
            time += delta.TotalSeconds;
        }
        public override void Render(TimeSpan delta) {
            int width = heroImage.Max(line => line.Length);
            int height = heroImage.Length;
            int x = 8;
            int y = (Height - height * 2) / 2;
            byte GetAlpha(int x, int y) {
                return (byte)(Math.Sin(time * 1.5 + Math.Sin(x) * 5 + Math.Sin(y) * 5) * 25 + 230);
            }
            int lineY = 0;
            foreach (var line in heroImage) {
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
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            return base.ProcessKeyboard(keyboard);
        }
    }
    public class SceneOption {
        public bool escape;
        public bool enter;
        public char key;
        public string name;
        public Func<Console> next;
    }
    public static class SScene {
        public static void ProcessMouseTree(this IScreenObject root, Mouse m) {
            List<IScreenObject> s = new List<IScreenObject>();
            AddChildren(root);
            void AddChildren(IScreenObject parent) {
                s.Add(parent);
                foreach(var c in parent.Children) {
                    AddChildren(c);
                }
            }
            foreach(var c in s) {
                c.ProcessMouse(new MouseScreenObjectState(c, m));
            }
        }
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
        public bool charging;
        public int descIndex;
        public int ticks;
        List<SceneOption> navigation;
        public int navIndex = 0;
        int[] charge;

        bool enter;
        public TextScene(Console prev, string desc, List<SceneOption> navigation) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.desc = desc;
            this.navigation = navigation;
            charge = new int[navigation.Count];
            descIndex = 0;
            ticks = 0;

            UseMouse = true;
            UseKeyboard = true;
        }
        public override void Update(TimeSpan delta) {
            bool f = IsFocused;

            ticks++;
            if(ticks%3 == 0) {
                if (descIndex < desc.Length - 1) {
                    descIndex++;
                } else if(descIndex < desc.Length) {
                    descIndex++;

                    int x = 64;
                    int y = 16 + desc.Count(c => c == '\n') + 3;

                    int i = 0;
                    foreach (var option in navigation) {
                        int index = i;

                        Children.Add(new LabelButton(option.name, () => {
                            navIndex = index;
                            charging = true;
                        }) { Position = new Point(x, y++) });
                        
                        i++;
                    }
                }
            }

            if (charging) {
                ref int c = ref charge[navIndex];
                c++;
                c++;
                charging = false;
            } else if(!enter) {
                ref int c = ref charge[navIndex];
                if (c >= 60) {
                    Transition(navigation[navIndex].next?.Invoke());
                }
            }
            for (int i = 0; i < charge.Length; i++) {
                ref int c = ref charge[i];
                if (c > 0) {
                    c--;
                }
            }

            base.Update(delta);
        }
        public void Transition(Console next) {
            var p = Parent;
            var c = Parent.Children;
            c.Remove(this);
            if (next != null) {
                c.Add(next);
                next.IsFocused = true;
            } else {
                p.IsFocused = true;
            }
        }
        public override void Render(TimeSpan delta) {
            base.Render(delta);
            this.RenderBackground();


            int left = 64;
            int top = 16;
            
            int y = top;
            int x = left;

            for (int i = 0; i < descIndex; i++) {
                switch(desc[i]) {
                    case '\n':
                        x = left;
                        y++;
                        break;
                    default:
                        this.SetCellAppearance(x, y, new ColoredGlyph(Color.LightBlue, Color.Black, desc[i]));
                        x++;
                        break;
                }
            }

            x = 64;
            y = 16 + desc.Count(c => c == '\n') + 3;


            if (descIndex == desc.Length) {
                this.Print(x - 4, y + navIndex, new ColoredString("--->", Color.Yellow, Color.Transparent));

                this.Print(x + navigation[navIndex].name.Length, y + navIndex, new ColoredString(new string('>', 60 / 3), Color.Gray, Color.Transparent));

                for (int i = 0; i < charge.Length; i++) {
                    int c = charge[i];
                    if(c < 60) {
                        this.Print(x + navigation[i].name.Length, y + i, new ColoredString(new string('>', c / 3), Color.White, Color.Transparent));
                    } else {
                        this.Print(x + navigation[i].name.Length, y + i, new ColoredString(new string('>', 20), Color.White, Color.Transparent));
                    }
                }

                if(charge[navIndex] < 60) {
                    this.Print(x + navigation[navIndex].name.Length, y + navIndex, new ColoredString(new string('>', charge[navIndex] / 3), Color.Yellow, Color.Transparent));
                } else {
                    this.Print(x + navigation[navIndex].name.Length, y + navIndex, new ColoredString(new string('>', 20), Color.Red, Color.Transparent));
                }

            }
            
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if(keyboard.IsKeyPressed(Keys.Enter)) {
                if(descIndex < desc.Length - 1) {
                    descIndex = desc.Length - 1;
                }
                enter = true;
            } else if(keyboard.IsKeyDown(Keys.Enter)) {
                if (descIndex == desc.Length) {
                    charging = true;
                }
                enter = true;
            } else {
                enter = false;
            }
            if(keyboard.IsKeyDown(Keys.Up)) {
                navIndex = (navIndex - 1 + navigation.Count) % navigation.Count;
            }
            if(keyboard.IsKeyDown(Keys.Down)) {
                navIndex = (navIndex + 1) % navigation.Count;
            }
            return base.ProcessKeyboard(keyboard);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            foreach(var c in Children) {
                c.ProcessMouse(state);
            }
            return base.ProcessMouse(state);
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
