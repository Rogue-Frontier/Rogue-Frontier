using SadRogue.Primitives;
using SadConsole;
using SadConsole.Input;
using SadConsole.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = SadConsole.Console;

using static UI;
using Common;

namespace TranscendenceRL {
    class ShipScreen : Console {
        public Console prev;
        public Console back;
        public PlayerShip PlayerShip;
        //Idea: Show an ASCII-art map of the ship where the player can walk around
        public ShipScreen(Console prev, PlayerShip PlayerShip) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.back = new Console(prev.Width, prev.Height);
            back.RenderBackground();


            this.PlayerShip = PlayerShip;
        }
        public override void Render(TimeSpan delta) {

            //back.Render(delta);

            this.Clear();
            this.RenderBackground();

            var name = PlayerShip.shipClass.name;
            var x = Width / 4 - name.Length / 2;
            var y = 4;

            void Print(int x, int y, string s) =>
                this.Print(x, y, s, Color.White, Color.Transparent);
            void Print2(int x, int y, string s) =>
                this.Print(x, y, s, Color.White, Color.Black.SetAlpha(102));

            Print(x, y, name);


            var map = PlayerShip.shipClass.playerSettings?.map ?? new string[] { "" };
            x = Math.Max(0, Width / 4 - map.Select(line => line.Length).Max() / 2);
            y = 2;

            int width = map.Max(l => l.Length);
            foreach (var line in map) {
                var l = line.PadRight(width);
                Print2(x, y++, l);
                Print2(x, y++, l);
            }
            y++;

            Print(x, y, $"{$"Thrust:    {PlayerShip.shipClass.thrust}", -16}{$"Rotation acceleration: {PlayerShip.shipClass.rotationAccel, 4} deg/s^2"}");
            y++;
            Print(x, y, $"{$"Max Speed: {PlayerShip.shipClass.maxSpeed}",-16}{$"Rotation deceleration: {PlayerShip.shipClass.rotationDecel, 4} deg/s^2"}");
            y++; 
            Print(x, y, $"{"",-16}{$"Rotation max speed:    {PlayerShip.shipClass.rotationMaxSpeed*30, 4} deg/s^2"}");

            x = Width / 2;
            y = 2;

            var ds = PlayerShip.ship.damageSystem;
            if(ds is HPSystem hp) {
                Print(x, y++, "[Health]");
                Print(x, y++, $"HP: {hp.hp}");
            } else if(ds is LayeredArmorSystem las) {
                Print(x, y++, "[Armor]");
                foreach(var a in las.layers) {
                    Print(x, y++, $"{a.source.type.name}: {a.hp} / {a.desc.maxHP}");
                }
            }

            var weapons = PlayerShip.ship.devices.Weapons;
            if (weapons.Any()) {
                Print(x, y++, "[Weapons]");
                foreach (var w in weapons) {
                    Print(x, y++, $"{w.source.type.name, -32}{w.GetBar()}");
                    Print(x, y++, $"Damage per shot:  {w.desc.damageHP}");
                    Print(x, y++, $"Projectile speed: {w.desc.missileSpeed}");
                    Print(x, y++, $"Shots per second: {60f / w.desc.fireCooldown}");

                    y++;
                }
            }


            x = 1;
            y = Height - 10;
            this.Print(x, y++, "[C] Cargo", Color.White, Color.Black);
            this.Print(x, y++, "[D] Devices", Color.White, Color.Black);
            this.Print(x, y++, "[U] Usables", Color.White, Color.Black);


            /*
            foreach(var item in PlayerShip.ship.Items) {

            }
            */

            base.Render(delta);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(SadConsole.Input.Keys.S) || info.IsKeyPressed(Keys.Escape)) {
                Parent.Children.Remove(this);
                prev.IsFocused = true;
            }
            if(info.IsKeyPressed(Keys.U)) {
                var s = SListScreen.UsableScreen(prev, PlayerShip);
                s.IsFocused = true;
                Parent.Children.Add(s);
                Parent.Children.Remove(this);
            }
            if(info.IsKeyPressed(Keys.C)) {
                var s = SListScreen.CargoScreen(prev, PlayerShip);
                s.IsFocused = true;
                Parent.Children.Add(s);
                Parent.Children.Remove(this);
            }
            if (info.IsKeyPressed(Keys.D)) {
                var s = SListScreen.LoadoutScreen(prev, PlayerShip);
                s.IsFocused = true;
                Parent.Children.Add(s);
                Parent.Children.Remove(this);
            }
            return base.ProcessKeyboard(info);
        }
    }
    public class SListScreen {
        public static ListScreen<Item> UsableScreen(Console prev, PlayerShip player) {
            ListScreen<Item> screen = null;
            IEnumerable<Item> cargoUsable;
            IEnumerable<Item> installedUsable;
            List<Item> usable;
            void UpdateList() {
                cargoUsable = player.cargo.Where(i => i.type.invoke != null);
                installedUsable = player.devices.Installed.Select(d => d.source).Where(i => i.type.invoke != null);
                usable = new List<Item>(installedUsable.Concat(cargoUsable));
            }
            UpdateList();

            return screen = new ListScreen<Item>(prev,
                player,
                usable,
                GetName,
                GetDesc,
                InvokeItem
                );

            string GetName(Item i) => $"{(installedUsable.Contains(i) ? "[Installed] " : "[Cargo]     ")}{i.type.name}";
            List<ColoredString> GetDesc(Item item) {
                var invoke = item.type.invoke;

                List<ColoredString> result = new List<ColoredString>();

                var itemDesc = item.type.desc.SplitLine(32);
                if (itemDesc.Any()) {
                    foreach (var line in itemDesc) {
                        result.Add(new ColoredString(line,
                            Color.White, Color.Transparent));
                    }
                    result.Add(new ColoredString(""));
                }

                if (invoke != null) {
                    string action = $"[Enter] {invoke.GetDesc(player, item)}";
                    result.Add(new ColoredString(action, Color.Yellow, Color.Transparent));
                }
                return result;
            }
            void InvokeItem(Item item) {
                item.type.invoke?.Invoke(screen, player, item, Update);
                screen.UpdateIndex();
            }
            void Update() {
                UpdateList();
                screen.items = usable;
                screen.UpdateIndex();
            }
        }
        public static ListScreen<Device> LoadoutScreen(Console prev, PlayerShip player) {
            ListScreen<Device> screen = null;
            var devices = player.devices.Installed;
            return screen = new ListScreen<Device>(prev,
                player,
                devices,
                GetName,
                GetDesc,
                InvokeDevice
                );

            string GetName(Device d) => d.source.type.name;
            List<ColoredString> GetDesc(Device d) {
                var item = d.source;
                var invoke = item.type.invoke;

                List<ColoredString> result = new List<ColoredString>();

                var itemDesc = item.type.desc.SplitLine(32);
                if (itemDesc.Any()) {
                    foreach (var line in itemDesc) {
                        result.Add(new ColoredString(line,
                            Color.White, Color.Transparent));
                    }
                    result.Add(new ColoredString(""));
                }

                if (invoke != null) {
                    result.Add(new ColoredString($"[Enter] {invoke.GetDesc(player, item)}", Color.Yellow, Color.Transparent));
                }
                return result;
            }
            void InvokeDevice(Device device) {
                var item = device.source;
                var invoke = item.type.invoke;

                invoke?.Invoke(screen, player, item);
                screen.UpdateIndex();
            }
        }
        public static ListScreen<Item> CargoScreen(Console prev, PlayerShip player) {
            ListScreen<Item> screen = null;
            var items = player.cargo;
            return screen = new ListScreen<Item>(prev,
                player,
                items,
                GetName,
                GetDesc,
                InvokeItem
                );

            string GetName(Item i) => i.type.name;
            List<ColoredString> GetDesc(Item item) {
                var invoke = item.type.invoke;

                List<ColoredString> result = new List<ColoredString>();

                var itemDesc = item.type.desc.SplitLine(32);
                if (itemDesc.Any()) {
                    foreach (var line in itemDesc) {
                        result.Add(new ColoredString(line,
                            Color.White, Color.Transparent));
                    }
                    result.Add(new ColoredString(""));
                }

                if (invoke != null) {
                    result.Add(new ColoredString($"[Enter] {invoke.GetDesc(player, item)}", Color.Yellow, Color.Transparent));
                }
                return result;
            }
            void InvokeItem(Item item) {
                var invoke = item.type.invoke;

                invoke?.Invoke(screen, player, item);
                screen.UpdateIndex();
            }
        }
        public static ListScreen<Armor> RepairArmorScreen(Console prev, PlayerShip player, Item source, RepairArmor repair, Action callback) {
            ListScreen<Armor> screen = null;
            var devices = (player.hull as LayeredArmorSystem).layers;
            return screen = new ListScreen<Armor>(prev,
                player,
                devices,
                GetName,
                GetDesc,
                Repair
                ) { IsFocused = true };

            string GetName(Armor d) => $"[{d.hp} / {d.desc.maxHP}]{d.source.type.name}";
            List<ColoredString> GetDesc(Armor d) {
                var item = d.source;
                var invoke = item.type.invoke;

                List<ColoredString> result = new List<ColoredString>();

                var itemDesc = item.type.desc.SplitLine(32);
                if (itemDesc.Any()) {
                    foreach (var line in itemDesc) {
                        result.Add(new ColoredString(line,
                            Color.White, Color.Transparent));
                    }
                    result.Add(new ColoredString(""));
                }

                result.Add(new ColoredString("[Enter] Repair this armor", Color.Yellow, Color.Transparent));
                return result;
            }
            void Repair(Armor segment) {
                segment.hp += repair.repairHP;
                player.cargo.Remove(source);
                player.AddMessage(new InfoMessage($"Used {source.type.name} to restore {repair.repairHP} hp on {segment.source.type.name}"));

                callback?.Invoke();

                var p = screen.Parent;
                p.Children.Remove(screen);
                p.Children.Add(prev);
                p.IsFocused = true;
            }
        }
    }
    public class ListScreen<T> : Console {
        Console prev;
        PlayerShip player;
        public IEnumerable<T> items;
        int? playerIndex;
        GetName getName;
        GetDesc getDesc;
        Invoke invoke;
        public delegate string GetName(T t);
        public delegate List<ColoredString> GetDesc(T t);
        public delegate void Invoke(T t);
        public ListScreen(Console prev, PlayerShip player, IEnumerable<T> items, GetName getName, GetDesc getDesc, Invoke invoke) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.player = player;
            this.items = items;
            this.getName = getName;
            this.getDesc = getDesc;
            this.invoke = invoke;
        }
        public void UpdateIndex() {
            if (items.Any()) {
                playerIndex = Math.Min(playerIndex ?? 0, items.Count() - 1);
            } else {
                playerIndex = null;
            }
        }

        public override bool ProcessKeyboard(Keyboard keyboard) {
            foreach (var key in keyboard.KeysPressed) {
                switch (key.Key) {
                    case Keys.Up:
                        if (items.Any()) {
                            if (playerIndex == null) {
                                playerIndex = items.Count() - 1;
                            } else {
                                playerIndex = Math.Max(playerIndex.Value - 1, 0);
                            }
                        } else {
                            playerIndex = null;
                        }
                        break;
                    case Keys.Down:
                        if (items.Any()) {
                            if (playerIndex == null) {
                                playerIndex = 0;
                            } else {
                                playerIndex = Math.Min(playerIndex.Value + 1, items.Count() - 1);
                            }
                        } else {
                            playerIndex = null;
                        }
                        break;
                    case Keys.Enter:
                        if (playerIndex != null) {
                            var item = items.ElementAt(playerIndex.Value);
                            invoke(item);
                        }
                        break;
                    case Keys.Escape:
                        var parent = Parent;
                        parent.Children.Remove(this);
                        prev.IsFocused = true;
                        break;
                    default:
                        var ch = char.ToLower(key.Character);
                        if (ch >= 'a' && ch <= 'z') {
                            int start = Math.Max(playerIndex.Value - 13, 0);
                            var letterIndex = start + letterToIndex(ch);
                            if (letterIndex < items.Count()) {
                                //var item = items.ElementAt(letterIndex);
                                playerIndex = letterIndex;
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
            this.Print(x, y, player.name, Color.Yellow, Color.Transparent);
            y++;
            int start = 0;
            int? highlight = null;
            if (playerIndex != null) {
                start = Math.Max(playerIndex.Value - 16, 0);
                highlight = playerIndex;
            }
            int end = Math.Min(items.Count(), start + 26);
            if (items.Any()) {
                int i = start;
                while (i < end) {
                    var highlightColor = i == highlight ? Color.Yellow : Color.White;
                    var name = new ColoredString($"{UI.indexToLetter(i - start)}. ", highlightColor, Color.Transparent)
                             + new ColoredString(getName(items.ElementAt(i)), highlightColor, Color.Transparent);
                    this.Print(x, y, name);

                    i++;
                    y++;
                }
            } else {
                var highlightColor = Color.Yellow;
                var name = new ColoredString("<Empty>", highlightColor, Color.Transparent);
                this.Print(x, y, name);
            }

            y = Height - 16;
            foreach (var m in player.messages) {
                this.Print(x, y++, m.Draw());
            }

            x += 32;
            y = 16;
            if (playerIndex != null) {
                var item = items.ElementAt(playerIndex.Value);
                
                var desc = getDesc(item);
                if (desc.Any()) {
                    foreach (var line in desc) {
                        this.Print(x, y++, line);
                    }
                    y++;
                }
            }

            base.Render(delta);
        }

    }
}
