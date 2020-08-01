using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using SadRogue.Primitives;

namespace TranscendenceRL {
    //A space background made up of randomly generated layers with different depths
    public class Backdrop {
        public List<GeneratedLayer> layers;
        public GridLayer planets;
        public GridLayer orbits;
        public Backdrop() : this(new Random()) {

        }
        public Backdrop(Random r) {
            int layerCount = 5;
            layers = new List<GeneratedLayer>(layerCount);
            for(int i = 0; i < layerCount; i++) {
                var layer = new GeneratedLayer(1f / (i * i * 1.5 + i + 1), r);
                layers.Insert(0, layer);
            }
            planets = new GridLayer(1);
            orbits = new GridLayer(1);
        }
        public Color GetBackgroundFixed(XY point) => GetBackground(point, XY.Zero);
        public Color GetBackground(XY point, XY camera) {
            Color result = Color.Black;
            foreach(var layer in layers) {
                result = result.Blend(layer.GetTile(point, camera).Background);
            }
            return result;
        }
        public ColoredGlyph GetTile(XY point, XY camera) {
            //ColoredGlyph result = new ColoredGlyph(Color.Transparent, Color.Black, ' ');
            var top = planets.GetTile(point, camera);

            var g = top.GlyphCharacter;
            var b = top.Background;
            var f = top.Foreground;

            Blend(orbits.GetTile(point, camera));

            for(int i = layers.Count - 1; i > -1; i--) {
                Blend(layers[i].GetTile(point, camera));
            }
            void Blend(ColoredGlyph tile) {
                b = b.Premultiply().Blend(tile.Background);
                if (g == ' ' || g == 0) {
                    if (tile.GlyphCharacter != ' ' && tile.GlyphCharacter != 0) {
                        f = tile.Foreground;
                        g = tile.GlyphCharacter;
                    }
                }
            }
            return new ColoredGlyph(f, b, g);
        }
        public ColoredGlyph GetTileFixed(XY point) => GetTile(point, XY.Zero);
    }
    public interface ILayer {
        double parallaxFactor { get; }
        ColoredGlyph GetTile(XY point, XY camera);
    }
    public class GridLayer : ILayer {
        public double parallaxFactor { get; private set; }
        public Dictionary<(int, int), ColoredGlyph> tiles;
        public GridLayer(double parallaxFactor) {
            this.parallaxFactor = parallaxFactor;
            this.tiles = new Dictionary<(int, int), ColoredGlyph>();
        }
        public ColoredGlyph GetTile(XY point, XY camera) {
            var apparent = point - camera * (1 - parallaxFactor);
            return tiles.TryGetValue(apparent.RoundDown, out var result) ? result : new ColoredGlyph(Color.Transparent, Color.Transparent, ' ');
        }
    }
    public class GeneratedLayer : ILayer {
        public double parallaxFactor { get; private set; }                   //Multiply the camera by this value
        public GeneratedGrid<ColoredGlyph> tiles;  //Dynamically generated grid of tiles
        public GeneratedLayer(double parallaxFactor, GeneratedGrid<ColoredGlyph> tiles) {
            this.parallaxFactor = parallaxFactor;
            this.tiles = tiles;
        }
        public GeneratedLayer(double parallaxFactor, Random random) {
            //Random r = new Random();
            this.parallaxFactor = parallaxFactor;
            tiles = new GeneratedGrid<ColoredGlyph>(p => {
                var (x, y) = p;
                var value = random.Next(51);
                var (r, g, b) = (value, value, value + random.Next(25));

                var init = new XY[] {
                    new XY(-1, -1),
                    new XY(-1, 0),
                    new XY(-1, 1),
                    new XY(0, -1),
                    new XY(0, 1),
                    new XY(1, -1),
                    new XY(1, 0),
                    new XY(1, 1),}.Select(xy => new XY(xy.xi + x, xy.yi + y)).Where(xy => tiles.IsInit(xy.xi, xy.yi));

                var count = init.Count() + 1;
                foreach (var xy in init) {
                    var t = tiles.Get(xy.xi, xy.yi).Background;
                    (r, g, b) = (r + t.R, g + t.G, b + t.B);
                }
                (r, g, b) = (r / count, g / count, b / count);
                var a = (byte)random.Next(25, 104);
                var background = new Color(r, g, b, a);

                if (random.NextDouble() * 100 < (1 / (parallaxFactor + 1))) {
                    const string vwls = "?&%~=+;";
                    var star = vwls[random.Next(vwls.Length)];
                    var foreground = new Color(255, 255 - random.Next(25, 51), 255 - random.Next(25, 51), (byte)(225 * Math.Sqrt(parallaxFactor)));
                    return new ColoredGlyph(foreground, background, star);
                } else {
                    return new ColoredGlyph(Color.Transparent, background, ' ');
                }
            });
        }
        public ColoredGlyph GetTile(XY point, XY camera) {
            var apparent = point - camera * (1 - parallaxFactor);
            apparent = apparent.RoundDown;
            return tiles[apparent.xi, apparent.yi];
        }
        public ColoredGlyph GetTileFixed(XY point) {
            point = point.RoundDown;
            return tiles[point.xi, point.yi];
        }
    }

}
