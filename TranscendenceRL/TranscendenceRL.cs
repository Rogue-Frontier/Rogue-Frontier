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
namespace TranscendenceRL {
    partial class TranscendenceRL {
		public static int TICKS_PER_SECOND = 60;

        static TranscendenceRL() {
            Height = 60;
            Width = Height * 5 / 3;
        }
        public static int Width, Height;
		static void Main(string[] args) {
			// Setup the engine and create the main window.
			SadConsole.Game.Create(Width, Height, "RogueFrontierContent/IBMCGA.font");
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
			w.types.Load("RogueFrontierContent/Main.xml");

            //var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}save", "*.trl");
            //SaveGame.Deserialize(File.ReadAllText(files.First()));

            var poster = new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText("RogueFrontierContent/RogueFrontierPoster.cg")));

            var title = new TitleScreen(Width, Height, w);
            var titleSlide = new TitleSlideOpening(title) { IsFocused = true };

            KeyConsole container = new KeyConsole(Width, Height, (k) => {
                if (k.IsKeyPressed(Keys.Enter)) {
                    ShowTitle();
                }
            }) { IsFocused = true, UseKeyboard = true,
            };

            var splashBack = new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText("RogueFrontierContent/SplashBackgroundV2.asc.cg")));
            var splashBackground = new DisplayImage(Width / 2, Height / 2, splashBack, new Point()) { FontSize = container.FontSize * 2 };
            container.Children.Add(splashBackground);

            GameHost.Instance.Screen = container;

#if DEBUG
            ShowTitle();
            title.QuickStart();
#else
            ShowSplash();
#endif
            void ShowSplash() {
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
                    FontSize = prev.FontSize
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
                var display = new DisplayImage(Width, Height, poster, new Point(Width/2 - poster.Size.X/2 - 16, -5));

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
                titleSlide.IsFocused = true;
                GameHost.Instance.Screen = titleSlide;
            }
            //GameHost.Instance.Screen = new TitleDraw();
        }
    }
}
