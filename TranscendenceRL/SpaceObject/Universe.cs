using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TranscendenceRL {
    public class UniverseDesc {
        public List<SystemDesc> systems;
        public List<LinkDesc> links;
        public UniverseDesc(XElement e) {
            systems = new();
            links = new();

            if (e.HasElement("Topology", out var xmlTopology)) {
                foreach (var element in xmlTopology.Elements()) {
                    switch (element.Name.LocalName) {
                        case "System":
                            systems.Add(new SystemDesc(element));
                            break;
                        case "Link":
                            links.Add(new LinkDesc(element));
                            break;
                    }
                }
            }
            if (e.HasElement("Links", out var xmlLinks)) {
                foreach (var element in xmlLinks.Elements()) {
                    links.Add(new LinkDesc(element));
                }
            }
        }

        public class SystemDesc {
            public string id;
            public string name;
            public SystemGroup systemGroup;
            public string codename;

            public List<GlobalStargateDesc> globalStargates;

            public SystemDesc() { }
            public SystemDesc(XElement e) {
                id = e.ExpectAttribute(nameof(id));
                name=e.ExpectAttribute(nameof(name));
                if(e.HasElement("SystemGroup", out var xmlSystemGroup)) {
                    systemGroup=new SystemGroup(xmlSystemGroup);
                }
                codename=e.TryAttribute(nameof(codename));
                globalStargates = new();
                foreach (var g in e.Elements("GlobalStargate")) {
                    globalStargates.Add(new GlobalStargateDesc(g));
                }
            }
        }
        public class GlobalStargateDesc {
            public string globalId;
            public string gateId;
            public GlobalStargateDesc() {}
            public GlobalStargateDesc(XElement e) {
                globalId = e.ExpectAttribute(nameof(globalId));
                gateId = e.ExpectAttribute(nameof(gateId));
            }
        }
        public class LinkDesc {
            public string fromGateId;
            public string toGateId;
            public LinkDesc() { }
            public LinkDesc(XElement e) {
                fromGateId = e.ExpectAttribute(nameof(fromGateId));
                toGateId = e.ExpectAttribute(nameof(toGateId));
            }
        }
    }
    public class Universe {
        public Rand karma;
        public TypeCollection types;

        public Dictionary<string, System> systems;
        public Dictionary<string, Stargate> stargates;
        public Universe(TypeCollection types = null, Rand karma = null) {
            this.types = types ?? new TypeCollection();
            this.karma = karma ?? new Rand();
            systems = new();
        }
        public Universe(UniverseDesc desc, TypeCollection types = null, Rand karma = null) : this(types, karma) {
            systems = new();
            stargates = new();
            foreach(var s in desc.systems) {
                System sys = new System(this) {  id = s.id, name = s.name };
                systems[s.id] = sys;
            }
            //Record all the system stargates
            foreach(var s in desc.systems) {
                var sys = systems[s.id];
                types.Lookup<SystemType>(s.codename).Generate(sys);
                sys.UpdatePresent();
                foreach (var g in sys.entities.all.OfType<Stargate>()) {
                    stargates[$"{s.id}:{g.gateId}"] = g;
                }
            }
            //Record any global stargates
            foreach (var s in desc.systems) {
                var sys = systems[s.id];
                Dictionary<string, Stargate> gateLookup = new();
                foreach (var g in sys.entities.all.OfType<Stargate>()) {
                    gateLookup[g.gateId] = g;
                }

                foreach (var g in s.globalStargates) {
                    stargates[g.globalId] = gateLookup[g.gateId];
                }
            }
            //Build all local links
            foreach (var s in desc.systems) {
                var sys = systems[s.id];
                foreach (var g in sys.entities.all.OfType<Stargate>()) {
                    if (g.destGateId.Any()) {
                        g.destGate = stargates[g.destGateId];
                    }
                }
            }
            //Build links
            foreach (var l in desc.links) {
                Stargate fromGate = stargates[l.fromGateId], toGate = stargates[l.toGateId];
                fromGate.destGate = toGate;
                toGate.destGate = fromGate;
            }
        }
    }
}
