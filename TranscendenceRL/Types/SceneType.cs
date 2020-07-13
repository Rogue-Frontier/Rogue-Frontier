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
        Dockable docked;
        HashSet<Item> playerItems => player.Items;
        HashSet<Item> dockedItems => docked.Items;
        bool playerSide;
        int? playerIndex;
        int? dockedIndex;
        public WreckScene(SetScene setScene, PlayerShip player, Dockable dock) {
            this.player = player;
            this.docked = dock;
            this.setPane = setScene;
            this.playerSide = false;

            if(player.Items.Any()) {
                playerIndex = 0;
            }
            if(docked.Items.Any()) {
                dockedIndex = 0;
            }
        }
        public void Update() {

        }
        public void Handle(Keyboard keyboard) {
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
                        setPane(null);
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
        }
        public void Draw(ICellSurface w) {
            int x = 16;
            int y = 16;

            foreach (var point in new Rectangle(x, y, 32, 26).Positions()) {
                w.SetCellAppearance(point.X, point.Y, new ColoredGlyph(Color.Gray, Color.Transparent, '.'));
            }

            w.Print(x, y, player.Name, playerSide ? Color.Yellow : Color.White, Color.Black);
            y++;
            int i = 0;
            int? highlight = null;

            if (playerSide && playerIndex != null) {
                i = Math.Max(playerIndex.Value - 16, 0);
                highlight = playerIndex;
            }


            while (i < playerItems.Count) {

                var highlightColor = i == highlight ? Color.Yellow : Color.White;
                var name = new ColoredString($"{UI.indexToLetter(i)}. ", playerSide ? highlightColor : Color.Gray, Color.Transparent)
                         + new ColoredString(playerItems.ElementAt(i).type.name, highlightColor, Color.Transparent);
                w.Print(x, y, name);

                i++;
                y++;
            }

            x += 32;
            y = 16;
            foreach(var point in new Rectangle(x, y, 32, 26).Positions()) {
                w.SetCellAppearance(point.X, point.Y, new ColoredGlyph(Color.Gray, Color.Transparent, '.'));
            }

            w.Print(x, y, docked.Name, !playerSide ? Color.Yellow : Color.White, Color.Black);
            y++;
            i = 0;
            highlight = null;
            if (!playerSide && dockedIndex != null) {
                i = Math.Max(dockedIndex.Value - 16, 0);
                highlight = dockedIndex;
            }
            while (i < dockedItems.Count) {

                var highlightColor = (i == highlight ? Color.Yellow : Color.White);
                var name = new ColoredString($"{UI.indexToLetter(i)}. ", !playerSide ? highlightColor : Color.Gray, Color.Transparent)
                         + new ColoredString(dockedItems.ElementAt(i).type.name, highlightColor, Color.Transparent);
                w.Print(x, y, name);

                i++;
                y++;
            }
        }
    }
}
