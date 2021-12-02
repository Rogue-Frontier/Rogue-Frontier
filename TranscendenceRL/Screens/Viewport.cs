using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = SadConsole.Console;
namespace TranscendenceRL
{
    public class Viewport : Console {
		Camera camera;
		public Dictionary<(int, int), ColoredGlyph> tiles;
		public Viewport(Console prev, Camera camera, Dictionary<(int, int), ColoredGlyph> tiles) : base(prev.Width, prev.Height) {
			this.camera = camera;
			this.tiles = tiles;

        }
        public override void Render(TimeSpan delta) {
			this.Clear();
			int ViewWidth = Width;
			int ViewHeight = Height;
			int HalfViewWidth = ViewWidth / 2;
			int HalfViewHeight = ViewHeight / 2;

			for (int x = -HalfViewWidth; x < HalfViewWidth; x++)
			{
				for (int y = -HalfViewHeight; y < HalfViewHeight; y++)
				{
					XY location = camera.position + new XY(x, y).Rotate(camera.rotation);

					if (tiles.TryGetValue(location.roundDown, out var tile))
					{
						var xScreen = x + HalfViewWidth;
						var yScreen = HalfViewHeight - y;
						this.SetCellAppearance(xScreen, yScreen, tile);
					}
				}
			}
			base.Render(delta);
        }
    }
}
