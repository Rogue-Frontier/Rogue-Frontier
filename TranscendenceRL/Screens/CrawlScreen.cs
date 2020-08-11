using SadConsole;
using SadConsole.Input;
using SadConsole.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Common.Helper;
using static SadConsole.ColoredString;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using System.IO;
using TranscendenceRL.RogueFrontierContent;

namespace TranscendenceRL {
    class CrawlScreen : Console {
        World World;
        Player player;
        ShipClass playerClass;

        private readonly string text;
        private int lines;
        private int index;
        int tick;

        bool speedUp;
        LoadingSymbol loading;
        int loadingTicks = 150 * 0;

        ColoredString[] effect;

        class ParticleCloud {
            public List<Point> points = new List<Point>();
            public List<ColoredGlyph> particles = new List<ColoredGlyph>();
            public void Update(Random random) {
                points = new List<Point>(points.Select(p => p + new Point(1, 0)));
                foreach(var p in particles) {
                    var f = p.Foreground;
                    var (r, g, b, a) = (f.R, f.G, f.B, f.A);
                    Func<int, int> transform = i => Math.Max(0, i - random.Next(0, 2));
                    p.Foreground = new Color(transform(r), transform(g), transform(b), transform(a));
                }
            }
        }
        List<ParticleCloud> clouds;

        Random random = new Random();
        public CrawlScreen(int width ,int height, World World, Player player, ShipClass playerClass) : base(width, height) {
            this.World = World;
            this.player = player;
            this.playerClass = playerClass;

            text = File.ReadAllText("RogueFrontierContent/Crawl.txt").Replace("\r\n", "\n");
            lines = text.Count(c => c == '\n') + 1;
            index = 0;
            tick = 0;

            int effectWidth = Width * 3 / 5;
            int effectHeight = Height * 3 / 5;

            effect = new ColoredString[effectHeight];
            for(int y = 0; y < effectHeight; y++) {
                effect[y] = new ColoredString(effectWidth);
                for(int x = 0; x < effectWidth; x++) {
                    effect[y][x] = GetGlyph(x, y);
                }

            }

            clouds = new List<ParticleCloud>();

            Color Front(int value) {
                return new Color(255 - value / 2, 255 - value, 255, 255 - value / 4);
                //return new Color(128 + value / 2, 128 + value/4, 255);
            }
            Color Back(int value) {
                //return new Color(255 - value, 255 - value, 255 - value).Noise(r, 0.3).Round(17).Subtract(25);
                return new Color(204 - value, 204 - value, 255 - value).Noise(random, 0.3).Round(17).Subtract(25);
            }
            ColoredGlyphEffect GetGlyph(int x, int y) {
                Color front = Front(255 * x / effectWidth);
                Color back = Back(255 * x / effectWidth);
                char c;
                if (random.Next(x) < 5
                    || (effect[y][x-1].GlyphCharacter != ' ' && random.Next(x) < 10)
                    ) {
                    const string vwls = "?&%~=+;";
                    c = vwls[random.Next(vwls.Length)];
                } else {
                    c = ' ';
                }
                
                
                return new ColoredGlyphEffect() { Foreground = front, Background = back, Glyph = c };
            }
        }
        public override void Update(TimeSpan time) {
            if(index < text.Length) {
                tick++;
                if(speedUp) {
                    index++;
                } else {
                    if (tick % 4 == 0) {
                        index++;
                    }
                }
                if(tick%8 == 0) {
                    clouds.ForEach(c => c.Update(random));
                }
                if(tick%64 == 0) {

                    int effectMinY = Height / 5;
                    int effectMaxY = 4 * Height / 5;

                    ParticleCloud cloud = new ParticleCloud();

                    
                    
                    var cloudPoint = new Point(0, random.Next(effectMinY, effectMaxY));
                    var cloudParticle = new ColoredGlyph(new Color(204 + random.Next(0, 51), 0, 204 + random.Next(0, 51)), Color.Transparent, GetRandomChar());

                    cloud.points.Add(cloudPoint);
                    cloud.particles.Add(cloudParticle);

                    double i = 1;
                    while(random.NextDouble() < 0.9) {
                        cloudPoint += new Point(-1, (random.Next(0, 5) - 2)/2);

                        cloudParticle = new ColoredGlyph(new Color(204 + random.Next(0, 51), 0, 225 + random.Next(0, 25)), Color.Transparent, GetRandomChar());

                        cloud.points.Add(cloudPoint);
                        cloud.particles.Add(cloudParticle);
                        for (int y = 1; y < random.Next(2, 5); y++) {
                            var verticalPoint = cloudPoint + new Point(0, y);

                            cloudParticle = new ColoredGlyph(new Color(225 + random.Next(0, 25), 153 + random.Next(102), 225 + random.Next(0, 25)), Color.Transparent, GetRandomChar());

                            cloud.points.Add(verticalPoint);
                            cloud.particles.Add(cloudParticle);
                        }


                        i++;
                    }
                    clouds.Add(cloud);

                    char GetRandomChar() {
                        const string vwls = "?&%~=+;";
                        return vwls[random.Next(vwls.Length)];
                    }
                }
            } else if (loading == null) {
                loading = new LoadingSymbol(16);
            } else if(loadingTicks > 0) {
                loading.Update();
                loadingTicks--;
            } else {
                //Name is seed
                var seed = player.name.GetHashCode();
                World = new World(World.types, new Random(seed), new Backdrop(new Random(seed)));
                World.types.Lookup<SystemType>("system_orion").Generate(World);
                World.UpdatePresent();

                var start = World.entities.all.OfType<Marker>().First(m => m.Name == "Start");
                start.Active = false;
                var playerStart = start.Position;
                var playerSovereign = World.types.Lookup<Sovereign>("svPlayer");
                var playerShip = new PlayerShip(player, new BaseShip(World, playerClass, playerSovereign, playerStart));
                playerShip.Messages.Add(new InfoMessage("Welcome to Transcendence: Rogue Frontier!"));

                World.AddEffect(new Heading(playerShip));
                World.AddEntity(playerShip);

                var playerMain = new PlayerMain(Width, Height, World, playerShip);
                playerShip.OnDestroyed += (p, d, wreck) => playerMain.EndGame(d, wreck);

                World.AddEvent(new PlayerTutorial(playerMain));

                playerMain.Update(time);
                playerMain.PlaceTiles();
                playerMain.DrawWorld();
                SadConsole.Game.Instance.Screen = new CrawlTransition(Width, Height, this, playerMain) { IsFocused = true };
            }
        }
        public override void Render(TimeSpan drawTime) {
            this.Clear();
            //Print background
            int effectY = Height / 5;
            foreach (var line in effect) {
                this.Print(0, effectY, line);
                effectY++;
            }

            foreach(var cloud in clouds) {
                var points = cloud.points;
                var particles = cloud.particles;
                for(int i = 0; i < points.Count; i++) {
                    var (x, y) = points[i];
                    this.SetForeground(x, y, particles[i].Foreground);
                    this.SetGlyph(x, y, particles[i].Glyph);
                }
            }


            //Print text
            int ViewWidth = Width;
            int ViewHeight = Height;

            //int leftMargin = (ViewWidth) / 2;
            int leftMargin = Width * 2 / 5;
            int topMargin = (ViewHeight / 2) - lines / 2;
            int textX = leftMargin;
            int textY = topMargin;
            for (int i = 0; i < index; i++) {
                char c = text[i];
                if(c == '\n') {
                    textX = leftMargin;
                    textY++;
                } else {
                    if (c != ' ') {
                        this.Print(textX, textY, "" + c, Color.White, this.GetBackground(textX, textY));
                    }
                    textX++;
                }
            }

            //Show loading circle
            if(loading != null) {
                var symbol = loading.Draw();
                int symbolX = Width - symbol[0].Count;
                int symbolY = Height - symbol.Length;
                foreach (var line in symbol) {
                    this.Print(symbolX, symbolY, line);
                    symbolY++;
                }

                this.Print(0, Height - 1, "[Creating Game...]");
            } else {
                if(speedUp) {
                    this.Print(0, Height - 1, "[Press Enter again to skip intro]");
                } else {
                    this.Print(0, Height - 1, "[Press Enter to speed up intro]");
                }
                
            }
            

            base.Render(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(SadConsole.Input.Keys.Enter)) {
                if(speedUp) {
                    index = text.Length;
                } else {
                    speedUp = true;
                }
            }

            return base.ProcessKeyboard(info);
        }
    }
}
