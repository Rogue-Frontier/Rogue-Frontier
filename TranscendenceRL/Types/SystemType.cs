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
        public int? radius;
        public List<SystemElement> subelements;
        public SystemGroup() { }
        public SystemGroup(XElement e) {
            radius = e.TryAttributeIntOptional(nameof(radius));
            subelements = e.Elements().Select(sub => SSystemElement.Create(sub)).ToList();
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var sub_lc = new LocationContext() {
                world = lc.world,
                focus = lc.focus,
                angle = lc.angle,
                radius = radius ?? lc.radius,
                pos = lc.focus + XY.Polar(lc.angle * Math.PI / 180, radius ?? lc.radius)
            };
            subelements.ForEach(g => g.Generate(sub_lc, tc));
        }
    }
    public class SystemOrbital : SystemElement {
        public List<SystemElement> subelements;

        public int count;

        public int angle;
        public bool randomAngle;

        public int increment;
        public bool randomInc;
        public bool equidistant;

        public int radius;
        public SystemOrbital() { }
        public SystemOrbital(XElement e) {
            subelements = e.Elements().Select(sub => SSystemElement.Create(sub)).ToList();
            count = e.TryAttributeInt(nameof(count), 1);
            switch (e.ExpectAttribute(nameof(angle))) {
                case "random":
                    angle = 0;
                    randomAngle = true;
                    //Default to random increment
                    randomInc = true;
                    break;
                case var i when int.TryParse(i, out var a):
                    angle = a;
                    break;
                case var unknown:
                    throw new Exception($"Invalid angle {unknown}");
            }
            if (e.TryAttribute(nameof(increment), out string si)) {
                switch (si) {
                    case "random":
                        increment = 0;
                        randomInc = true;
                        break;
                    case "equidistant":
                        increment = 0;
                        equidistant = true;
                        break;
                    case var i when int.TryParse(i, out var inc):
                        increment = inc;
                        break;
                    case var unknown:
                        throw new Exception($"Invalid increment {unknown}");
                }
            }
            
            radius = e.ExpectAttributeInt(nameof(radius));
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var angle = this.angle;
            var increment = this.increment;
            int equidistantInterval = 360 / subelements.Count;

            if (randomAngle) {
                angle = lc.world.karma.NextInteger(360);
            }

            for (int i = 0; i < count; i++) {
                foreach (var sub in subelements) {
                    Generate(sub);

                    if (increment > 0) {
                        angle += increment;
                    } else if (randomInc) {
                        angle = lc.world.karma.NextInteger(360);
                    } else if (equidistant) {
                        angle += equidistantInterval;
                    }
                }
            }

            void Generate(SystemElement sub) {
                var loc = new LocationContext() {
                    world = lc.world,
                    focus = lc.pos,
                    angle = angle,
                    radius = radius,
                    pos = lc.pos + XY.Polar(angle * Math.PI / 180, radius)
                };
                sub.Generate(loc, tc);
            }
        }
    }
    public class SystemMarker : SystemElement {
        public string name;
        public SystemMarker() { }
        public SystemMarker(XElement e) {
            name = e.ExpectAttribute("name");
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            lc.world.AddEntity(new Marker(name, lc.pos));
        }
    }
    public class SystemNebula : SystemElement {
        public SystemNebula() { }
        public SystemNebula(XElement e) {

        }
        public void Generate(LocationContext lc, TypeCollection tc) {

        }
    }
    public class SystemPlanet : SystemElement {
        public int radius;
        public bool showOrbit;
        public SystemPlanet() { }
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
                    f = f.Blend(Color.DarkBlue.SetAlpha((byte)r.NextInteger(0, 153)));
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
                    t.Foreground = t.Foreground.Blend(tiles[from.xi, from.yi].Foreground.SetAlpha((byte)r.NextInteger(0, 51)));
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
        public SystemSibling() { }
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
    public class LightGenerator : IGridGenerator<Color> {
        public LocationContext lc;
        public int radius;
        public LightGenerator() { }
        public LightGenerator(LocationContext lc, int radius) {
            this.lc = lc;
            this.radius = radius;
        }
        public Color Generate((int,int) p) {
            //var xy = new XY(p);
            return new Color(255, 255, 204, Math.Min(255, (int)(radius * 255 / ((lc.pos - p).Magnitude + 1))));
        }
    }
    public class SystemStar : SystemElement {
        public int radius;
        public SystemStar() { }
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
            lc.world.backdrop.starlight.layers.Insert(0, new GeneratedGrid<Color>(new LightGenerator(lc, radius)));
        }
    }
    public class SystemStation : SystemElement {
        public string codename;
        public SystemStation() { }
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
