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

namespace TranscendenceRL {
    public class PauseMenu : Console {
        PlayerMain playerMain;
        public PauseMenu(PlayerMain playerMain) : base(playerMain.Width, playerMain.Height) {
            this.playerMain = playerMain;

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
        }
        public override void Render(TimeSpan delta) {
            this.Clear();
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    var cg = playerMain.GetCellAppearance(x, y);


                    var cgBack = playerMain.back.GetCellAppearance(x, y);

                    var glyph = cgBack.Glyph;
                    var front = cgBack.Foreground.Blend(Color.Black);
                    if(cg.Glyph != 0 && cg.Glyph != ' ') {
                        glyph = cg.Glyph;
                        front = cg.Foreground.Blend(front);
                    }
                    front = front.SetHSL(0, 0, front.GetBrightness());


                    var back = cgBack.Background.Blend(Color.Black);
                    back = cg.Background.Blend(back);
                    back = back.SetHSL(0, 0, back.GetBrightness());

                    this.SetCellAppearance(x, y, new ColoredGlyph(front, back, glyph));
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
            GameHost.Instance.Screen = new TitleScreen(playerMain.Width, playerMain.Height, playerMain.World);
        }
    }
}
