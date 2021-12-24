using Common;
using System.Collections.Generic;
using System.Xml.Linq;
using Color = SadRogue.Primitives.Color;
using System;
using SadConsole;

namespace RogueFrontier;

public enum StationBehaviors {
    none,
    raisu,
    pirate,
    reinforceNearby
}
public class StationType : DesignType {
    public string codename;
    public string name;
    public int hp;
    public bool crimeOnDestroy;
    public StationBehaviors behavior;
    public Sovereign Sovereign;
    public StaticTile tile;
    public ItemList cargo;
    public WeaponList weapons;

    public List<SegmentDesc> segments;
    public List<XY> dockPoints;

    public ShipList guards;

    public Dictionary<(int, int), ColoredGlyph> heroImage;

    public void Initialize(TypeCollection collection, XElement e) {
        codename = e.ExpectAtt("codename");
        name = e.ExpectAtt("name");
        hp = e.ExpectAttInt("hp");
        crimeOnDestroy = e.TryAttBool(nameof(crimeOnDestroy));
        behavior = e.TryAttEnum(nameof(behavior), StationBehaviors.none);
        Sovereign = collection.Lookup<Sovereign>(e.ExpectAtt("sovereign"));
        tile = new StaticTile(e);
        segments = new();
        dockPoints = new();

        if (e.HasElement("Segments", out var xmlSegments)) {
            foreach (var xmlSegment in xmlSegments.Elements()) {
                switch (xmlSegment.Name.LocalName) {
                    case "MultiPoint":
                        var t = new StaticTile(xmlSegment);
                        int angleInc = xmlSegment.ExpectAttInt("angleInc");
                        var x = xmlSegment.ExpectAttDouble("offsetX");
                        var y = xmlSegment.ExpectAttDouble("offsetY");
                        XY offset = new XY(x, y);


                        for (int angle = 0; angle < 360; angle += angleInc) {
                            segments.Add(new SegmentDesc(offset.Rotate(angle * Math.PI / 180), t));
                        }
                        break;
                    case "Ring":
                        string foreground = xmlSegment.TryAtt("foreground", "White");
                        string background = xmlSegment.TryAtt("background", "Transparent");
                        segments.AddRange(CreateRing(foreground, background));
                        break;
                    case "Point":
                        segments.Add(new SegmentDesc(xmlSegment));
                        break;
                }
            }
        }
        if (e.HasElement("Dock", out var xmlDock)) {
            foreach (var xmlPart in xmlDock.Elements()) {
                switch (xmlPart.Name.LocalName) {
                    case "MultiPoint": {
                            int angleInc = xmlPart.ExpectAttInt("angleInc");
                            var x = xmlPart.ExpectAttDouble("offsetX");
                            var y = xmlPart.ExpectAttDouble("offsetY");
                            XY offset = new XY(x, y);
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
                            dockPoints.Add(new XY(x, y));
                            break;
                        }
                }
            }
        }
        if (e.HasElement("Cargo", out XElement xmlCargo) || e.HasElement("Items", out xmlCargo)) {
            cargo = new ItemList(xmlCargo);
        }
        if (e.HasElement("Weapons", out var xmlWeapons)) {
            weapons = new WeaponList(xmlWeapons);
        }
        if (e.HasElement("Guards", out var xmlGuards)) {
            guards = new ShipList(xmlGuards);
        }
        if (e.HasElement("HeroImage", out var heroImage)) {
            if (heroImage.TryAttribute("path", out string path)) {
                this.heroImage = ColorImage.FromFile(path).Sprite;
            } else {
                var heroImageText = heroImage.Value.Trim('\n').Replace("\r\n", "\n").Split('\n');
                var heroImageTint = heroImage.TryAttColor("tint", Color.White);
                this.heroImage = heroImageText.ToImage(heroImageTint);
            }
        }

    }
    public static List<SegmentDesc> CreateRing(string foreground = "White", string background = "Black") {
        SegmentDesc Create(int x, int y, char c) {
            return new SegmentDesc(new XY(x, y), new StaticTile(c, foreground, background));
        }
        return new List<SegmentDesc> {
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

    public static List<XY> CreateRing() {
        return new List<XY> {
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
            offset = new XY(x, y);
            tile = new StaticTile(e);
        }
    }
}
