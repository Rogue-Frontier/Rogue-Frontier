using Common;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RogueFrontier;

public class UniverseDesc {
    public List<SystemDesc> systems;
    public List<LinkDesc> links;
    public UniverseDesc(TypeCollection tc, XElement e) {
        systems = new();
        links = new();

        if (e.HasElement("Topology", out var xmlTopology)) {
            foreach (var element in xmlTopology.Elements()) {
                switch (element.Name.LocalName) {
                    case "System":
                        systems.Add(new SystemDesc(tc, element));
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
        public SystemDesc(TypeCollection tc, XElement e) {
            id = e.ExpectAtt(nameof(id));
            name = e.ExpectAtt(nameof(name));
            if (e.HasElement("SystemGroup", out var xmlSystemGroup)) {
                systemGroup = new SystemGroup(xmlSystemGroup, SGenerator.ParseFrom(tc, SSystemElement.Create));
            }
            codename = e.TryAtt(nameof(codename));
            globalStargates = new();
            foreach (var g in e.Elements("GlobalStargate")) {
                globalStargates.Add(new GlobalStargateDesc(g));
            }
        }
    }
    public class GlobalStargateDesc {
        public string globalId;
        public string gateId;
        public GlobalStargateDesc() { }
        public GlobalStargateDesc(XElement e) {
            globalId = e.ExpectAtt(nameof(globalId));
            gateId = e.ExpectAtt(nameof(gateId));
        }
    }
    public class LinkDesc {
        public string fromGateId;
        public string toGateId;
        public LinkDesc() { }
        public LinkDesc(XElement e) {
            fromGateId = e.ExpectAtt(nameof(fromGateId));
            toGateId = e.ExpectAtt(nameof(toGateId));
        }
    }
}
public class Universe {
    public Rand karma;
    public TypeCollection types;

    public Dictionary<string, Entity> named=new();
    public Dictionary<string, System> systems=new();
    public Dictionary<string, Stargate> stargates=new();
    public Dictionary<System, HashSet<Stargate>> systemGates=new();
    public Universe(TypeCollection types = null, Rand karma = null) {
        this.types = types ?? new TypeCollection();
        this.karma = karma ?? new Rand();
    }
    public Universe(UniverseDesc desc, TypeCollection types = null, Rand karma = null) : this(types, karma) {
        foreach (var s in desc.systems) {
            System sys = new System(this) { id = s.id, name = s.name };
            systems[s.id] = sys;
        }
        //Record all the system stargates
        foreach (var s in desc.systems) {
            var sys = systems[s.id];
            types.Lookup<SystemType>(s.codename).Generate(sys);
            sys.UpdatePresent();

            var gates = sys.entities.all.OfType<Stargate>();
            systemGates[sys] = new(gates);
            foreach (var g in gates) {
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

        var _ = FindGateTo(systems.Values.First(), systems.Values.Last());
    }
    public Stargate FindGateTo(System from, System to) {
        Dictionary<System, Stargate> gateTo = new();
        HashSet<System> visited = new();
        visited.Add(from);
        Queue<System> q = new();
        q.Enqueue(from);
        while (q.Any()) {
            var top = q.Dequeue();

            foreach (var g in systemGates[top].Where(g => g.destGate != null)) {
                if (visited.Add(g.destGate.world)) {
                    gateTo[g.destGate.world] = g;
                    q.Enqueue(g.destGate.world);
                }
            }
            if (top == to) {
                var g = gateTo[to];
                while (g.world != from) {
                    g = gateTo[g.world];
                }
                return g;
            }
        }
        return null;
    }
    public IEnumerable<Entity> GetAllEntities() =>
        systems.Values.SelectMany(s => s.entities.all);
}
