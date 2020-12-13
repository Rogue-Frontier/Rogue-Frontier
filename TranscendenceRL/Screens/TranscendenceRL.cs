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
		public static int Width = 150, Height = 90;
		static void Main(string[] args) {
			// Setup the engine and create the main window.
			SadConsole.Game.Create(Width, Height, "RogueFrontierContent/IBMCGA.font");
            // Hook the start event so we can add consoles to the system.
            SadConsole.Game.Instance.OnStart = Init;
#if DEBUG
			// Start the game.
			SadConsole.Game.Instance.Run();
			SadConsole.Game.Instance.Dispose();
#else
			try {
				// Start the game.
				SadConsole.Game.Instance.Run();
			} catch (Exception e) {
				throw;
			} finally {
				SadConsole.Game.Instance.Dispose();
			}
#endif
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

            var title = new TitleSlideOpening(new TitleScreen(Width, Height, w)) { IsFocused = true };

            KeyConsole container = new KeyConsole(Width, Height, (k) => {
                if (k.IsKeyPressed(Keys.Enter)) {
                    ShowTitle();
                }
            }) { IsFocused = true, UseKeyboard = true };
            GameHost.Instance.Screen = container;
            ShowSplash();

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
                }) { Position = new Point(prev.Width/4 - s.Length / 2, 18), FontSize = prev.FontSize * 2 };
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
                Console c = null;
                c = new SimpleCrawl(
@"                  
...A reimagining of
    
   --Transcendence--
by George Moromisato

Because I know it was
more than just a dream...
                    ".Replace("\r", null), () => ShowFade2(c)) {
                    Position = new Point(4, 4),
                    FontSize = prev.FontSize * 2
                };

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            void ShowFade2(Console prev) {
                Console c = null;
                c = new FadeOut(prev, () => ShowPoster(c), 1);

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }

            void ShowPoster(Console prev) {
                var display = new DisplayImage(Width, Height, poster, new Point(-5, -5));

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
                title.IsFocused = true;
                GameHost.Instance.Screen = title;
            }
            //GameHost.Instance.Screen = new TitleDraw();
        }
    }
}
