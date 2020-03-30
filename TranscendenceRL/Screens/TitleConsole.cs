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
using System.IO;

namespace TranscendenceRL {
    class TitleConsole : Window {
        string[] title = Properties.Resources.Title.Replace("\r\n", "\n").Split('\n');
        World World = new World();

        public IShip pov;
        public int povTimer;
        public XY camera;
        public Dictionary<(int, int), ColoredGlyph> tiles;

        public TitleConsole(int width, int height) : base(width, height) {
            UseKeyboard = true;
            ButtonTheme BUTTON_THEME = new SadConsole.Themes.ButtonTheme() {
                Normal = new SadConsole.Cell(Color.Blue, Color.Transparent),
                Disabled = new Cell(Color.Gray, Color.Transparent),
                Focused = new Cell(Color.Cyan, Color.Transparent),
                MouseDown = new Cell(Color.White, Color.Transparent),
                MouseOver = new Cell(Color.Cyan, Color.Transparent),
            };
            camera = new XY(0, 0);
            tiles = new Dictionary<(int, int), ColoredGlyph>();
            /*
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
            */

            World.types.Load(Directory.GetFiles("Content", "*.xml"));

        }
        private void StartGame() {
            Hide();
            //new GameConsole(Width/2, Height/2).Show(true);
            new ShipSelector(Width, Height, World).Show(true);
        }
        private void Exit() {
            Environment.Exit(0);
        }
        public override void Update(TimeSpan timeSpan) {
            World.entities.UpdateSpace();
            World.effects.UpdateSpace();

            tiles.Clear();
            //Update everything
            foreach (var e in World.entities.all) {
                e.Update();
                if (e.Tile != null && !tiles.ContainsKey(e.Position)) {
                    tiles[e.Position] = e.Tile;
                }
            }
            foreach (var e in World.effects.all) {
                e.Update();
                if (e.Tile != null && !tiles.ContainsKey(e.Position)) {
                    tiles[e.Position] = e.Tile;
                }
            }

            World.entitiesAdded.ForEach(e => World.entities.all.Add(e));
            World.effectsAdded.ForEach(e => World.effects.all.Add(e));
            World.entitiesAdded.Clear();
            World.effectsAdded.Clear();
            World.entities.all.RemoveWhere(e => !e.Active);
            World.effects.all.RemoveWhere(e => !e.Active);

            if(World.entities.all.OfType<IShip>().Count() < 5) {
                var shipClasses = World.types.shipClass.Values;
                var shipClass = shipClasses.ElementAt(new Random().Next(shipClasses.Count));
                var angle = World.karma.NextDouble() * Math.PI * 2;
                var distance = World.karma.Next(10, 20);
                var center = World.entities.all.FirstOrDefault()?.Position ?? new XY(0, 0);
                var ship = new Ship(World, shipClass, Sovereign.Gladiator, center + XY.Polar(angle, distance));
                var enemy = new AIShip(ship, new AttackAllOrder(ship));
                World.entities.all.Add(enemy);
            }
            if(pov == null || povTimer < 1) {
                pov = World.entities.all.OfType<IShip>().First();
                povTimer = 150;
            } else if(!pov.Active) {
                povTimer--;
            }
            /*
            //The buffer is updated every update rather than every draw, so that the camera stays centered on the ship
            tiles.Clear();
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var offset = new XY(x, y) - new XY(Width / 2, Height / 2);
                    var location = camera + offset;
                    var e = World.entities[location].FirstOrDefault() ?? World.effects[location].FirstOrDefault();
                    if (e != null) {
                        var tile = e.Tile;
                        if (tile.Background == Color.Transparent) {
                            tile.Background = World.backdrop.GetBackground(location, camera);
                        }
                        tiles[(x, y)] = tile;
                    } else {
                        tiles[(x, y)] = World.backdrop.GetTile(new XY(x, y), camera);
                    }
                }
            }
            */
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

            titleY++;
            titleY++;
            Print(titleX, titleY, new ColoredString("[Enter]  Start", Color.White, Color.Black));
            titleY++;
            Print(titleX, titleY, new ColoredString("[Escape] Exit", Color.White, Color.Black));
            camera = pov.Position;
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = GetGlyph(x, y);
                    if (g == 0 || g == ' ') {
                        
                        var offset = new XY(x, y) - new XY(Width / 2, Height / 2);
                        var location = camera + offset;
                        if(tiles.TryGetValue(location, out var tile)) {
                            if(tile.Background == Color.Transparent) {
                                tile.Background = World.backdrop.GetBackground(location, camera);
                            }
                            Print(x, y, tile);
                        } else {
                            Print(x, y, World.backdrop.GetTile(location, camera));
                        }
                        
                        /*
                        var e = World.entities[location].FirstOrDefault() ?? World.effects[location].FirstOrDefault();
                        if (e != null) {
                            var tile = e.Tile;
                            if(tile.Background == Color.Transparent) {
                                tile.Background = World.backdrop.GetBackground(location, camera);
                            }
                            Print(x, y, tile);
                        } else {
                            Print(x, y, World.backdrop.GetTile(new XY(x, y), camera));
                        }
                        */
                    } else {
                        SetBackground(x, y, World.backdrop.GetBackground(new XY(x, y), camera));
                    }
                    
                }
            }

            base.Draw(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(Enter)) {
                StartGame();
            }
            if(info.IsKeyPressed(Escape)) {
                Exit();
            }
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
