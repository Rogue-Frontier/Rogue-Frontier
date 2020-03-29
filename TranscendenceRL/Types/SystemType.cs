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
        public XY prev;
    }
    public interface SystemElement {
        List<SpaceObject> Generate(LocationContext lc, TypeCollection tc);
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

        }
        public List<SpaceObject> Generate(LocationContext lc, TypeCollection tc) {
            var result = new List<SpaceObject>();
            subelements.ForEach(g => result.AddRange(g.Generate(lc, tc)));
            return result;
        }
    }
    public class SystemOrbital : SystemElement {
        List<SystemElement> subelements;
        public SystemOrbital(XElement e) {
            subelements = e.Elements().Select(sub => SSystemElement.Create(sub)).ToList();
        }
        public List<SpaceObject> Generate(LocationContext lc, TypeCollection tc) {
            //Modify the LocationContext
            //TO DO

            var result = new List<SpaceObject>();
            subelements.ForEach(g => result.AddRange(g.Generate(lc, tc)));
            return result;
        }
    }
    public class SystemStation : SystemElement {
        string codename;
        public SystemStation(XElement e) {
            codename = e.ExpectAttribute("codename");
        }
        public List<SpaceObject> Generate(LocationContext lc, TypeCollection tc) {
            var stationtype = tc.Lookup<StationType>(codename);
            return new List<SpaceObject>() { new Station(lc.world, stationtype, lc.pos) };
        }
    }
}
