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
    class LoadMenu : Console {
        Settings settings;
        public LoadMenu(int Width, int Height, Settings settings) : base(Width, Height) {
            this.settings = settings;

            UseKeyboard = true;
            FocusOnMouseClick = true;

            Init();
        }
        public void Reset() {
            Children.Clear();
            Init();
        }
        public void Init() {
            int x = 2;
            int y = 0;

            var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}save", "*.trl");
            if (files.Any()) {
                var dir = Path.GetFullPath(".");
                foreach (var file in files) {

                    var b = new LabelButton(file.Replace(dir, null), () => {
                        var loaded = SaveGame.Deserialize(File.ReadAllText(file));

                        var s = (Console)GameHost.Instance.Screen;
                        int Width = s.Width, Height = s.Height;

                        switch (loaded) {
                            case LiveGame live: {
                                    var playerMain = new PlayerMain(Width, Height, live.world, live.playerShip) { IsFocused = true };
                                    live.playerShip.onDestroyed += new EndGamePlayerDestroyed(playerMain);
                                    GameHost.Instance.Screen = playerMain;
                                    break;
                                }
                            case DeadGame dead: {
                                    var deathScreen = new DeathScreen(new PlayerMain(Width, Height, dead.world, dead.playerShip), dead.epitaph) { IsFocused = true };
                                    GameHost.Instance.Screen = deathScreen;
                                    break;
                                }
                        }
                    }) { Position = new Point(x, y++), FontSize = FontSize };
                    Children.Add(b);
                }
            } else {
                Children.Add(new Label("No save files found") { Position = new Point(x, y++), FontSize = FontSize });
            }
        }
        string GetLabel(ControlKeys control) => $"{control.ToString(),-16} {settings.controls[control].ToString(),-12}";

        public override bool ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(Keys.Escape)) {
                Parent.Children.Remove(this);
            }
            return base.ProcessKeyboard(info);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
    }
}
