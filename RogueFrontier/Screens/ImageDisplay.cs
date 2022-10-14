using System;
using SadConsole;
using Console = SadConsole.Console;
using Common;
using SadRogue.Primitives;

namespace RogueFrontier;

public class ImageDisplay : Console {
    public ColorImage image;
    public Point adjust;
    public ImageDisplay(int width, int height, ColorImage image, Point adjust) : base(width, height) {
        this.image = image;
        this.adjust = adjust;
    }
    public override void Render(TimeSpan delta) {
        //var adj = (new Point(Width, Height) - dimensions.Size) / 2 - dimensions.Position;
        foreach (((int x, int y) p, ColoredGlyph t) in image.Sprite) {
            var pos = (Point)p + adjust;

            this.SetCellAppearance(pos.X, pos.Y, t);
        }

        base.Render(delta);
    }
}
