using ArchConsole;
using Newtonsoft.Json;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Console = SadConsole.Console;

namespace TranscendenceRL {
    class LoadScreen : Console {
        TitleScreen prev;
        Settings settings;

        public LoadScreen(TitleScreen prev, Settings settings, World World) : base(prev.Width, prev.Height) {
            this.prev = prev;
            this.settings = settings;

            UseKeyboard = true;
            FocusOnMouseClick = true;

            int x = 3;
            int y = 24;

            var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}save", "*.trl");
            if (files.Any()) {
                foreach (var file in files) {

                    var b = new LabelButton(file, () => {
                        var loaded = SaveGame.Deserialize(File.ReadAllText(file));

                        switch (loaded) {
                            case LiveGame live:
                                var playerMain = new PlayerMain(Width, Height, live.world, live.playerShip) { IsFocused = true };
                                live.playerShip.OnDestroyed += new EndGame(playerMain);
                                GameHost.Instance.Screen = playerMain;
                                break;
                        }
                    }) { Position = new Point(x, y++) };
                    Children.Add(b);
                }
            } else {
                Children.Add(new Label("No save files found") { Position = new Point(x, y++) });
            }

        }
        string GetLabel(ControlKeys control) => $"{control.ToString(),-16} {settings.controls[control].ToString(),-12}";
        public override void Update(TimeSpan timeSpan) {
            prev.Update(timeSpan);
            base.Update(timeSpan);
        }
        public override void Render(TimeSpan drawTime) {
            this.Clear();
            prev.Render(drawTime);
            base.Render(drawTime);
        }

        public override bool ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(Keys.Escape)) {
                SadConsole.Game.Instance.Screen = prev;
                prev.IsFocused = true;
            }
            return base.ProcessKeyboard(info);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
    }
}
