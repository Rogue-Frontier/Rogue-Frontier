
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Console = SadConsole.Console;

namespace TranscendenceRL {
    class DeathScreen : Console {
        World world;
        Player player;
        PlayerShip playerShip;
        Epitaph epitaph;
        public DeathScreen(int width, int height, World world, PlayerShip playerShip, Epitaph epitaph) : base(width, height) {
            this.world = world;
            this.player = playerShip.player;
            this.playerShip = playerShip;
            this.epitaph = epitaph;
        }
        public override void Update(TimeSpan delta) {
            base.Update(delta);
        }
        public override void Draw(TimeSpan delta) {
            var str =
@$"
{player.name}
{player.genome.name}
{epitaph.desc}

Final Devices
{string.Join('\n', playerShip.Devices.Installed.Select(device => device.source.type.name))}

Final Cargo
{string.Join('\n', playerShip.Items.Select(item => item.type.name))}
";
            int y = 2;
            foreach(var line in str.Split('\n')) {
                this.Print(2, y++, line);
            }
            base.Draw(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            return base.ProcessKeyboard(keyboard);
        }
    }
}
