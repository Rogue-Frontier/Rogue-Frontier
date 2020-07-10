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
        public IScene Get(SetScene setScene, PlayerShip player, Dockable dock) {
            throw new NotImplementedException();
        }
    }
    public class LocalScene : ISceneDesc {
        public IScene Get(SetScene setScene, PlayerShip player, Dockable dock) {
            throw new NotImplementedException();
        }
    }
    public delegate void SetScene(IScene next);
    public class SceneOption {
        bool escape;
        bool enter;
        char key;
        string name;
        ISceneDesc next;
    }
    public interface ISceneDesc {
        IScene Get(SetScene setScene, PlayerShip player, Dockable dock);
    }
    public class WreckSceneDesc : ISceneDesc {
        public IScene Get(SetScene setScene, PlayerShip player, Dockable dock) => new WreckScene(setScene, player, dock);
    }
    public class TextSceneDesc : ISceneDesc {
        public string description;
        public List<SceneOption> navigation;

        public TextSceneDesc(string description, List<SceneOption> navigation) {
            this.description = description;
            this.navigation = navigation;
        }
        public IScene Get(SetScene setScene, PlayerShip player, Dockable dock) {
            return new TextScene(setScene, description, navigation);
        }
    }
    public interface IScene {
        void Update();
        void Handle(Keyboard info);
        void Draw(ICellSurface w);
    }
    class TextScene : IScene {
        SetScene setScene;
        public string desc;
        List<SceneOption> navigation;
        public TextScene(SetScene setScene, string desc, List<SceneOption> navigation) {
            this.setScene = setScene;
            this.desc = desc;
            this.navigation = navigation;
        }
        public void Update() {

        }
        public void Handle(Keyboard info) {

        }
        public void Draw(ICellSurface w) {

        }
    }
    class WreckScene : IScene {
        SetScene setPane;
        PlayerShip player;
        Dockable dock;
        HashSet<Item> playerItems => player.Items;
        HashSet<Item> dockItems => dock.Items;
        bool playerSide;
        int? index;
        public WreckScene(SetScene setScene, PlayerShip player, Dockable dock) {
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
