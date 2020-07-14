using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using System.IO;
using Common;
using SadConsole;
using Console = SadConsole.Console;
using System.Linq;
using static SadConsole.Input.Keys;
using static UI;
using ASECII;

namespace TranscendenceRL {
    class ArenaScreen : Console {
        World World;
        public XY camera;
        public Dictionary<(int, int), ColoredGlyph> tiles;
        XY screenCenter;

        public ArenaScreen(Console prev, World World) : base(prev.Width, prev.Height) {
            UseKeyboard = true;
            this.World = World;
            camera = new XY(0.5, 0.5);
            tiles = new Dictionary<(int, int), ColoredGlyph>();
            screenCenter = new XY(Width / 2, Height / 2);

            this.Children.Add(new TextField(16) { Position = new Point(8, 8)});
        }
        public override void Update(TimeSpan timeSpan) {
            World.UpdateAdded();

            tiles.Clear();
            World.UpdateActive(tiles);
            World.UpdateRemoved();
            base.Update(timeSpan);
        }
        public override void Draw(TimeSpan drawTime) {
            this.Clear();

            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = this.GetGlyph(x, y);

                    var offset = new XY(x, y) - screenCenter;
                    var location = camera + offset;
                    if (g == 0 || g == ' ' || this.GetForeground(x, y).A == 0) {


                        if (tiles.TryGetValue(location.RoundDown, out var tile)) {
                            if (tile.Background == Color.Transparent) {
                                tile.Background = World.backdrop.GetBackground(location, camera);
                            }
                            this.SetCellAppearance(x, y, tile);
                        } else {
                            this.SetCellAppearance(x, y, World.backdrop.GetTile(location, camera));
                        }
                    } else {
                        this.SetBackground(x, y, World.backdrop.GetBackground(location, camera));
                    }
                }
            }
            base.Draw(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            return base.ProcessKeyboard(info);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
    }
}
