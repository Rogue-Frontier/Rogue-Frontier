using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using static RogueFrontier.StationType;
using Console = SadConsole.Console;
using Newtonsoft.Json;
using static RogueFrontier.Weapon;

namespace RogueFrontier;

public class Wreck : DockableObject {
    [JsonIgnore]
    public string name => $"Wreck of {creator.name}";
    [JsonIgnore]
    public ColoredGlyph tile => new ColoredGlyph(new Color(128, 128, 128), Color.Transparent, creator.tile.GlyphCharacter);

    [JsonProperty]
    public int id { get; private set; }
    [JsonProperty]
    public SpaceObject creator { get; private set; }
    [JsonProperty]
    public System world { get; private set; }
    [JsonProperty]
    public Sovereign sovereign { get; private set; }
    [JsonProperty]
    public XY position { get; private set; }
    [JsonProperty]
    public XY velocity { get; private set; }
    [JsonProperty]
    public bool active { get; private set; }
    [JsonProperty]
    public HashSet<Item> cargo { get; private set; }
    [JsonProperty]
    public int ticks { get; private set; }
    [JsonProperty]
    public XY gravity { get; private set; }
    public Wreck() { }
    public Wreck(SpaceObject creator, IEnumerable<Item> cargo = null) {
        this.id = creator.world.nextId++;
        this.creator = creator;


        this.world = creator.world;
        this.sovereign = Sovereign.Inanimate;
        this.position = creator.position;
        this.velocity = creator.velocity;
        this.active = true;
        this.cargo = new HashSet<Item>();
        if (cargo?.Any() == true) {
            this.cargo.UnionWith(cargo);
        }

        gravity = new XY(0, 0);
    }
    public Console GetDockScene(Console prev, PlayerShip playerShip) => new WreckScene(prev, playerShip, this);
    public void Damage(Projectile p) {
    }

    public void Destroy(SpaceObject source) {
        active = false;
    }

    public void Update() {
        position += velocity / Program.TICKS_PER_SECOND;

        ticks++;
        if (ticks % 30 == 0) {
            gravity = new XY(0, 0);
            double stress = 0;
            foreach (var star in world.stars) {
                var towards = (star.position - position);
                var magnitude = Math.Pow(star.radius, 2) / (towards.magnitude2 * Program.TICKS_PER_SECOND);
                var pull = towards.WithMagnitude(magnitude);
                gravity += pull;
                stress += magnitude;
            }
            if (stress > 10f / Program.TICKS_PER_SECOND) {
                Destroy(null);
            }
        }
        velocity += gravity;
    }
}
public interface StationBehavior {
    void Update(Station owner);
}
public class Station : DockableObject, ITrader {
    [JsonIgnore]
    public string name => type.name;
    [JsonProperty]
    public int id { get; set; }
    [JsonProperty]
    public System world { get; set; }
    [JsonProperty]
    public StationType type { get; set; }
    [JsonProperty]
    public Sovereign sovereign { get; set; }
    [JsonProperty]
    public XY position { get; set; }
    [JsonProperty]
    public XY velocity { get; set; }
    [JsonProperty]
    public bool active { get; set; }
    [JsonProperty]
    public StationBehavior behavior;
    [JsonProperty]
    public List<Segment> segments;
    [JsonProperty]
    public HullSystem damageSystem;
    [JsonProperty]
    public HashSet<Item> cargo { get; set; }
    [JsonProperty]
    public List<Weapon> weapons;
    [JsonProperty]
    public List<AIShip> guards;
    public delegate void Destroyed(Station station, SpaceObject destroyer, Wreck wreck);
    public FuncSet<IContainer<Destroyed>> onDestroyed = new();
    public Station() { }
    public Station(System World, StationType Type, XY Position) {
        this.id = World.nextId++;
        this.world = World;
        this.type = Type;
        this.position = Position;
        this.velocity = new XY();
        this.active = true;
        this.sovereign = Type.Sovereign;
        damageSystem = new HPSystem(Type.hp);
        cargo = new HashSet<Item>(Type.cargo?.Generate(World.types) ?? new List<Item>());
        weapons = type.weapons?.Generate(World.types);
        weapons?.ForEach(w => w.aiming = new Omnidirectional());
        InitBehavior(Type.behavior);
    }
    public void InitBehavior(StationBehaviors behavior) {
        this.behavior = behavior switch {
            StationBehaviors.raisu => null,
            StationBehaviors.pirate => new Pirate(),
            StationBehaviors.reinforceNearby => new ReinforceNearby(),
            StationBehaviors.none => null,
            _ => null
        };
    }
    public void CreateSegments() {
        segments = new List<Segment>();
        foreach (var segmentDesc in type.segments) {
            var s = new Segment(this, segmentDesc);
            segments.Add(s);
            world.AddEntity(s);
        }
    }
    public void CreateGuards() {
        guards = new List<AIShip>();
        if (type.guards != null) {
            //Suppose we should pass in the owner object
            var generated = type.guards.Generate(world.types, this);
            foreach (var guard in generated) {
                guards.Add(guard);
                world.AddEntity(guard);
                world.AddEffect(new Heading(guard));
            }
        }
    }
    public IEnumerable<AIShip> GetDocked() {
        return world.entities.GetAll(p => (position - p).magnitude < 5)
            .OfType<AIShip>().Where(s => s.dock?.Target == this);
    }
    public XY GetDockPoint() {
        return type.dockPoints.Except(GetDocked().Select(s => s.dock?.Offset)).FirstOrDefault() ?? XY.Zero;
    }
    public void UpdateGuardList() {
        guards = new List<AIShip>(world.entities.all.OfType<AIShip>()
            .Where(s => ((s.behavior as BaseShipBehavior)?.current ?? s.behavior) switch {
                GuardOrder g => g.GuardTarget == this,
                PatrolOrbitOrder p => p.patrolTarget == this,
                PatrolCircuitOrder p => p.patrolTarget == this,
                _ => false
            }));
    }
    /*
    public HashSet<SpaceObject> GetParts() {
        var set = new HashSet<SpaceObject>();
        set.Add(this);
        set.UnionWith(segments);
        set.UnionWith(guards);
        return set;
    }
    */
    public void Damage(Projectile p) {
        damageSystem.Damage(this, p);
        var source = p.source;
        if (source != null && source.sovereign != sovereign) {
            var guards = world.entities.all.OfType<AIShip>()
                .Select(s => s.behavior.GetOrder())
                .OfType<GuardOrder>()
                .Where(g => g.GuardTarget == this);
            foreach (var order in guards) {
                order.Attack(source, 300);
            }
        }
    }
    public void Destroy(SpaceObject source) {
        active = false;
        var wreck = new Wreck(this);
        if (source is PlayerShip ps) {
            ps.stationsDestroyed.Add(this);
            if (type.crimeOnDestroy) {
                ps.crimeRecord.Add(new Destruction(this));
            }
        }
        var drop = weapons?.Select(w => w.source);
        if (drop != null) {
            foreach (var item in drop) {
                item.Remove<Weapon>();
                wreck.cargo.Add(item);
            }
        }
        world.AddEntity(wreck);
        if (segments != null) {
            foreach (var segment in segments) {
                var offset = segment.desc.offset;
                var tile = new ColoredGlyph(new Color(128, 128, 128), Color.Transparent, segment.desc.tile.GlyphCharacter);
                world.AddEntity(new Segment(wreck, new SegmentDesc(offset, new StaticTile(tile))));
            }
        }
        var guards = world.entities.all.OfType<AIShip>().Where(
            s => s.behavior is BaseShipBehavior b
            && b.current is GuardOrder o
            && o.GuardTarget == this);
        var gate = world.entities.all.OfType<Stargate>().FirstOrDefault();
        var lastOrder = new BaseShipBehavior(new AttackOrder(source), new GateOrder(gate));
        if (source != null && source.sovereign != sovereign) {
            foreach (var g in guards) {
                g.behavior = lastOrder;
            }
        } else {
            var next = world.entities.all.OfType<Station>().Where(s => s.type == type && s != this).OrderBy(p => (p.position - position).magnitude2).FirstOrDefault();
            if (next != null) {
                foreach (var g in guards) {
                    var o = (GuardOrder)(g.behavior as BaseShipBehavior).current;
                    o.SetTarget(next);
                }
            } else {
                foreach (var g in guards) {
                    g.behavior = new PatrolOrbitOrder(this, 20);
                }
            }
        }
        onDestroyed.set.RemoveWhere(d => d.Value == null);
        foreach (var on in onDestroyed.set) {
            on.Value.Invoke(this, source, wreck);
        }
    }
    public void Update() {
        weapons?.ForEach(w => w.Update(this));
        behavior?.Update(this);
    }
    public Console GetDockScene(Console prev, PlayerShip playerShip) => null;
    public ColoredGlyph tile => type.tile.Original;
}
public interface ISegment : SpaceObject {
    SpaceObject parent { get; }
}
public class Segment : ISegment {
    //The segment essentially impersonates its parent station but with a different tile
    [JsonIgnore]
    public string name => parent.name;
    [JsonIgnore]
    public System world => parent.world;
    [JsonIgnore]
    public XY position => parent.position + desc.offset;
    [JsonIgnore]
    public XY velocity => parent.velocity;
    [JsonIgnore]
    public Sovereign sovereign => parent.sovereign;

    [JsonProperty]
    public int id { get; private set; }
    [JsonProperty]
    public SpaceObject parent { get; private set; }
    [JsonProperty]
    public SegmentDesc desc { get; private set; }
    public Segment() { }
    public Segment(SpaceObject parent, SegmentDesc desc) {
        this.id = parent.world.nextId++;
        this.parent = parent;
        this.desc = desc;
    }
    [JsonIgnore]
    public bool active => parent.active;
    public void Damage(Projectile p) => parent.Damage(p);
    public void Destroy(SpaceObject source) => parent.Destroy(source);
    public void Update() {}
    [JsonIgnore]
    public ColoredGlyph tile => desc.tile.Original;
}
public class AngledSegment : ISegment {
    //The segment essentially impersonates its parent station but with a different tile
    [JsonIgnore]
    public string name => parent.name;
    [JsonIgnore]
    public System world => parent.world;
    [JsonIgnore]
    public XY position => parent.position + desc.offset.Rotate(parent.rotationRad);
    [JsonIgnore]
    public XY velocity => parent.velocity;
    [JsonIgnore]
    public Sovereign sovereign => parent.sovereign;
    [JsonProperty]
    public int id { get; private set; }
    [JsonProperty]
    public IShip parent { get; private set; }
    [JsonProperty]
    public SegmentDesc desc { get; private set; }
    public AngledSegment() { }
    public AngledSegment(IShip parent, SegmentDesc desc) {
        this.id = parent.world.nextId++;
        this.parent = parent;
        this.desc = desc;
    }
    [JsonIgnore]
    public bool active => parent.active;
    public void Damage(Projectile p) => parent.Damage(p);
    public void Destroy(SpaceObject source) => parent.Destroy(source);
    public void Update() { }
    [JsonIgnore]
    public ColoredGlyph tile => desc.tile.Original;
    [JsonIgnore]
    SpaceObject ISegment.parent => parent;
}
public class CommonStationBehavior : StationBehavior {
    public CommonStationBehavior() {
    }
    public void Update(Station owner) {

    }
}