using ArchConsole;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Console = SadConsole.Console;
using SadRogue.Primitives;
using System.IO;
using ASECII;
using Common;

namespace TranscendenceRL {
    public class PauseMenu : Console {
        public PlayerMain playerMain;
        public SparkleFilter sparkle;
        public PauseMenu(PlayerMain playerMain) : base(playerMain.Width, playerMain.Height) {
            this.playerMain = playerMain;
            this.sparkle = new SparkleFilter(Width, Height);

            int x = 2;
            int y = 2;

            var fs = FontSize * 4;

            this.Children.Add(new Label("[Paused]") { Position = new Point(x, y++), FontSize = fs });
            y++;
            this.Children.Add(new LabelButton("Continue", Continue) { Position = new Point(x, y++), FontSize = fs });
            y++;
            this.Children.Add(new LabelButton("Save & Continue", SaveContinue) { Position = new Point(x, y++), FontSize = fs });
            y++;
            this.Children.Add(new LabelButton("Save & Quit", SaveQuit) { Position = new Point(x, y++), FontSize = fs });
            y++;
            y++;
            y++;
            this.Children.Add(new LabelButton("Delete & Quit", Quit) { Position = new Point(x, y++), FontSize = fs });
        }
        public override void Update(TimeSpan delta) {
            sparkle.Update();
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            this.Clear();

            var c = new ConsoleComposite(playerMain.back, playerMain);
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    var cg = c[x, y].Gray();
                    sparkle.Filter(x, y, ref cg);
                    this.SetCellAppearance(x, y, cg);
                }
            }
            base.Render(delta);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(Keys.Escape)) {
                Continue();
            }
            return base.ProcessKeyboard(info);
        }
        public void Continue() {
            IsVisible = false;
        }
        public void Save() {
            File.WriteAllText(playerMain.playerShip.player.file, SaveGame.Serialize(new LiveGame() {
                player = playerMain.playerShip.player,
                playerShip = playerMain.playerShip,
                world = playerMain.World
            }));
        }
        public void SaveContinue() {
            Save();
            Continue();
        }
        public void SaveQuit() {
            Save();
            Quit();
        }

        public void Quit() {
            GameHost.Instance.Screen = new TitleScreen(playerMain.Width, playerMain.Height, playerMain.World);
        }
    }
}
