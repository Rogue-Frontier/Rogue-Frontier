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
using System.Linq;

namespace TranscendenceRL {
    public class PauseMenu : Console {
        public PlayerMain playerMain;
        public SparkleFilter sparkle;
        public PauseMenu(PlayerMain playerMain) : base(playerMain.Width, playerMain.Height) {
            this.playerMain = playerMain;
            this.sparkle = new SparkleFilter(Width, Height);

            int x = 2;
            int y = 2;

            var fs = FontSize * 3;

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
                    var source = c[x, y];
                    var cg = source.Gray();
                    sparkle.Filter(x, y, ref cg);
                    this.SetCellAppearance(x, y, cg);
                }
            }

            {
                int x = Width/2 + 8;
                int y = 6;
                var controls = playerMain.playerShip.player.Settings;
                foreach (var line in controls.GetString().Replace("\r",null).Split('\n')) {
                    this.Print(x, y++, line.PadRight(Width - x - 4), Color.White, Color.Black);
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

            var ps = playerMain.playerShip;
            new LiveGame(playerMain.World, ps.player, ps).Save();
        }
        public void SaveContinue() {
            //Temporarily PlayerMain events before saving
            var ps = playerMain.playerShip;
            var endgame = new HashSet<EndGame>(ps.onDestroyed.set.OfType<EndGame>());
            ps.onDestroyed.set.ExceptWith(endgame);

            Save();

            ps.onDestroyed.set.UnionWith(endgame);
            
            Continue();
        }
        public void SaveQuit() {
            //Remove PlayerMain events
            playerMain.playerShip.onDestroyed.set.RemoveWhere(d => d is EndGame);

            Save();
            Quit();
        }

        public void Quit() {
            var w = playerMain.World;
            GameHost.Instance.Screen = new TitleScreen(playerMain.Width, playerMain.Height, new World(new Universe(w.types, new Rand()))) { IsFocused = true };
        }
    }
}
