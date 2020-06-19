using static SadConsole.Input.Keys;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SadRogue.Primitives;
using System.Xml.Linq;

namespace TranscendenceRL {
    public class DockScreenDesc : IDockViewDesc {
        IDockViewDesc main;
        public static DockScreenDesc WreckScreen = new DockScreenDesc() {
            main = new ExchangeViewDesc()
        };

        private DockScreenDesc() {

        }
        public DockScreenDesc(XElement e) {

        }
        public IDockView Get(Action<IDockView> setPane, PlayerShip player, Dockable dock) {
            return main.Get(setPane, player, dock);
        }
    }
    public interface IDockViewDesc {
        IDockView Get(Action<IDockView> setPane, PlayerShip player, Dockable dock);
    }
    public class TextViewDesc : IDockViewDesc {
        public string description;
        public Dictionary<string, string> navigation;
        public Dictionary<string, IDockViewDesc> map;
        public TextViewDesc(string description, Dictionary<string, string> navigation, Dictionary<string, IDockViewDesc> map) {
            this.description = description;
            this.navigation = navigation;
            this.map = map;
        }
        public IDockView Get(Action<IDockView> setPane, PlayerShip player, Dockable dock) {
            return new TextView(setPane, description, navigation.ToDictionary(pair => pair.Key, pair => map[pair.Value].Get(setPane, player, dock)));
        }
    }
    public class ExchangeViewDesc : IDockViewDesc {
        public ExchangeViewDesc() {

        }
        public IDockView Get(Action<IDockView> setPane, PlayerShip player, Dockable dock) {
            return new ExchangeView(setPane, player, dock);
        }
    }
    public interface IDockView {
        void Update();
        void Handle(Keyboard info);
        void Draw(ICellSurface w);
    }
    class TextView : IDockView {
        public Action<IDockView> setPane;
        public string desc;
        Dictionary<string, IDockView> next;
        public TextView(Action<IDockView> setPane, string desc, Dictionary<string, IDockView> navigation) {
            this.setPane = setPane;
            this.desc = desc;
        }
        public void Update() {

        }
        public void Handle(Keyboard info) {

        }
        public void Draw(ICellSurface w) {

        }
    }
    class ExchangeView : IDockView {
        public Action<IDockView> setPane;
        PlayerShip player;
        Dockable dock;
        HashSet<Item> playerItems => player.Items;
        HashSet<Item> dockItems => dock.Items;
        bool playerSide;
        int? index;
        public ExchangeView(Action<IDockView> setPane, PlayerShip player, Dockable dock) {
            this.player = player;
            this.dock = dock;
            this.setPane = setPane;
            this.playerSide = false;

            var items = (playerSide ? this.playerItems : this.dockItems);
            if(items.Any()) {
                index = 0;
            }
        }
        public void Update() {

        }
        public void Handle(Keyboard keyboard) {
            var from = playerSide ? playerItems : dockItems;

            if(keyboard.IsKeyPressed(Up)) {
                if(index == null && from.Any()) {
                    index = from.Count - 1;
                } else if(index != null) {
                    index = Math.Min(index.Value, from.Count - 1);
                }
            }
            if(keyboard.IsKeyPressed(Down)) {
                if (index == null && from.Any()) {
                    index = 0;
                } else if (index != null) {
                    index = Math.Min(index.Value, from.Count - 1);
                }
            }
            if(keyboard.IsKeyPressed(Left) || keyboard.IsKeyPressed(Right)) {
                playerSide = !playerSide;
                from = playerSide ? playerItems : dockItems;
                if(from.Any()) {
                    index = Math.Min(index ?? 0, from.Count - 1);
                } else {
                    index = null;
                }
            }
            if(keyboard.IsKeyPressed(Enter) && index != null) {
                var to = playerSide ? dockItems : playerItems;

                var item = from.ElementAt(index.Value);
                from.Remove(item);
                to.Add(item);

                if (from.Any()) {
                    index = Math.Min(index ?? 0, from.Count - 1);
                } else {
                    index = null;
                }
            }
            if(keyboard.IsKeyPressed(Escape)) {
                setPane(null);
            }
        }
        public void Draw(ICellSurface w) {
            int x = 16;
            int y = 16;
            int entries = w.ViewHeight - 32;

            w.Print(x, y, player.Name, Color.White, Color.Black);
            y++;
            int i;
            int? highlight;

            i = 0;
            highlight = null;
            if(playerSide && index != null) {
                i = Math.Max(index.Value - 16, 0);
                highlight = index;
            }
            while (i < playerItems.Count) {
                if(i == highlight) {
                    w.Print(x, y, playerItems.ElementAt(i).type.name, Color.Yellow, Color.Black);
                } else {
                    w.Print(x, y, playerItems.ElementAt(i).type.name, Color.White, Color.Black);
                }

                i++;
                y++;
            }

            x = w.ViewWidth/2 + 16;
            y = 16;
            w.Print(x, y, dock.Name, Color.White, Color.Black);
            y++;
            i = 0;
            highlight = null;
            if (!playerSide && index != null) {
                i = Math.Max(index.Value - 16, 0);
                highlight = index;
            }
            while (i < dockItems.Count) {
                if (i == highlight) {
                    w.Print(x, y, dockItems.ElementAt(i).type.name, Color.Yellow, Color.Black);
                } else {
                    w.Print(x, y, dockItems.ElementAt(i).type.name, Color.White, Color.Black);
                }

                i++;
                y++;
            }
        }
    }
}
