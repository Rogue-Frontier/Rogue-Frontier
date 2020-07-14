using System;
using SadConsole;
using Console = SadConsole.Console;
using TranscendenceRL;

namespace TranscendenceRL {

	class TranscendenceRL {

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
			var title = new TitleSlideOpening(new TitleScreen(Width, Height)) { IsFocused = true };
			GameHost.Instance.Screen = new SplashScreen(title) { IsFocused = true };
		}
	}
}
