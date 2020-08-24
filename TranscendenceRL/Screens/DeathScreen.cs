
using ArchConsole;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Linq;
using Console = SadConsole.Console;

namespace TranscendenceRL {
    class DeathScreen : Console {
        World world;
        Player player;
        PlayerShip playerShip;
        Epitaph epitaph;
        public DeathScreen(PlayerMain playerMain, World world, PlayerShip playerShip, Epitaph epitaph) : base(playerMain.Width, playerMain.Height) {
            this.world = world;
            this.player = playerShip.player;
            this.playerShip = playerShip;
            this.epitaph = epitaph;

            this.Children.Add(new LabelButton("Resurrect", () => {
                //Restore mortality chances
                playerShip.mortalChances = 3;

                //To do: Restore player HP
                playerShip.Ship.DamageSystem.Restore();

                //Resurrect the player; remove wreck and restore ship + heading
                world.entities.all.Remove(epitaph.wreck);
                playerShip.Ship.Active = true;
                playerShip.AddMessage(new InfoMessage("A vision of disaster flashes before your eyes"));
                world.entities.all.Add(playerShip);
                world.effects.all.Add(new Heading(playerShip));
                SadConsole.Game.Instance.Screen = new TitleSlideOpening(new Pause(playerMain)) { IsFocused = true };
            }) { Position = new Point(Width - 16, Height - 4) });

            this.Children.Add(new LabelButton("Title Screen", () => {
                SadConsole.Game.Instance.Screen = new TitleSlideOpening(new TitleScreen(Width, Height)) { IsFocused = true };
            }) { Position = new Point(Width - 16, Height - 2) });
        }
        public override void Update(TimeSpan delta) {
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            var size = epitaph.deathFrame.GetLength(0);
            int y;
            for (y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    this.SetCellAppearance(Width - x - 2, y + 1, epitaph.deathFrame[x, y]);
                }
            }

            var str =
@$"
{player.name}
{player.Genome.name}
{playerShip.ShipClass.name}
{epitaph.desc}

Final Devices
{string.Join('\n', playerShip.Devices.Installed.Select(device => $"    {device.source.type.name}"))}

Final Cargo
{string.Join('\n', playerShip.Items.Select(item => $"    {item.type.name}"))}

Ships Destroyed
{string.Join('\n', playerShip.ShipsDestroyed.dict.Select(pair => $"    {pair.Key.name, -16}{pair.Value, 4}"))}
".Replace("\r", "");
            y = 2;
            foreach(var line in str.Split('\n')) {
                this.Print(2, y++, line);
            }

            base.Render(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            return base.ProcessKeyboard(keyboard);
        }
    }
}
