using Common;
using System.Collections.Generic;
using System.Xml.Linq;
using Color = SadRogue.Primitives.Color;
using System;
using SadConsole;
using System.Linq;
using ASECII;

namespace RogueFrontier;

public class StationType : IDesignType {
    [Req] public string codename;
    [Req] public string name;
    [Req] public int hp;
    [Opt] public bool crimeOnDestroy;
    [Opt] public double stealth;
    public FragmentDesc explosionType;

    public Station.Behaviors behavior;
    public Sovereign Sovereign;

    public StaticTile tile;
    public List<SegmentDesc> segments;
    public List<XY> dockPoints;


    public Group<Item> cargo;
    public WeaponList weapons;


    public ConstructionDesc construction;
    public ShipGroup ships;
    public SystemGroup satellites;

    public Dictionary<(int, int), ColoredGlyph> heroImage;

    public void Initialize(TypeCollection tc, XElement e) {
        e.Initialize(this);
        explosionType = e.TryAtt(nameof(explosionType), out var s)
            ? tc.Lookup<ItemType>(s).weapon.projectile ?? throw new Exception($"Expected Weapon desc")
            : explosionType;
        explosionType = e.HasElement("Explosion", out var xmlExplosion) ? new FragmentDesc(xmlExplosion) : explosionType;

        behavior = e.TryAttEnum(nameof(behavior), Station.Behaviors.none);
        Sovereign = tc.Lookup<Sovereign>(e.ExpectAtt("sovereign"));
        dockPoints = new();

        if (e.HasElement("Weapons", out var xmlWeapons)) {
            weapons = new(xmlWeapons);
        }

        if(e.TryAtt("structure", out var structure)) {
            var sprite = ASECIILoader.LoadCG(structure).ToDictionary(
                pair => (pair.Key.Item1, -pair.Key.Item2),
                pair => pair.Value.cg);
            tile = sprite.TryGetValue((0, 0), out var cg) ? new(cg) : null;
            sprite.Remove((0, 0));
            segments = new(sprite.Select((pair) => new SegmentDesc(new(pair.Key), pair.Value)));
        } else {
            tile = new(e);
            if (e.HasElement("Segments", out var xmlSegments)) {
                segments = new();
                foreach (var xmlSegment in xmlSegments.Elements()) {
                    switch (xmlSegment.Name.LocalName) {
                        case "MultiPoint": {
                                var t = new StaticTile(xmlSegment);
                                int angleInc = xmlSegment.ExpectAttInt("angleInc");
                                var x = xmlSegment.ExpectAttDouble("offsetX");
                                var y = xmlSegment.ExpectAttDouble("offsetY");
                                XY offset = new XY(x, y);
                                for (int angle = 0; angle < 360; angle += angleInc) {
                                    segments.Add(new SegmentDesc(offset.Rotate(angle * Math.PI / 180), t));
                                }
                                break;
                            }
                        case "Line": {
                                var t = new StaticTile(xmlSegment);
                                var d = xmlSegment.ExpectAttDouble;
                                var td = xmlSegment.TryAttDouble2;

                                var x = d("offsetX");
                                var y = d("offsetY");
                                if (td("deltaX", out var dx, 0) | td("deltaY", out var dy, 0)) {
                                    segments.AddRange(new XY(x, y)
                                        .LineTo(new(x + dx, y + dy))
                                        .Select(p => new SegmentDesc(p, t)));
                                } else if (td("width", out var w, 1) | td("height", out var h, 1)) {
                                    segments.AddRange(new XY(x - w / 2, y - h / 2)
                                        .LineTo(new(x + w / 2, y + h / 2))
                                        .Select(p => new SegmentDesc(p, t)));
                                }
                                /*
                                var fromX = d("fromOffsetX");
                                var fromY = d("fromOffsetY");
                                var toX = d("toOffsetX");
                                var toY = d("toOffsetY");
                                segments.AddRange(new XY(fromX, fromY)
                                    .LineTo(new(toX, toY))
                                    .Select(p => new SegmentDesc(p, t)));
                                */

                                break;
                            }
                        case "Ring": {
                                var f = xmlSegment.TryAttColor("foreground", Color.White);
                                var b = xmlSegment.TryAttColor("background", Color.Transparent);
                                segments.AddRange(CreateRing(f, b));
                                break;
                            }
                        case "Box": {
                                string foreground = xmlSegment.TryAtt("foreground", "White");
                                string background = xmlSegment.TryAtt("background", "Transparent");
                                segments.AddRange(CreateBox(foreground, background));
                                break;
                            }
                        case "Point":
                            segments.Add(new(xmlSegment));
                            break;
                    }
                }
            }
        }

        if (e.HasElement("Satellites", out var xmlSatellites)) {
            satellites = new(xmlSatellites, (XElement e) => SSystemElement.Create(tc, e));
        }
        if (e.HasElement("Dock", out var xmlDock)) {
            foreach (var xmlPart in xmlDock.Elements()) {
                switch (xmlPart.Name.LocalName) {
                    case "MultiPoint": {
                            int angleInc = xmlPart.ExpectAttInt("angleInc");
                            var x = xmlPart.ExpectAttDouble("offsetX");
                            var y = xmlPart.ExpectAttDouble("offsetY");
                            var offset = new XY(x, y);
                            for (int angle = 0; angle < 360; angle += angleInc) {
                                dockPoints.Add(offset.Rotate(angle * Math.PI / 180));
                            }
                            break;
                        }
                    case "Ring": {
                            dockPoints.AddRange(CreateRing());
                            break;
                        }
                    case "Point": {
                            var x = xmlPart.ExpectAttDouble("offsetX");
                            var y = xmlPart.ExpectAttDouble("offsetY");
                            dockPoints.Add(new(x, y));
                            break;
                        }
                }
            }
        }
        if (e.HasElement("Cargo", out XElement xmlCargo) || e.HasElement("Items", out xmlCargo)) {
            cargo = new(xmlCargo, SGenerator.ParseFrom(tc, SGenerator.ItemFrom));
        }

        construction = e.HasElement("Construction", out var xmlConstruction) ? new ConstructionDesc(tc, xmlConstruction) : null;

        if (e.HasElement("Ships", out var xmlShips)) {
            ships = new(xmlShips, SGenerator.ParseFrom(tc, SGenerator.ShipFrom));
        }
        if (e.HasElement("HeroImage", out var heroImage)) {
            if (heroImage.TryAtt("path", out string path)) {
                this.heroImage = ColorImage.FromFile(path).Sprite;
            } else {
                var heroImageText = heroImage.Value.Trim('\n').Replace("\r\n", "\n").Split('\n');
                var heroImageTint = heroImage.TryAttColor("tint", Color.White);
                this.heroImage = heroImageText.ToImage(heroImageTint);
            }
        }

    }
    public static List<SegmentDesc> CreateRing(Color? f = null, Color? b = null) {
        f ??= Color.White;
        b ??= Color.Black;
        SegmentDesc Create(int x, int y, char c) =>
            new SegmentDesc(new XY(x, y), new StaticTile(c, f.Value, b.Value));
        return new() {
            Create(0, 1, '-'),
            Create(1, 1, '\\'),
            Create(1, 0, '|'),
            Create(1, -1, '/'),
            Create(0, -1, '-'),
            Create(-1, -1, '\\'),
            Create(-1, 0, '|'),
            Create(-1, 1, '/')
        };
    }
    public static List<SegmentDesc> CreateBox(string foreground = "White", string background = "Black") {
        SegmentDesc Create(int x, int y, char c) =>
            new SegmentDesc(new XY(x, y), new StaticTile(c, foreground, background));
        return new() {
            Create(0, 1,   '-'),
            Create(1, 1,   '+'),
            Create(1, 0,   '|'),
            Create(1, -1,  '+'),
            Create(0, -1,  '-'),
            Create(-1, -1, '+'),
            Create(-1, 0,  '|'),
            Create(-1, 1,  '+')
        };
    }

    public static List<XY> CreateRing() {
        return new() {
                                new XY(0, 1),
                                new XY(1, 1),
                                new XY(1, 0),
                                new XY(1, -1),
                                new XY(0, -1),
                                new XY(-1, -1),
                                new XY(-1, 0),
                                new XY(-1, 1)
                            };
    }

    public class SegmentDesc {
        public XY offset;
        public StaticTile tile;
        public SegmentDesc() {

        }
        public SegmentDesc(XY offset, StaticTile tile) {
            this.offset = offset;
            this.tile = tile;
        }
        public SegmentDesc(XElement e) {
            var x = e.ExpectAttDouble("offsetX");
            var y = e.ExpectAttDouble("offsetY");
            offset = new(x, y);
            tile = new(e);
        }
    }

    public record ConstructionDesc {
        [Opt]public int max = int.MaxValue;
        public List<ConstructionEntry> catalog;
        public ConstructionDesc() {
        }
        public ConstructionDesc(TypeCollection tc, XElement e) {
            e.Initialize(this);
            catalog = e.Elements("Construct").Select(s => new ConstructionEntry(tc, s)).ToList();
        }
    }
    public record ConstructionEntry {
        [Req] public int time;
        public ShipClass type;
        public ShipEntry.IShipOrderDesc order;
        public ConstructionEntry() { }
        public ConstructionEntry(TypeCollection tc, XElement e) {
            e.Initialize(this);
            type = tc.Lookup<ShipClass>(e.ExpectAtt("codename"));
            order = ShipEntry.IShipOrderDesc.Get(e);
        }
        public static implicit operator ConstructionJob(ConstructionEntry e) => new() { time=e.time, desc=e};
    }

    public class ConstructionJob {
        public int time;
        public ConstructionEntry desc;
    }
}
