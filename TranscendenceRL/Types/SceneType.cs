using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SadRogue.Primitives;
using System;
using SadConsole;
using static SadConsole.Input.Keys;
namespace TranscendenceRL {
    public class SceneType : DesignType, ISceneDesc {
        public void Initialize(TypeCollection collection, XElement e) {
        }
        public IScene Get(Action<IScene> setPane, PlayerShip player, Dockable dock) {
            throw new NotImplementedException();
        }
    }
    public class LocalScene : ISceneDesc {
        public IScene Get(Action<IScene> setPane, PlayerShip player, Dockable dock) {
            throw new NotImplementedException();
        }
    }
    public class SceneOption {
        bool escape;
        bool enter;
        char key;
        string name;
        ISceneDesc next;
    }
    public interface ISceneDesc {
        IScene Get(Action<IScene> setPane, PlayerShip player, Dockable dock);
    }
    public class TextSceneDesc : ISceneDesc {
        public string description;
        public Dictionary<string, string> navigation;
        public Dictionary<string, ISceneDesc> map;

        public TextSceneDesc(string description, Dictionary<string, string> navigation, Dictionary<string, ISceneDesc> map) {
            this.description = description;
            this.navigation = navigation;
            this.map = map;
        }
        public IScene Get(Action<IScene> setScene, PlayerShip player, Dockable dock) {
            return new TextScene(setScene, description, navigation.ToDictionary(pair => pair.Key, pair => map[pair.Value].Get(setScene, player, dock)));
        }
    }
    public class ExchangeSceneDesc : ISceneDesc {
        public ExchangeSceneDesc() {

        }
        public IScene Get(Action<IScene> setScene, PlayerShip player, Dockable dock) {
            return new ExchangeView(setScene, player, dock);
        }
    }
    public interface IScene {
        void Update();
        void Handle(Keyboard info);
        void Draw(ICellSurface w);
    }
    public class TextSceneIntro : IScene {
        //First we introduce the hero image in the center of the screen, line by line starting from the top
        //Then we introduce the area for the message box
        Action<IScene> setScene;
        public ColoredGlyph[,] heroImage;
        public TextSceneIntro(Action<IScene> setScene, string desc, Dictionary<string, IScene> navigation) {
            this.setScene = setScene;
        }
        public void Update() {

        }
        public void Handle(Keyboard info) {

        }
        public void Draw(ICellSurface w) {

        }
    }
    class TextScene : IScene {
        public Action<IScene> setScene;
        public ColoredGlyph[,] heroImage;
        public string desc;
        Dictionary<string, IScene> next;
        public TextScene(Action<IScene> setScene, string desc, Dictionary<string, IScene> navigation) {
            this.setScene = setScene;
            this.desc = desc;
        }
        public void Update() {

        }
        public void Handle(Keyboard info) {

        }
        public void Draw(ICellSurface w) {

        }
    }
    class ExchangeView : IScene {
        public Action<IScene> setPane;
        PlayerShip player;
        Dockable dock;
        HashSet<Item> playerItems => player.Items;
        HashSet<Item> dockItems => dock.Items;
        bool playerSide;
        int? index;
        public ExchangeView(Action<IScene> setScene, PlayerShip player, Dockable dock) {
            this.player = player;
            this.dock = dock;
            this.setPane = setScene;
            this.playerSide = false;

            var items = (playerSide ? this.playerItems : this.dockItems);
            if (items.Any()) {
                index = 0;
            }
        }
        public void Update() {

        }
        public void Handle(Keyboard keyboard) {
            var from = playerSide ? playerItems : dockItems;

            if (keyboard.IsKeyPressed(Up)) {
                if (index == null && from.Any()) {
                    index = from.Count - 1;
                } else if (index != null) {
                    index = Math.Min(index.Value, from.Count - 1);
                }
            }
            if (keyboard.IsKeyPressed(Down)) {
                if (index == null && from.Any()) {
                    index = 0;
                } else if (index != null) {
                    index = Math.Min(index.Value, from.Count - 1);
                }
            }
            if (keyboard.IsKeyPressed(Left) || keyboard.IsKeyPressed(Right)) {
                playerSide = !playerSide;
                from = playerSide ? playerItems : dockItems;
                if (from.Any()) {
                    index = Math.Min(index ?? 0, from.Count - 1);
                } else {
                    index = null;
                }
            }
            if (keyboard.IsKeyPressed(Enter) && index != null) {
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
            if (keyboard.IsKeyPressed(Escape)) {
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
            if (playerSide && index != null) {
                i = Math.Max(index.Value - 16, 0);
                highlight = index;
            }
            while (i < playerItems.Count) {
                if (i == highlight) {
                    w.Print(x, y, playerItems.ElementAt(i).type.name, Color.Yellow, Color.Black);
                } else {
                    w.Print(x, y, playerItems.ElementAt(i).type.name, Color.White, Color.Black);
                }

                i++;
                y++;
            }

            x = w.ViewWidth / 2 + 16;
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
