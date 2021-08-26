
using ArchConsole;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Linq;
using Console = SadConsole.Console;

namespace TranscendenceRL {
    class DeathScreen : Console {
        PlayerMain playerMain;
        Epitaph epitaph;
        public DeathScreen(PlayerMain playerMain, Epitaph epitaph) : base(playerMain.Width, playerMain.Height) {
            this.playerMain = playerMain;
            this.epitaph = epitaph;

            this.Children.Add(new LabelButton("Resurrect", Resurrect) {
                Position = new Point(1, Height/2 - 4), FontSize = playerMain.FontSize * 2 });

            this.Children.Add(new LabelButton("Title Screen", Exit) { Position = new Point(1, Height/2 - 2), FontSize = playerMain.FontSize * 2 });
        }
        public void Resurrect() {
            var playerShip = playerMain.playerShip;
            var world = playerShip.world;

            //Restore mortality chances
            playerShip.mortalChances = 3;

            //To do: Restore player HP
            playerShip.ship.damageSystem.Restore();

            //Resurrect the player; remove wreck and restore ship + heading
            var wreck = epitaph.wreck;
            if (wreck != null) {
                wreck.Destroy(null);
                world.RemoveEntity(wreck);
                world.entities.all.Remove(wreck);
            }
            playerShip.ship.active = true;
            playerShip.AddMessage(new Message("A vision of disaster flashes before your eyes"));
            world.entities.all.Add(playerShip);
            world.effects.all.Add(new Heading(playerShip));
            GameHost.Instance.Screen = new TitleSlideOpening(new Pause(playerMain, Resume, 4), false) { IsFocused = true };
            void Resume() {
                GameHost.Instance.Screen = playerMain;
                playerMain.IsFocused = true;
                playerMain.ShowUI();
            }
        }
        public void Exit() {
            var profile = playerMain.profile;
            if(profile != null) {
                var unlocked = SAchievements.GetAchievements(profile, playerMain.playerShip)
                    .Except(profile.achievements);
                if (unlocked.Any()) {
                    Console c = new Console(Width, Height) { FontSize = playerMain.FontSize, IsFocused = true };

                    int x = 1;
                    int y = 1;
                    var fs = playerMain.FontSize * 2;
                    c.Children.Add(new Label("Achievement Unlocked") {
                        Position = new Point(x, y++),
                        FontSize = fs
                    });
                    y++;
                    foreach (var a in unlocked) {
                        c.Children.Add(new Label(SAchievements.names[a]) {
                            Position = new Point(x, y++),
                            FontSize = fs
                        });
                    }

                    c.Children.Add(new LabelButton("Continue", TitleScreen) {
                        Position = new Point(x, c.Height/2 - 2),
                        FontSize = fs
                    });

                    SadConsole.Game.Instance.Screen = c;
                    profile.achievements.UnionWith(unlocked);
                    profile.Save();
                    return;
                }

            }
            TitleScreen();
            void TitleScreen() {
                SadConsole.Game.Instance.Screen = new TitleSlideOpening(new TitleScreen(Width, Height, new World(playerMain.world.universe))) { IsFocused = true };
            }
        }
        public override void Update(TimeSpan delta) {
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            var playerShip = playerMain.playerShip;
            var player = playerShip.player;
            var str =
@$"
{player.name}
{player.Genome.name}
{playerShip.shipClass.name}
{epitaph.desc}

Final Devices
{string.Join('\n', playerShip.devices.Installed.Select(device => $"    {device.source.type.name}"))}

Final Cargo
{string.Join('\n', playerShip.cargo.Select(item => $"    {item.type.name}"))}

Ships Destroyed
{string.Join('\n', playerShip.shipsDestroyed.GroupBy(sc => sc.shipClass).Select(pair => $"    {pair.Key.name, -16}{pair.Count(), 4}"))}
".Replace("\r", "");
            int y = 2;
            foreach(var line in str.Split('\n')) {
                this.Print(2, y++, line);
            }

            if(epitaph.deathFrame != null) {
                var size = epitaph.deathFrame.GetLength(0);
                for (y = 0; y < size; y++) {
                    for (int x = 0; x < size; x++) {
                        this.SetCellAppearance(Width - x - 2, y + 1, epitaph.deathFrame[x, y]);
                    }
                }
            }
            

            base.Render(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            return base.ProcessKeyboard(keyboard);
        }
    }
}
