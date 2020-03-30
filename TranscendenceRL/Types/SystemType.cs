using Common;
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
                case "Orbital":
                    return new SystemOrbital(e);
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
        private AngleGenerator angleGenerator;
        private int radius;
        public SystemOrbital(XElement e) {
            subelements = e.Elements().Select(sub => SSystemElement.Create(sub)).ToList();
            switch (e.ExpectAttribute("angle")) {
                case "equidistant":
                    angleGenerator = new Equidistant(e.Elements().Count());
                    break;
                case "incrementing":
                    angleGenerator = new Incrementing(e.ExpectAttributeInt("inc"));
                    break;
                case var i when int.TryParse(i, out var angle):
                    angleGenerator = new Constant(angle);
                    break;
            }
            radius = e.ExpectAttributeInt("radius");
        }
        public void Generate(LocationContext lc, TypeCollection tc) {
            var result = new List<SpaceObject>();
            foreach(var sub in subelements) {
                var angle = angleGenerator.GetAngle();
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

        interface AngleGenerator {
            int GetAngle();
        }
        class Constant : AngleGenerator {
            private int degrees;
            public Constant(int degrees) {
                this.degrees = degrees;
            }
            public int GetAngle() => degrees;
        }
        class Equidistant : AngleGenerator {
            private int interval;
            public int i;
            public Equidistant(int count) {
                this.interval = 360 / count;
                this.i = 0;
            }
            public int GetAngle() {
                return interval * i++;
            }
        }
        class Incrementing : AngleGenerator {
            private int start;
            private int inc;
            public Incrementing(int inc) {
                this.start = new Random().Next(360);
                this.inc = inc;
            }
            public int GetAngle() {
                return start += inc;
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
            lc.world.AddEntity(new Station(lc.world, stationtype, lc.pos));
        }
    }
}
