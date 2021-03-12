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

            var name = PlayerShip.ShipClass.name;
            var x = Width / 4 - name.Length / 2;
            var y = 4;

            void Print(int x, int y, string s) =>
                this.Print(x, y, s, Color.White, Color.Transparent);
            void Print2(int x, int y, string s) =>
                this.Print(x, y, s, Color.White, Color.Black.SetAlpha(102));

            Print(x, y, name);


            var map = PlayerShip.ShipClass.playerSettings?.map ?? new string[] { "" };
            x = Math.Max(0, Width / 4 - map.Select(line => line.Length).Max() / 2);
            y = 2;

            int width = map.Max(l => l.Length);
            foreach (var line in map) {
                var l = line.PadRight(width);
                Print2(x, y++, l);
                Print2(x, y++, l);
            }
            y++;

            Print(x, y, $"{$"Thrust:    {PlayerShip.ShipClass.thrust}", -16}{$"Rotation acceleration: {PlayerShip.ShipClass.rotationAccel, 4} deg/s^2"}");
            y++;
            Print(x, y, $"{$"Max Speed: {PlayerShip.ShipClass.maxSpeed}",-16}{$"Rotation deceleration: {PlayerShip.ShipClass.rotationDecel, 4} deg/s^2"}");
            y++; 
            Print(x, y, $"{"",-16}{$"Rotation max speed:    {PlayerShip.ShipClass.rotationMaxSpeed*30, 4} deg/s^2"}");

            x = Width / 2;
            y = 2;

            var ds = PlayerShip.Ship.DamageSystem;
            if(ds is HPSystem hp) {
                Print(x, y++, "[Health]");
                Print(x, y++, $"HP: {hp.hp}");
            } else if(ds is LayeredArmorSystem las) {
                Print(x, y++, "[Armor]");
                foreach(var a in las.layers) {
                    Print(x, y++, $"{a.source.type.name}: {a.hp} / {a.desc.maxHP}");
                }
            }

            var weapons = PlayerShip.Ship.Devices.Weapons;
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
            if(info.IsKeyPressed(Keys.C)) {
                Parent.Children.Add(new CargoScreen(prev, PlayerShip) {
                    IsFocused = true
                });
                Parent.Children.Remove(this);
            }
            if (info.IsKeyPressed(Keys.D)) {
                Parent.Children.Add(new LoadoutScreen(prev, PlayerShip) {
                    IsFocused = true
                });
                Parent.Children.Remove(this);
            }
            return base.ProcessKeyboard(info);
        }
    }


    public class LoadoutScreen : Console {
        Console prev;
        PlayerShip player;
        List<Device> playerDevices => player.Devices.Installed;
        int? playerIndex;
        public LoadoutScreen(Console prev, PlayerShip player) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.player = player;

            if (player.Items.Any()) {
                playerIndex = 0;
            }
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            foreach (var key in keyboard.KeysPressed) {
                switch (key.Key) {
                    case Keys.Up:
                        if (playerDevices.Any()) {
                            if (playerIndex == null) {
                                playerIndex = playerDevices.Count - 1;
                            } else {
                                playerIndex = Math.Max(playerIndex.Value - 1, 0);
                            }
                        } else {
                            playerIndex = null;
                        }
                        break;
                    case Keys.Down:
                        if (playerDevices.Any()) {
                            if (playerIndex == null) {
                                playerIndex = 0;
                            } else {
                                playerIndex = Math.Min(playerIndex.Value + 1, playerDevices.Count - 1);
                            }
                        } else {
                            playerIndex = null;
                        }
                        break;
                    case Keys.Enter:
                        if (playerIndex != null) {

                            var item = playerDevices.ElementAt(playerIndex.Value).source;

                            var invoke = item.type.invoke;
                            if (invoke == InvokeAction.installWeapon) {
                                player.AddMessage(new InfoMessage($"Removed weapon {item.type.name}"));
                                
                                player.Devices.Remove(item.weapon);
                                item.RemoveWeapon();
                                player.Items.Add(item);

                                if (playerDevices.Any()) {
                                    playerIndex = Math.Min(playerIndex ?? 0, playerDevices.Count - 1);
                                } else {
                                    playerIndex = null;
                                }
                            }

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
                            var letterIndex = letterToIndex(ch);
                            if (letterIndex < playerDevices.Count) {
                                var item = playerDevices.ElementAt(letterIndex);
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
            this.Print(x, y, player.Name, Color.Yellow, Color.Transparent);
            y++;
            int i = 0;
            int? highlight = null;

            if (playerIndex != null) {
                i = Math.Max(playerIndex.Value - 16, 0);
                highlight = playerIndex;
            }

            if (playerDevices.Any()) {
                while (i < playerDevices.Count) {

                    var highlightColor = i == highlight ? Color.Yellow : Color.White;
                    var name = new ColoredString($"{UI.indexToLetter(i)}. ", highlightColor, Color.Transparent)
                             + new ColoredString(playerDevices[i].source.type.name, highlightColor, Color.Transparent);
                    this.Print(x, y, name);

                    i++;
                    y++;
                }
            } else {
                var highlightColor = Color.Yellow;
                var name = new ColoredString("<Empty>", highlightColor, Color.Transparent);
                this.Print(x, y, name);
            }

            x += 32;
            y = 16;
            if (playerIndex != null) {
                var item = playerDevices.ElementAt(playerIndex.Value).source;
                var invoke = item.type.invoke;
                if (invoke == InvokeAction.installWeapon) {
                    this.Print(x, y, "[Enter] Remove this weapon", Color.Yellow, Color.Transparent);
                }
            }

            base.Render(delta);
        }

    }
    public class CargoScreen : Console {
        Console prev;
        PlayerShip player;
        HashSet<Item> playerItems => player.Items;
        int? playerIndex;
        public CargoScreen(Console prev, PlayerShip player) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.player = player;

            if (player.Items.Any()) {
                playerIndex = 0;
            }
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            foreach (var key in keyboard.KeysPressed) {
                switch (key.Key) {
                    case Keys.Up:
                        if (playerItems.Any()) {
                            if (playerIndex == null) {
                                playerIndex = playerItems.Count - 1;
                            } else {
                                playerIndex = Math.Max(playerIndex.Value - 1, 0);
                            }
                        } else {
                            playerIndex = null;
                        }
                        break;
                    case Keys.Down:
                        if (playerItems.Any()) {
                            if (playerIndex == null) {
                                playerIndex = 0;
                            } else {
                                playerIndex = Math.Min(playerIndex.Value + 1, playerItems.Count - 1);
                            }
                        } else {
                            playerIndex = null;
                        }
                        break;
                    case Keys.Enter:
                        if (playerIndex != null) {

                            var item = playerItems.ElementAt(playerIndex.Value);

                            var invoke = item.type.invoke;
                            if (invoke == InvokeAction.installWeapon) {
                                player.Devices.Install(item.InstallWeapon());
                                player.AddMessage(new InfoMessage($"Installed weapon {item.type.name}"));



                                playerItems.Remove(item);
                                if (playerItems.Any()) {
                                    playerIndex = Math.Min(playerIndex ?? 0, playerItems.Count - 1);
                                } else {
                                    playerIndex = null;
                                }
                            }
                            
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
                            var letterIndex = letterToIndex(ch);
                            if (letterIndex < playerItems.Count) {
                                var item = playerItems.ElementAt(letterIndex);
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
            this.Print(x, y, player.Name, Color.Yellow, Color.Transparent);
            y++;
            int i = 0;
            int? highlight = null;

            if (playerIndex != null) {
                i = Math.Max(playerIndex.Value - 16, 0);
                highlight = playerIndex;
            }

            if (playerItems.Any()) {
                while (i < playerItems.Count) {

                    var highlightColor = i == highlight ? Color.Yellow : Color.White;
                    var name = new ColoredString($"{UI.indexToLetter(i)}. ", highlightColor, Color.Transparent)
                             + new ColoredString(playerItems.ElementAt(i).type.name, highlightColor, Color.Transparent);
                    this.Print(x, y, name);

                    i++;
                    y++;
                }
            } else {
                var highlightColor = Color.Yellow;
                var name = new ColoredString("<Empty>", highlightColor, Color.Transparent);
                this.Print(x, y, name);
            }

            x += 32;
            y = 16;
            if (playerIndex != null) {
                var item = playerItems.ElementAt(playerIndex.Value);
                var invoke = item.type.invoke;
                if (invoke == InvokeAction.installWeapon) {
                    this.Print(x, y, "[Enter] Install this weapon", Color.Yellow, Color.Transparent);
                }
            }

            base.Render(delta);
        }

    }
}
