using Common;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Console = SadConsole.Console;
namespace RogueFrontier;

public class Viewport : ScreenSurface {

    public int Width => Surface.Width;
    public int Height => Surface.Height;
    public Camera camera;
    public System world;
    public Dictionary<(int, int), ColoredGlyph> tiles=new();
    public Viewport(ScreenSurface prev, Camera camera, System world) : base(prev.Surface.Width, prev.Surface.Height) {
        this.camera = camera;
        this.world = world;
    }
    public override void Update(TimeSpan delta) {
        tiles.Clear();
        world.PlaceTiles(tiles);
        base.Update(delta);
    }

    public void UpdateVisible(TimeSpan delta, Func<Entity, double> getVisibleDistanceLeft) {
        tiles.Clear();
        world.PlaceTilesVisible(tiles, getVisibleDistanceLeft);
        base.Update(delta);
    }
    public void UpdateBlind(TimeSpan delta, Func<Entity, double> getVisibleDistanceLeft) {
        world.PlaceTilesOver(tiles, getVisibleDistanceLeft);
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        Surface.Clear();
        int HalfViewWidth = Width / 2;
        int HalfViewHeight = Height / 2;
        for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
            for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
                XY location = camera.position + new XY(x, y).Rotate(camera.rotation);
                if (tiles.TryGetValue(location.roundDown, out var tile)) {
                    var xScreen = x + HalfViewWidth;
                    var yScreen = HalfViewHeight - y;
                    Surface.SetCellAppearance(xScreen, yScreen, tile);
                }
            }
        }
        /*
        Parallel.For(-HalfViewWidth, HalfViewWidth, x => {
        });
        */
        base.Render(delta);
    }
    public ColoredGlyph GetTile(int x, int y) {
        XY location = camera.position + new XY(x - Width / 2, y - Height / 2).Rotate(camera.rotation);
        return tiles.TryGetValue(location.roundDown, out var tile) ? tile : new ColoredGlyph(Color.Transparent, Color.Transparent);
    }
}
