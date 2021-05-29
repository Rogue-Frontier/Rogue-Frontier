using SadConsole;
using Console = SadConsole.Console;
using System.IO;
using System.Collections.Generic;
using Common;
using SadRogue.Primitives;
using System.Linq;
using ASECII;
using SadConsole.Input;
using System;
using static Common.Main;
namespace TranscendenceRL {
    partial class Program {
		public static int TICKS_PER_SECOND = 60;

        static Program() {
            Height = 60;
            Width = Height * 5 / 3;
        }
        public static int Width, Height;
        public static string font = CheckFile("RogueFrontierContent/sprites/IBMCGA.font");
        public static string main = CheckFile("RogueFrontierContent/scripts/Main.xml");
        public static string cover = CheckFile("RogueFrontierContent/sprites/RogueFrontierPosterV2.asc.cg");
        public static string splash = CheckFile("RogueFrontierContent/sprites/SplashBackgroundV2.asc.cg");
		static void Main(string[] args) {
			// Setup the engine and create the main window.
			SadConsole.Game.Create(Width, Height, font);
            // Hook the start event so we can add consoles to the system.
            SadConsole.Game.Instance.OnStart = Init;
			// Start the game.
			SadConsole.Game.Instance.Run();
			SadConsole.Game.Instance.Dispose();
		}

		private static void Init() {
            Directory.CreateDirectory("save");
#if false
            GameHost.Instance.Screen = new BackdropConsole(Width, Height, new Backdrop(), () => new Common.XY(0.5, 0.5));
			return;
#endif
			World w = new World();
			w.types.LoadFile(main);

            //var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}save", "*.trl");
            //SaveGame.Deserialize(File.ReadAllText(files.First()));

            var poster = new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText(cover)));

            var title = new TitleScreen(Width, Height, w);
            var titleSlide = new TitleSlideOpening(title) { IsFocused = true };

            var splashBack = new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText(splash)));
            var splashBackground = new DisplayImage(Width / 2, Height / 2, splashBack, new Point()) { FontSize = title.FontSize * 2 };

            int index = 0;
            KeyConsole container = null;
            container = new KeyConsole(Width, Height, (k) => {
                if (k.IsKeyPressed(Keys.Enter)) {
                    switch(index) {
                        case 1: {
                                container.Children.Clear();
                                Console c = new(Width, Height);
                                container.Children.Add(c);
                                ShowOpening(c);
                                break;
                            }
                        case 2: {
                                container.Children.Clear();
                                Console c = new(Width, Height);
                                container.Children.Add(c);
                                ShowPoster(c);
                                break;
                            }
                        case 3:
                        default:
                            ShowTitle();
                            break;
                    }
                    
                }
            }) { IsFocused = true, UseKeyboard = true,
            };
            container.Children.Add(splashBackground);

            GameHost.Instance.Screen = container;

#if DEBUG
            ShowTitle();
            //title.QuickStart();
            title.StartSurvival();
#else
            ShowSplash();
#endif

            void ShowSplash() {
                index = 1;
                SplashScreen c = null;
                c = new SplashScreen(() => ShowCrawl(c));
                container.Children.Add(c);
            }
            void ShowCrawl(Console prev) {
                SimpleCrawl c = null;
                string s = "Presents...";
                c = new SimpleCrawl(s, () => {
                    ShowPause(prev);
                }) { Position = new Point(prev.Width/4 - s.Length / 2, 13), FontSize = prev.FontSize * 2 };
                prev.Children.Add(c);
            }
            void ShowPause(Console prev) {
                Console c = null;
                c = new PauseTransition(Width, Height, 1, prev, () => ShowFade(c));

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            void ShowFade(Console prev) {
                Console c = null;
                c = new FadeOut(prev, () => ShowOpening(c), 1);

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }

            void ShowOpening(Console prev) {
                index = 2;
                prev.Parent.Children.Remove(splashBackground);

                Console c = null;
                c = new SimpleCrawl(
@"                  
A reimagining of...
                    
      Transcendence  
 by George Moromisato
                    
And the vision that was
more than just a dream...
                    ".Replace("\r", null), () => ShowFade2(c)) {
                    Position = new Point(4, 4),
                    FontSize = title.FontSize * 3
                };

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            /*
            void ShowPause2(Console prev) {
                Console c = null;
                c = new PauseTransition(Width, Height, 1, prev, () => ShowFade2(c));

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            */
            void ShowFade2(Console prev) {
                Console c = null;
                c = new FadeOut(prev, () => ShowPause2(c), 1);

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            void ShowPause2(Console prev) {
                Console c = null;
                c = new PauseTransition(Width, Height, 1, prev, () => ShowPoster(c));

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            void ShowPoster(Console prev) {
                index = 3;
                var display = new DisplayImage(Width, Height, poster, new Point(Width / 2 - poster.Size.X / 2 - 32, -5)) { FontSize = title.FontSize };

                Console pause = null;
                pause = new PauseTransition(Width, Height, 2, display, () => ShowPosterFade(pause));

                //Note that FadeIn automatically replaces the child console
                Console c = null;
                c = new FadeIn(pause);

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            void ShowPosterFade(Console prev) {
                var c = new FadeOut(prev, ShowTitle, 1);

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }

            void ShowTitle() {
                index = 4;
                titleSlide.IsFocused = true;
                GameHost.Instance.Screen = titleSlide;
            }
            //GameHost.Instance.Screen = new TitleDraw();
        }
    }
}
