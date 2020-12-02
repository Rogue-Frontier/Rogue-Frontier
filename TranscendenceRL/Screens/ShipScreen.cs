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

namespace TranscendenceRL {
    class ShipScreen : Console {
        public Console prev;
        public PlayerShip PlayerShip;
        //Idea: Show an ASCII-art map of the ship where the player can walk around
        public ShipScreen(Console prev, PlayerShip PlayerShip) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.PlayerShip = PlayerShip;
        }
        public override void Render(TimeSpan delta) {
            this.Clear();
            this.RenderBackground();
            var name = PlayerShip.ShipClass.name;
            var x = Width / 4 - name.Length / 2;
            var y = 4;

            Color f = Color.White, b = Color.Black;
            void Print(int x, int y, string s) => this.Print(x, y, s, f, b);

            Print(x, y, name);


            var map = PlayerShip.ShipClass.playerSettings?.map ?? new string[] { "" };
            x = Math.Max(0, Width / 4 - map.Select(line => line.Length).Max() / 2);
            y = 2;
            foreach (var line in map) {
                Print(x, y++, line);
                Print(x, y++, line);
            }
            y++;

            Print(x, y, $"{$"Thrust: {PlayerShip.ShipClass.thrust}", -16}{$"Rotation acceleration: {PlayerShip.ShipClass.rotationAccel} degrees/s^2"}");
            y++;
            Print(x, y, $"{$"Max Speed: {PlayerShip.ShipClass.maxSpeed}",-16}{$"Rotation deceleration: {PlayerShip.ShipClass.rotationDecel} degrees/s^2"}");
            y++;
            Print(x, y, $"{"",-16}{$"Rotation max speed: {PlayerShip.ShipClass.rotationMaxSpeed*30} degrees/s^2"}");

            x = Width / 2;
            y = 0;

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
                    Print(x, y++, $"{$"Damage: {w.desc.damageHP}",-16}{$"Shots per second: {60f / w.desc.fireCooldown}",-16}{ $"Projectile speed: {w.desc.missileSpeed}",-16}");
                    y++;
                }
            }
            /*
            foreach(var item in PlayerShip.ship.Items) {

            }
            */

            base.Render(delta);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(SadConsole.Input.Keys.S) || info.IsKeyPressed(Keys.Escape)) {
                prev.Children.Remove(this);
                prev.IsFocused = true;
            }
            return base.ProcessKeyboard(info);
        }
    }
}
