using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class ShipScreen : Window {
        public Window prev;
        public PlayerShip PlayerShip;
        //Idea: Show an ASCII-art map of the ship where the player can walk around
        public ShipScreen(Window prev, PlayerShip PlayerShip) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.PlayerShip = PlayerShip;
        }
        public override void Draw(TimeSpan delta) {
            Clear();
            var name = PlayerShip.ShipClass.name;
            var x = Width / 4 - name.Length / 2;
            var y = 0;
            Print(x, y, name);

            var map = PlayerShip.ShipClass.playerSettings.map;
            x = Math.Max(0, Width / 4 - map.Select(line => line.Length).Max() / 2);
            y = 2;
            foreach (var line in map) {
                Print(x, y, line);
                y++;
            }
            y++;

            Print(x, y, $"{$"Thrust: {PlayerShip.ShipClass.thrust}", -16}{$"Rotation acceleration: {PlayerShip.ShipClass.rotationAccel} degrees/s^2"}");
            y++;
            Print(x, y, $"{$"Max Speed: {PlayerShip.ShipClass.maxSpeed}",-16}{$"Rotation deceleration: {PlayerShip.ShipClass.rotationDecel} degrees/s^2"}");
            y++;
            Print(x, y, $"{"",-16}{$"Rotation max speed: {PlayerShip.ShipClass.rotationMaxSpeed*30} degrees/s^2"}");

            x = Width / 2;
            y = 0;

            foreach(var device in PlayerShip.Ship.Devices.Devices) {
                Print(x, y++, device.source.type.name);
                if(device is Weapon w) {
                    Print(x, y++, $"{$"Damage: {w.desc.damageHP}", -16}{$"Shots per second: {60f / w.desc.fireCooldown}",-16}{ $"Projectile speed: {w.desc.missileSpeed}",-16}");
                }
            }
            /*
            foreach(var item in PlayerShip.ship.Items) {

            }
            */

            base.Draw(delta);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.S)) {
                Hide();
                prev.Show(true);
            }
            return base.ProcessKeyboard(info);
        }
    }
}
