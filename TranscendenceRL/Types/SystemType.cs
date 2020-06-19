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
        List<SystemElement> subelements;
        public SystemGroup(XElement e) {
            subelements = e.Elements().Select(sub => SSystemElement.Create(sub)).ToList();
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            subelements.ForEach(g => g.Generate(lc, tc));
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
            radius = e.ExpectAttributeInt("radius");
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var result = new List<SpaceObject>();

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
    public class SystemMarker : SystemElement {
        string name;
        public SystemMarker(XElement e) {
            name = e.ExpectAttribute("name");
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            lc.world.AddEntity(new Marker(name, lc.pos));
        }
    }
    public class SystemPlanet : SystemElement {
        private int radius;
        public SystemPlanet(XElement e) {
            this.radius = e.ExpectAttributeInt("radius");
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var diameter = radius * 2;
            var center = new XY(radius, radius);
            for(int x = 0; x < diameter; x++) {
                //Change to diameter when we get a square tileset
                for(int y = 0; y < diameter; y++) {
                    var pos = new XY(x, y);
                    var offset = (pos - center);
                    //Try to optimize using manhattan
                    if (/*offset.MaxCoord < radius || */offset.Magnitude < radius) {
                        var tile = new ColoredGlyph(Color.Gray, Color.Black, '%');
                        lc.world.AddEffect(new FixedTile(tile, lc.pos + offset));
                    }
                }
            }

        }
    }
}
