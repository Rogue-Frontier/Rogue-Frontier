using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TranscendenceRL {
    public class SystemType : DesignType {
        public string codename;
        public string name;
        public SystemGroup system;
        public SystemType() {

        }
        public void Initialize(TypeCollection collection, XElement e) {
            codename = e.ExpectAttribute("codename");
            name = e.ExpectAttribute("name");
            if(e.HasElement("System", out var xmlSystem)) {
                system = new SystemGroup(e);
            }
        }
        public void Generate(World world) {
            system.Generate(new LocationContext() {
            pos = new XY(0, 0),
            focus = new XY(0, 0),
            world = world,
            angle = 0,
            radius = 0
            }, world.types);
        }
    }
    public struct LocationContext {
        public World world;
        public XY pos;
        public double angle;
        public double radius;
        public XY focus;
    }
    public interface SystemElement {
        void Generate(LocationContext lc, TypeCollection tc);
    }
    public static class SSystemElement {
        public static SystemElement Create(XElement e) {
            switch(e.Name.LocalName) {
                case "System":
                case "Group":
                    return new SystemGroup(e);
                case "Orbital":
                    return new SystemOrbital(e);
                case "Planet":
                    return new SystemPlanet(e);
                case "Sibling":
                    return new SystemSibling(e);
                case "Star":
                    return new SystemStar(e);
                case "Station":
                    return new SystemStation(e);
                case "Marker":
                    return new SystemMarker(e);
                default:
                    throw new Exception($"Unknown system element <{e.Name}>");
            }
        }
    }
    public class SystemGroup : SystemElement {
        int radius;
        List<SystemElement> subelements;
        public SystemGroup(XElement e) {
            radius = e.TryAttributeInt(nameof(radius), 0);
            subelements = e.Elements().Select(sub => SSystemElement.Create(sub)).ToList();
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var sub_lc = new LocationContext() {
                world = lc.world,
                focus = lc.focus,
                angle = lc.angle,
                radius = radius,
                pos = lc.focus + XY.Polar(lc.angle * Math.PI / 180, radius)
            };
            subelements.ForEach(g => g.Generate(sub_lc, tc));
        }
    }
    public class SystemOrbital : SystemElement {
        private List<SystemElement> subelements;
        private int angle;
        private AngleType angleType;
        private enum AngleType {
            Constant, Random, Equidistant, Incrementing
        }
        private int increment;

        private int radius;
        public SystemOrbital(XElement e) {
            subelements = e.Elements().Select(sub => SSystemElement.Create(sub)).ToList();
            switch (e.ExpectAttribute("angle")) {
                case "random":
                    angleType = AngleType.Random;
                    break;
                case "equidistant":
                    angleType = AngleType.Equidistant;
                    break;
                case "incrementing":
                    angleType = AngleType.Incrementing;
                    increment = e.ExpectAttributeInt("increment");
                    break;
                case var i when int.TryParse(i, out var angle):
                    angleType = AngleType.Constant;
                    this.angle = angle;
                    break;
                case var unknown:
                    throw new Exception($"Invalid angle {unknown}");
            }
            radius = e.TryAttributeInt(nameof(radius), 0);
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var angle = this.angle;
            var increment = this.increment;
            int equidistantInterval = 360 / subelements.Count;
            switch (angleType) {
                case AngleType.Constant:
                    increment = 0;
                    break;
                case AngleType.Equidistant:
                    angle = lc.world.karma.Next(360);
                    break;
                case AngleType.Incrementing:
                    break;
                case AngleType.Random:
                    angle = lc.world.karma.Next(360);
                    break;
            }
            foreach(var sub in subelements) {
                var loc = new LocationContext() {
                    world = lc.world,
                    focus = lc.pos,
                    angle = angle,
                    radius = radius,
                    pos = lc.pos + XY.Polar(angle * Math.PI / 180, radius)
                };
                sub.Generate(loc, tc);

                switch(angleType) {
                    case AngleType.Equidistant:
                        angle += equidistantInterval;
                        break;
                    case AngleType.Incrementing:
                        angle += increment;
                        break;
                    case AngleType.Random:
                        angle = lc.world.karma.Next(360);
                        break;
                }
            }
        }
    }
    public class SystemMarker : SystemElement {
        string name;
        public SystemMarker(XElement e) {
            name = e.ExpectAttribute("name");
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            lc.world.AddEntity(new Marker(name, lc.pos));
        }
    }
    public class SystemNebula : SystemElement {
        public SystemNebula(XElement e) {

        }
        public void Generate(LocationContext lc, TypeCollection tc) {

        }
    }
    public class SystemPlanet : SystemElement {
        private int radius;
        private bool showOrbit;
        public SystemPlanet(XElement e) {
            radius = e.ExpectAttributeInt(nameof(radius));
            showOrbit = e.TryAttributeBool(nameof(showOrbit), false);
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var diameter = radius * 2;
            var radius2 = radius * radius;
            var center = new XY(radius, radius);

            var r = lc.world.karma;
            ColoredGlyph[,] tiles = new ColoredGlyph[diameter, diameter];
            for(int x = 0; x < diameter; x++) {
                var xOffset = Math.Abs(x - radius);
                var yRange = Math.Sqrt(radius2 - (xOffset * xOffset));
                var yStart = radius - (int)Math.Round(yRange, MidpointRounding.AwayFromZero);
                var yEnd = radius + yRange;
                for(int y = yStart; y < yEnd; y++) {
                    var pos = lc.pos + (new XY(x, y) - center);

                    var f = Color.LightBlue;
                    f = f.Blend(Color.DarkBlue.SetAlpha((byte)r.Next(0, 153)));
                    f = f.Blend(Color.Gray.SetAlpha(102));

                    var tile = new ColoredGlyph(f, Color.Black, '%');
                    lc.world.backdrop.planets.tiles[pos] = tile;
                    tiles[x, y] = tile;
                    //lc.world.AddEffect(new FixedTile(tile, pos));
                }
            }

            var circ = radius * 2 * Math.PI;
            for (int x = 0; x < diameter; x++) {
                var xOffset = Math.Abs(x - radius);
                var yRange = Math.Sqrt(radius2 - (xOffset * xOffset));
                var yStart = radius - (int)Math.Round(yRange, MidpointRounding.AwayFromZero);
                var yEnd = radius + yRange;
                for (int y = yStart; y < yEnd; y++) {
                    var loc = r.NextDouble() * circ * (radius - 2);
                    var from = center + XY.Polar(loc % 2 * Math.PI, loc / circ);
                    var t = tiles[x, y];
                    t.Foreground = t.Foreground.Blend(tiles[from.xi, from.yi].Foreground.SetAlpha((byte)r.Next(0, 51)));
                }
            }
            /*
            var orbitFocus = lc.focus;
            var orbitRadius = lc.radius;
            var orbitCirc = orbitRadius * 2 * Math.PI;
            for (int i = 0; i < orbitCirc; i++) {
                var angle = i / orbitRadius;
                lc.world.backdrop.orbits.tiles[orbitFocus + XY.Polar(angle, orbitRadius)] = new ColoredGlyph(Color.White, Color.Transparent, '.');
            }
            */
        }
    }
    public class SystemSibling : SystemElement {
        public int arcInc;
        public int angleInc;
        public int radiusInc;

        public List<SystemElement> subelements;
        public SystemSibling(XElement e) {
            arcInc = e.TryAttributeInt(nameof(arcInc), 0);
            angleInc = e.TryAttributeInt(nameof(angleInc), 0);
            radiusInc = e.TryAttributeInt(nameof(radiusInc), 0);

            subelements = e.Elements().Select(sub => SSystemElement.Create(sub)).ToList();
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var angle = lc.angle + angleInc + arcInc / lc.radius;
            var radius = lc.radius + radiusInc;
            var sub_lc = new LocationContext() {
                world = lc.world,
                focus = lc.focus,
                angle = angle,
                radius = radius,
                pos = lc.focus + XY.Polar(angle * Math.PI / 180, radius)
            };
            subelements.ForEach(s => s.Generate(sub_lc, tc));
        }
    }
    public class SystemStar : SystemElement {
        private int radius;
        public SystemStar(XElement e) {
            this.radius = e.ExpectAttributeInt("radius");
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            /*
            var diameter = radius * 2;
            var radius2 = radius * radius;
            var center = new XY(radius, radius);
            for (int x = 0; x < diameter; x++) {
                var xOffset = Math.Abs(x - radius);
                var yRange = Math.Sqrt(radius2 - (xOffset * xOffset));
                var yStart = Math.Round(yRange, MidpointRounding.AwayFromZero);
                for (int y = -(int)yStart; y < yRange; y++) {
                    var pos = new XY(x, y);
                    var offset = (pos - center);
                    var tile = new ColoredGlyph(Color.Gray, Color.Black, '%');
                    lc.world.AddEffect(new FixedTile(tile, lc.pos + offset));
                }
            }
            */
            lc.world.backdrop.starlight.layers.Insert(0, new GeneratedLayer(1, new GeneratedGrid<ColoredGlyph>(p => {
                var xy = new XY(p);
                return new ColoredGlyph(Color.Transparent, new Color(255, 255, 204, Math.Min(255, (int) (radius * 255 / ((lc.pos - p).Magnitude + 1)))));
            })));
        }
    }
    public class SystemStation : SystemElement {
        string codename;
        public SystemStation(XElement e) {
            codename = e.ExpectAttribute("codename");
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var stationtype = tc.Lookup<StationType>(codename);
            var s = new Station(lc.world, stationtype, lc.pos);
            lc.world.AddEntity(s);
            s.CreateSegments();
            s.CreateGuards();
        }
    }

}
