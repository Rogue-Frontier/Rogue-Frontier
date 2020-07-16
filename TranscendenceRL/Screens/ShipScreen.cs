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
        public override void Draw(TimeSpan delta) {
            this.Clear();
            var name = PlayerShip.ShipClass.name;
            var x = Width / 4 - name.Length / 2;
            var y = 0;
            this.Print(x, y, name);

            var map = PlayerShip.ShipClass.playerSettings?.map ?? new string[0];
            x = Math.Max(0, Width / 4 - map.Select(line => line.Length).Max() / 2);
            y = 2;
            foreach (var line in map) {
                this.Print(x, y++, line);
                this.Print(x, y++, line);
            }
            y++;

            this.Print(x, y, $"{$"Thrust: {PlayerShip.ShipClass.thrust}", -16}{$"Rotation acceleration: {PlayerShip.ShipClass.rotationAccel} degrees/s^2"}");
            y++;
            this.Print(x, y, $"{$"Max Speed: {PlayerShip.ShipClass.maxSpeed}",-16}{$"Rotation deceleration: {PlayerShip.ShipClass.rotationDecel} degrees/s^2"}");
            y++;
            this.Print(x, y, $"{"",-16}{$"Rotation max speed: {PlayerShip.ShipClass.rotationMaxSpeed*30} degrees/s^2"}");

            x = Width / 2;
            y = 0;

            foreach(var device in PlayerShip.Ship.Devices.Installed) {
                this.Print(x, y++, device.source.type.name);
                if(device is Weapon w) {
                    this.Print(x, y++, $"{$"Damage: {w.desc.damageHP}", -16}{$"Shots per second: {60f / w.desc.fireCooldown}",-16}{ $"Projectile speed: {w.desc.missileSpeed}",-16}");
                }
            }
            /*
            foreach(var item in PlayerShip.ship.Items) {

            }
            */

            base.Draw(delta);
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
