using Common;
using System.Collections.Generic;
using System.Xml.Linq;
using Color = SadRogue.Primitives.Color;
using System;
using SadConsole;
using System.Linq;

namespace RogueFrontier;

public enum EStationBehaviors {
    none,
    raisu,
    pirate,
    reinforceNearby,
    constellationShipyard
}
public class StationType : DesignType {
    [Req] public string codename;
    [Req] public string name;
    [Req] public int hp;
    [Opt] public bool crimeOnDestroy;
    public EStationBehaviors behavior;
    public Sovereign Sovereign;
    public StaticTile tile;
    public ItemList cargo;
    public WeaponList weapons;

    public List<SegmentDesc> segments;
    public List<XY> dockPoints;

    public ShipList ships;
    public SystemGroup satellites;

    public Dictionary<(int, int), ColoredGlyph> heroImage;

    public void Initialize(TypeCollection collection, XElement e) {
        e.Initialize(this);
        behavior = e.TryAttEnum(nameof(behavior), EStationBehaviors.none);
        Sovereign = collection.Lookup<Sovereign>(e.ExpectAtt("sovereign"));
        tile = new StaticTile(e);
        dockPoints = new();

        if (e.HasElement("Weapons", out var xmlWeapons)) {
            weapons = new(xmlWeapons);
        }
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
                            if(td("deltaX", out var dx, 0) | td("deltaY", out var dy, 0)) {
                                segments.AddRange(new XY(x, y)
                                    .LineTo(new(x + dx, y + dy))
                                    .Select(p => new SegmentDesc(p, t)));
                            } else if(td("width", out var w, 1) | td("height", out var h, 1)) {
                                segments.AddRange(new XY(x - w/2, y - h/2)
                                    .LineTo(new(x + w/2, y + h/2))
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
                            string foreground = xmlSegment.TryAtt("foreground", "White");
                            string background = xmlSegment.TryAtt("background", "Transparent");
                            segments.AddRange(CreateRing(foreground, background));
                            break;
                        }
                    case "Point":
                        segments.Add(new(xmlSegment));
                        break;
                }
            }
        }
        if (e.HasElement("Satellites", out var xmlSatellites)) {
            satellites = new(xmlSatellites);
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
            cargo = new(xmlCargo);
        }
        if (e.HasElement("Ships", out var xmlShips)) {
            ships = new(xmlShips);
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
    public static List<SegmentDesc> CreateRing(string foreground = "White", string background = "Black") {
        SegmentDesc Create(int x, int y, char c) =>
            new SegmentDesc(new XY(x, y), new StaticTile(c, foreground, background));
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
}
