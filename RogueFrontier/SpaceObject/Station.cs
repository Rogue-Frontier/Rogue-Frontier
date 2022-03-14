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

public class Wreck : MovingObject, IDockable {
    [JsonIgnore]
    public string name => $"Wreck of {creator.name}";
    [JsonIgnore]
    public ColoredGlyph tile => new ColoredGlyph(new Color(128, 128, 128), Color.Black, creator.tile.GlyphCharacter);
    [JsonProperty]
    public int id { get; private set; }
    [JsonProperty]
    public StructureObject creator { get; set; }
    [JsonProperty]
    public System world { get; private set; }
    [JsonProperty]
    public XY position { get; set; }
    [JsonProperty]
    public XY velocity { get; set; }
    [JsonProperty]
    public bool active { get; private set; }
    [JsonProperty]
    public HashSet<Item> cargo { get; private set; }
    [JsonProperty]
    public int ticks { get; private set; }
    [JsonProperty]
    public XY gravity { get; private set; }
    public Wreck() { }
    public Wreck(StructureObject creator, IEnumerable<Item> cargo = null) {
        this.id = creator.world.nextId++;
        this.creator = creator;
        this.world = creator.world;
        this.position = creator.position;
        this.velocity = creator.velocity;
        this.active = true;
        this.cargo = new HashSet<Item>();
        if (cargo?.Any() == true) {
            this.cargo.UnionWith(cargo);
        }

        gravity = new XY(0, 0);
    }
    public XY GetDockPoint() => XY.Zero;
    public Console GetDockScene(Console prev, PlayerShip playerShip) => new WreckScene(prev, playerShip, this);
    public void Damage(Projectile p) {
    }

    public void Destroy(ActiveObject source) {
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
public class Station : ActiveObject, ITrader, IDockable {
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
    public double rotation;

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

    public ConstructionJob construction;

    public double stealth;

    public delegate void Destroyed(Station station, ActiveObject destroyer, Wreck wreck);
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
        damageSystem = new HP(Type.hp);
        cargo = new(Type.cargo?.Generate(World.types) ?? new List<Item>());
        weapons = type.weapons?.Generate(World.types) ?? new();
        weapons.ForEach(w => w.aiming ??= new Omnidirectional());
        InitBehavior(Type.behavior);
    }
    public enum Behaviors {
        none,
        raisu,
        pirate,
        reinforceNearby,
        constellationShipyard,
        amethystStore,
        orionWarlords
    }
    public void InitBehavior(Behaviors behavior) {
        this.behavior = behavior switch {
            Behaviors.raisu => null,
            Behaviors.pirate => new PirateStation(),
            Behaviors.reinforceNearby => new ReinforceNearby(),
            Behaviors.orionWarlords => new OrionWarlordsStation(this),
            Behaviors.none => null,
            _ => null
        };
    }
    public void CreateSegments() {
        segments = new();
        foreach (var segmentDesc in type.segments??new()) {
            var s = new Segment(this, segmentDesc);
            segments.Add(s);
            world.AddEntity(s);
        }
    }
    public void CreateGuards() {
        guards = new();
        foreach (var guard in type.ships?.Generate(world.types, this) ?? guards) {
            guards.Add(guard);
            world.AddEntity(guard);
            world.AddEffect(new Heading(guard));
        }
    }
    public void CreateSatellites(LocationContext lc) {
        type.satellites?.Generate(lc, world.types);
    }
    public IEnumerable<AIShip> GetDocked() =>
        world.entities.GetAll(p => (position - p).magnitude < 5)
            .OfType<AIShip>().Where(s => s.dock?.Target == this);
    
    public XY GetDockPoint() =>
        type.dockPoints.Except(GetDocked().Select(s => s.dock?.Offset)).FirstOrDefault() ?? XY.Zero;
    public List<AIShip> UpdateGuardList() {
        return guards = new(world.entities.all.OfType<AIShip>()
            .Where(s => s.behavior switch {
                GuardOrder g => g.home == this,
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
        damageSystem.Damage(world.tick, p, () => Destroy(p.source));

        if (!active) {
            return;
        }
        var source = p.source;
        if (source != null && source.sovereign != sovereign) {
            var guards = world.entities.all.OfType<AIShip>()
                .Select(s => s.behavior.GetOrder())
                .OfType<GuardOrder>()
                .Where(g => g.home == this);
            foreach (var order in guards) {
                order.SetAttack(source, 300);
            }
        }
    }
    public void Destroy(ActiveObject source) {
        active = false;
        if (source is PlayerShip ps) {
            ps.stationsDestroyed.Add(this);
            if (type.crimeOnDestroy) {
                ps.crimeRecord.Add(new DestructionCrime(this));
            }
        }

        if(type.explosionType != null)
            new Weapon() { projectileDesc = type.explosionType, aiming = new Targeting() { target = source } }.Fire(this, rotation);

        var wreck = new Wreck(this);
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
                var tile = new ColoredGlyph(new Color(128, 128, 128), Color.Black, segment.desc.tile.glyph);
                world.AddEntity(new Segment(wreck, new SegmentDesc(offset, new StaticTile(tile))));
            }
        }
        var guards = world.entities.all.OfType<AIShip>().Where(
            s => s.behavior is GuardOrder o && o.home == this);
        var gate = world.entities.all.OfType<Stargate>().FirstOrDefault();
        IShipOrder lastOrder = gate == null ? new AttackOrder(source) : new CompoundOrder(new AttackOrder(source), new GateOrder(gate));
        if (source != null && source.sovereign != sovereign) {
            foreach (var g in guards) {
                g.behavior = lastOrder;
            }
        } else {
            var next = world.entities.all.OfType<Station>().Where(s => s.type == type && s != this).OrderBy(p => (p.position - position).magnitude2).FirstOrDefault();
            if (next != null) {
                foreach (var g in guards) {
                    var o = (GuardOrder)g.behavior;
                    o.SetHome(next);
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
        if(world.tick%15 == 0) {
            stealth = type.stealth;
            if (weapons.Any()) {
                stealth *= 1 - weapons.Max(w => ((double)w.delay / w.desc.fireCooldown));
            }
            if(construction != null) {
                construction.time -= 15;
                if (construction.time < 1) {
                    var s = new AIShip(new(world, construction.desc.type, position), sovereign, construction.desc.order.Value(this));
                    world.AddEntity(s);
                    guards.Add(s);
                    construction = null;
                }
            } else if(type.construction != null) {
                if(guards.Count < type.construction.max) {
                    construction = type.construction.catalog.GetRandom(world.karma);
                }
            }
        }
        weapons?.ForEach(w => w.Update(this));
        behavior?.Update(this);

    }
    public Console GetDockScene(Console prev, PlayerShip playerShip) => null;
    [JsonIgnore]
    public ColoredGlyph tile => type.tile.Original;
}
public interface ISegment : MovingObject {
    MovingObject parent { get; }
}
public class Segment : ISegment {
    //The segment essentially impersonates its parent station but with a different tile
    [JsonIgnore]
    public System world => parent.world;
    [JsonIgnore]
    public XY position => parent.position + desc.offset;
    [JsonIgnore]
    public XY velocity => parent.velocity;

    [JsonProperty]
    public int id { get; private set; }
    [JsonProperty]
    public MovingObject parent { get; private set; }
    [JsonProperty]
    public SegmentDesc desc { get; private set; }
    public Segment() { }
    public Segment(MovingObject parent, SegmentDesc desc) {
        this.id = parent.world.nextId++;
        this.parent = parent;
        this.desc = desc;
    }
    [JsonIgnore]
    public bool active => parent.active;
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
    [JsonIgnore]
    MovingObject ISegment.parent => parent;
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
    public void Destroy(ActiveObject source) => parent.Destroy(source);
    public void Update() { }
    [JsonIgnore]
    public ColoredGlyph tile => desc.tile.Original;
}