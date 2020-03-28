using Common;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranscendenceRL {
    //A space background made up of randomly generated layers with different depths
    public class Backdrop {
        List<Layer> layers;
        private class Layer {
            public double parallaxFactor;                   //Multiply the camera by this value
            public GeneratedGrid<ColoredGlyph> tiles;  //Dynamically generated grid of tiles
            public Layer(double parallaxFactor, Random r) {
                //Random r = new Random();
                this.parallaxFactor = parallaxFactor;
                tiles = new GeneratedGrid<ColoredGlyph>(p => {
                    var (x, y) = p;
                    var value = r.Next(28);
                    var background = new Color(value, value, value + r.Next(12));

                    var init = new XY[] { new XY(1, 0), new XY(0, 1), new XY(0, -1), new XY(-1, 0) }.Select(xy => new XY(xy.xi + x, xy.yi + y)).Where(xy => tiles.IsInit(xy.xi, xy.yi));

                    var count = init.Count() + 1;
                    foreach (var xy in init) {
                        background = background.Add(tiles.Get(xy.xi, xy.yi).Background);
                    }
                    background = background.Divide(count);
                    background.A = (byte) r.Next(51, 153);

                    if(r.NextDouble() * 100 < (1 / parallaxFactor) / 10) {
                        const string vwls = "?&%~=+;";
                        var star = vwls[r.Next(vwls.Length)];
                        var foreground = new Color(255, 255 - r.Next(25, 51), 255 - r.Next(25, 51), (byte) (204 * parallaxFactor));
                        return new ColoredGlyph(star, foreground, background);
                    } else {
                        return new ColoredGlyph(' ', Color.Transparent, background);
                    }
                });
            }
            public ColoredGlyph GetTile(XY point, XY camera) {
                var apparent = point - camera * parallaxFactor;
                return tiles[apparent.xi, apparent.yi];
            }
            public ColoredGlyph GetTileFixed(XY point) {
                return tiles[point.xi, point.yi];
            }
        }
        public Backdrop() {
            Random r = new Random();
            int layerCount = 10;
            layers = new List<Layer>(layerCount);
            for(int i = 0; i < layerCount; i++) {
                var n = r.Next(1, 5);
                layers.Add(new Layer((double)n / (n + r.Next(1, n)), r));
            }
            layers = layers.OrderBy(l => l.parallaxFactor).ToList();
        }
        public Color GetBackgroundFixed(XY point) => GetBackground(point, XY.Zero);
        public Color GetBackground(XY point, XY camera) {
            Color result = layers.First().GetTile(point, camera).Background;
            foreach(var layer in layers.Skip(1)) {
                result = result.Blend(layer.GetTile(point, camera).Background);
            }
            return result;
        }
        public ColoredGlyph GetTile(XY point, XY camera) {
            ColoredGlyph result = new ColoredGlyph(' ', Color.Transparent, Color.Black);
            foreach (var layer in layers) {
                var tile = layer.GetTile(point, camera);
                result.Background = result.Background.Blend(tile.Background);
                if(tile.GlyphCharacter != ' ') {
                    result.GlyphCharacter = tile.GlyphCharacter;
                    result.Foreground = tile.Foreground;
                }
            }
            return result;
        }
        public ColoredGlyph GetTileFixed(XY point) => GetTile(point, XY.Zero);
    }
}
