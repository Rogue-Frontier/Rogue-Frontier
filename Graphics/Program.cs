using Microsoft.Xna.Framework.Graphics;
using SadConsole;
using SadConsole.Renderers;
using System;
using System.IO;
using TranscendenceRL;
namespace Graphics
{

	class Program {
		public static int Width = 150, Height = 90;

		static void Main(string[] args) {
			// Setup the engine and create the main window.
			SadConsole.Game.Create(Width, Height, "RogueFrontierContent/IBMCGA.font");
			SadConsole.Game.Instance.DefaultFontSize = Font.Sizes.One;
			SadConsole.Game.Instance.OnStart = Init;
			SadConsole.Game.Instance.Run();
			SadConsole.Game.Instance.Dispose();
		}

		private static void Init() {
			World w = new World();
			w.types.Load("RogueFrontierContent/Main.xml");

			Directory.CreateDirectory("GraphicsContent");

			var s = new WorldDraw(2000, 2000, w);
			s.Render(new TimeSpan());
			var t = ((ScreenSurfaceRenderer)s.Renderer).BackingTexture;
			t.Save("GraphicsContent/Background.png");

		}
	}
}
