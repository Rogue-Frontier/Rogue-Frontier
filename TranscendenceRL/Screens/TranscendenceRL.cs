using SadConsole;
using Console = SadConsole.Console;
using TranscendenceRL;
using TranscendenceRL.Screens;
using SadConsole.Renderers;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Common;
using SadRogue.Primitives;
using System.Linq;
using Newtonsoft.Json;
using ASECII;

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
#if false
            GameHost.Instance.Screen = new BackdropConsole(Width, Height, new Backdrop(), () => new Common.XY(0.5, 0.5));
			return;
#endif
			World w = new World();
			w.types.Load("RogueFrontierContent/Main.xml");

            var poster = new ColorImage(ASECIILoader.DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText("RogueFrontierContent/RogueFrontierPoster.cg")));
            
            KeyConsole container = new KeyConsole(Width, Height, (k) => {
                
            });
            GameHost.Instance.Screen = container;
            ShowSplash();

            void ShowSplash() {
                SplashScreen c = null;
                c = new SplashScreen(() => ShowPause(c)) { IsFocused = true };
                container.Children.Add(c);
            }
            void ShowPause(Console prev) {
                Console c = null;
                c = new PauseTransition(1, prev, () => ShowFade(c)) { IsFocused = true };

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            void ShowFade(Console prev) {
                Console c = null;
                c = new FadeOut(prev, () => ShowPoster(c), 1) { IsFocused = true };

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            void ShowPoster(Console prev) {
                var display = new DisplayImage(Width, Height, poster, new Point(-5, -5));

                Console pause = null;
                pause = new PauseTransition(2, display, () => ShowPosterFade(pause));

                //Note that FadeIn automatically replaces the child console
                Console c = null;
                c = new FadeIn(pause) { IsFocused = true };

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }
            void ShowPosterFade(Console prev) {
                var c = new FadeOut(prev, ShowTitle, 1);

                prev.Parent.Children.Add(c);
                prev.Parent.Children.Remove(prev);
            }

            void ShowTitle() {
                var title = new TitleSlideOpening(new TitleScreen(Width, Height, w)) { IsFocused = true };
                GameHost.Instance.Screen = title;
            }
            //GameHost.Instance.Screen = new TitleDraw();
        }
    }
}
