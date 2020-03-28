using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Controls;
using SadConsole.Input;
using SadConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Xna.Framework.Input.Keys;
using static Common.Helper;
using Common;

namespace TranscendenceRL {
    class TitleConsole : Window {
        string[] title = Properties.Resources.Title.Replace("\r\n", "\n").Split('\n');
        World World = new World();
        public TitleConsole(int width, int height) : base(width, height) {
            UseKeyboard = true;
            ButtonTheme BUTTON_THEME = new SadConsole.Themes.ButtonTheme() {
                Normal = new SadConsole.Cell(Color.Blue, Color.Transparent),
                Disabled = new Cell(Color.Gray, Color.Transparent),
                Focused = new Cell(Color.Cyan, Color.Transparent),
                MouseDown = new Cell(Color.White, Color.Transparent),
                MouseOver = new Cell(Color.Cyan, Color.Transparent),
            };
            {
                var size = 20;
                var y = (Height * 3) / 4;
                var start = new Button(size, 1) {
                    //Position = new Point((Width / 2) - (size / 2), y),
                    Position = new Point(8, y),
                    Text = "NEW GAME",
                    Theme = BUTTON_THEME,
                };
                start.Click += (o, e) => StartGame();
                Add(start);
            }
            {
                var size = 20;
                //var y = (Height * 3) / 4;
                var y = (Height * 3) / 4 + 2;
                var exit = new Button(size, 1) {
                    //Position = new Point((Width * 3) / 4 - (size / 2), y),
                    Position = new Point(8, y),
                    Text = "EXIT",
                    Theme = BUTTON_THEME
                };
                exit.Click += (o, e) => Exit();
                Add(exit);
            }

            World.types.Load("Content/Ships.xml", "Content/Stations.xml", "Content/Player.xml", "Content/Items.xml");
        }
        private void StartGame() {
            Hide();
            //new GameConsole(Width/2, Height/2).Show(true);
            new ShipSelector(Width, Height, World).Show(true);
        }
        private void Exit() {
            Environment.Exit(0);
        }
        public override void Draw(TimeSpan drawTime) {
            Clear();

            var titleX = (Width / 2) - title[0].Length / 2;
            var titleY = 0;
            Print(0, titleY, new ColoredString(new string('.', Width), Color.White, Color.Black));
            titleY++;
            titleY++;
            foreach(var line in title) {
                Print(titleX, titleY, new ColoredString(line, Color.White, Color.Black));
                titleY++;
            }
            Print(0, titleY, new ColoredString(new string('.', Width), Color.White, Color.Black));
            titleY++;
            Print(titleX, titleY, new ColoredString(@"A fangame by Alex ""Archcannon"" Chen", Color.White, Color.Black));
            titleY++;
            Print(titleX, titleY, new ColoredString(@"For Transcendence by George Moromisato", Color.White, Color.Black));
            titleY++;
            Print(titleX, titleY, new ColoredString(@"April 2020", Color.White, Color.Black));

            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = GetGlyph(x, y);
                    if (g == 0 || g == ' ') {
                        Print(x, y, World.backdrop.GetTileFixed(new XY(x, y)));
                    } else {
                        SetBackground(x, y, World.backdrop.GetBackgroundFixed(new XY(x, y)));
                    }
                    
                }
            }

            base.Draw(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
#if DEBUG
            if (info.IsKeyDown(LeftShift) && info.IsKeyPressed(G)) {
                Hide();
                new GameConsole(Width, Height, World, World.types.Lookup<ShipClass>("scAmethyst")).Show(true);
            }
#endif
            return base.ProcessKeyboard(info);
        }
    }
}
