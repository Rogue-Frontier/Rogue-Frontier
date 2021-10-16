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
        public string name => $"Stargate";
        [JsonIgnore]
        public bool active => true;
        [JsonIgnore]
        public ColoredGlyph tile => new ColoredGlyph(Color.Purple, Color.White, '*');

        [JsonProperty]
        public int Id { get; private set; }
        [JsonProperty]
        public World world { get; private set; }
        [JsonProperty]
        public Sovereign sovereign { get; private set; }
        [JsonProperty]
        public XY position { get; private set; }
        [JsonProperty]
        public XY velocity { get; private set; }
        [JsonProperty]
        public HashSet<Segment> Segments { get; private set; }
        public Stargate() { }
        public Stargate(World World, XY Position) {
            this.Id = World.nextId++;
            this.world = World;
            this.sovereign = Sovereign.Inanimate;
            this.position = Position;
            this.velocity = new XY();
        }
        public void CreateSegments() {
            Segments = new HashSet<Segment>();

            int radius = 8;
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

            Rand r = new Rand();
            radius--;
            for (int i = 0; i < 2 * Math.PI * radius; i++) {
                Segments.Add(new Segment(this, new SegmentDesc(
                    XY.Polar(2 * Math.PI * i / circumference, radius),
                    new ColoredGlyph(
                        Color.Violet.SetAlpha((byte)(204 + r.NextInteger(-51, 51))),
                        Color.Blue.SetAlpha((byte)(204 + r.NextInteger(-51, 51))),
                        '#')
                    )));
            }
            for (int x = -radius + 1; x < radius; x++) {
                for (int y = -radius + 1; y < radius; y++) {
                    if (x * x + y * y <= radius*radius) {
                        Segments.Add(new Segment(this, new SegmentDesc(
                            new XY(x, y),
                            new ColoredGlyph(
                                Color.BlueViolet.SetAlpha((byte)(204 + r.NextInteger(-51, 51))),
                                Color.DarkBlue.SetAlpha((byte)(204 + r.NextInteger(-51, 51))), '%')
                        )));
                    }
                }
            }

            foreach (var s in Segments) {
                world.AddEntity(s);
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
