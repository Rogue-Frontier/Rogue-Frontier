using Common;
using Newtonsoft.Json;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using static TranscendenceRL.StationType;
using System.Linq;

namespace TranscendenceRL {
    public class Stargate : SpaceObject {
        [JsonIgnore]
        public string Name => $"Stargate";
        [JsonProperty]
        public World World { get; private set; }
        [JsonProperty]
        public Sovereign Sovereign { get; private set; }
        [JsonProperty]
        public XY Position { get; private set; }
        [JsonProperty]
        public XY Velocity { get; private set; }
        [JsonProperty]
        public bool Active => true;
        [JsonProperty]
        public HashSet<Segment> Segments { get; private set; }
        [JsonIgnore]
        public ColoredGlyph Tile => null;
        public Stargate() { }
        public Stargate(World World, XY Position) {
            this.World = World;
            this.Sovereign = Sovereign.Inanimate;
            this.Position = Position;
            this.Velocity = new XY();
        }
        public void CreateSegments() {
            Segments = new HashSet<Segment>();

            int radius = 6;
            double circumference = 2 * Math.PI * radius;
            for (int i = 0; i < 2 * Math.PI * radius; i++) {
                Segments.Add(new Segment(this, new SegmentDesc(
                    XY.Polar(2 * Math.PI * i / circumference, radius),
                    new ColoredGlyph(Color.White, Color.Transparent, '+')
                    )));
            }

            foreach(var i in Enumerable.Range(1 + radius, 5)) {
                Segments.Add(new Segment(this, new SegmentDesc(
                    XY.Polar(0, i),
                    new ColoredGlyph(Color.White, Color.Transparent, '+')
                    )));
                Segments.Add(new Segment(this, new SegmentDesc(
                   XY.Polar(Math.PI / 2, i),
                   new ColoredGlyph(Color.White, Color.Transparent, '+')
                   )));
                Segments.Add(new Segment(this, new SegmentDesc(
                   XY.Polar(Math.PI, i),
                   new ColoredGlyph(Color.White, Color.Transparent, '+')
                   )));
                Segments.Add(new Segment(this, new SegmentDesc(
                   XY.Polar(Math.PI * 3 / 2, i),
                   new ColoredGlyph(Color.White, Color.Transparent, '+')
                   )));
            }

            radius--;
            for (int i = 0; i < 2 * Math.PI * radius; i++) {
                Segments.Add(new Segment(this, new SegmentDesc(
                    XY.Polar(2 * Math.PI * i / circumference, radius),
                    new ColoredGlyph(Color.Violet.SetAlpha(204), Color.Transparent, '#')
                    )));
            }
            for (int x = -radius + 1; x < radius; x++) {
                for (int y = -radius + 1; y < radius; y++) {
                    if (x * x + y * y <= radius*radius) {
                        Segments.Add(new Segment(this, new SegmentDesc(
                            new XY(x, y),
                            new ColoredGlyph(Color.BlueViolet.SetAlpha(204),
                                Color.Transparent, '%')
                        )));
                    }
                }
            }

            foreach (var s in Segments) {
                World.AddEntity(s);
            }
        }
        public void Damage(SpaceObject source, int hp) {
        }

        public void Destroy(SpaceObject source) {
        }

        public void Update() {
        }
    }
}
