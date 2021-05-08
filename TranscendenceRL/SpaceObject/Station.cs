using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranscendenceRL;
using static TranscendenceRL.StationType;
using Console = SadConsole.Console;
using Newtonsoft.Json;
using static TranscendenceRL.Weapon;

namespace TranscendenceRL {
    public class Wreck : Dockable {
        [JsonIgnore]
        public string Name => $"Wreck of {creator.Name}";
        public SpaceObject creator;
        [JsonProperty]
        public World World { get; private set; }
        [JsonProperty]
        public Sovereign Sovereign { get; private set; }
        [JsonProperty] 
        public XY Position { get; private set; }
        [JsonProperty] 
        public XY Velocity { get; private set; }
        [JsonProperty] 
        public bool Active { get; private set; }
        [JsonProperty] 
        public HashSet<Item> Items { get; private set; }
        [JsonIgnore]
        public ColoredGlyph Tile => new ColoredGlyph(new Color(128, 128, 128), Color.Transparent, creator.Tile.GlyphCharacter);
        public Wreck() { }
        public Wreck(SpaceObject creator) {
            this.creator = creator;
            this.World = creator.World;
            this.Sovereign = Sovereign.Inanimate;
            this.Position = creator.Position;
            this.Velocity = creator.Velocity;
            this.Active = true;
            Items = new HashSet<Item>();
        }
        public Console GetScene(Console prev, PlayerShip playerShip) => new WreckScene(prev, playerShip, this);
        public void Damage(SpaceObject source, int hp) {
        }

        public void Destroy(SpaceObject source) {
            Active = false;
        }

        public void Update() {
            Position += Velocity / Program.TICKS_PER_SECOND;
        }
    }
    public class Station : SpaceObject, Dockable, ITrader {
        [JsonIgnore]
        public string Name => StationType.name;
        [JsonProperty]
        public World World { get; set; }
        [JsonProperty]
        public StationType StationType { get; set; }
        [JsonProperty]
        public Sovereign Sovereign { get; set; }
        [JsonProperty]
        public XY Position { get; set; }
        [JsonProperty]
        public XY Velocity { get; set; }
        [JsonProperty]
        public bool Active { get; set; }
        public List<Segment> segments;
        public HullSystem DamageSystem;
        [JsonProperty]
        public HashSet<Item> Items { get; set; }
        public List<Weapon> weapons;
        public List<AIShip> guards;
        public Station() { }
        public Station(World World, StationType Type, XY Position) {
            this.World = World;
            this.StationType = Type;
            this.Position = Position;
            this.Velocity = new XY();
            this.Active = true;
            this.Sovereign = Type.Sovereign;
            DamageSystem = new HPSystem(Type.hp);
            Items = new HashSet<Item>();
            weapons = StationType.weapons?.Generate(World.types);
            weapons?.ForEach(w => w.aiming = new Omnidirectional());
        }
        public void CreateSegments() {
            segments = new List<Segment>();
            foreach(var segmentDesc in StationType.segments) {
                var s = new Segment(this, segmentDesc);
                segments.Add(s);
                World.AddEntity(s);
            }
        }
        public void CreateGuards() {
            guards = new List<AIShip>();
            if(StationType.guards != null) {
                //Suppose we should pass in the owner object
                var generated = StationType.guards.Generate(World.types, this);
                foreach(var guard in generated) {
                    guards.Add(guard);
                    World.AddEntity(guard);
                    World.AddEffect(new Heading(guard));
                }
            }
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
        public void Damage(SpaceObject source, int hp) {
            DamageSystem.Damage(this, source, hp);
            if(source.Sovereign != Sovereign) {

                var guards = from guard in World.entities.all.OfType<AIShip>()
                             where guard.controller is GuardOrder order && order.guardTarget == this
                             select (GuardOrder)guard.controller;
                foreach(var order in guards) {
                    order.attackTime = 300;
                    order.attackOrder = new AttackOrder(source);
                }
            }
        }
        public void Destroy(SpaceObject source) {
            Active = false;
            var wreck = new Wreck(this);

            var drop = weapons?.Select(w => w.source);
            if(drop != null) {
                foreach (var item in drop) {
                    item.RemoveWeapon();
                    wreck.Items.Add(item);
                }
            }
            
            World.AddEntity(wreck);
            foreach(var segment in segments) {
                var offset = segment.desc.offset;
                var tile = new ColoredGlyph(new Color(128, 128, 128), Color.Transparent, segment.desc.tile.Glyph.GlyphCharacter);
                World.AddEntity(new Segment(wreck, new SegmentDesc(offset, new StaticTile(tile))));
            }

            if (source.Sovereign != Sovereign) {
                var guards = from guard in World.entities.all.OfType<AIShip>()
                             where guard.controller is GuardOrder order && order.guardTarget == this
                             select (GuardOrder)guard.controller;
                foreach (var order in guards) {
                    order.attackTime = -1;
                    order.attackOrder = new AttackOrder(source);
                }
            }
        }
        public void Update() {
            weapons?.ForEach(w => w.Update(this));
        }

        public Console GetScene(Console prev, PlayerShip playerShip) => null;

        public ColoredGlyph Tile => StationType.tile.Glyph;

    }
    public class Segment : SpaceObject {
        //The segment essentially impersonates its parent station but with a different tile
        [JsonIgnore]
        public string Name => Parent.Name;
        [JsonIgnore] 
        public World World => Parent.World;
        [JsonIgnore] 
        public XY Position => Parent.Position + desc.offset;
        [JsonIgnore] 
        public XY Velocity => Parent.Velocity;
        [JsonIgnore] 
        public Sovereign Sovereign => Parent.Sovereign;
        public SpaceObject Parent;
        public SegmentDesc desc;
        public Segment() { }
        public Segment(SpaceObject Parent, SegmentDesc desc) {
            this.Parent = Parent;
            this.desc = desc;
        }

        [JsonIgnore] 
        public bool Active => Parent.Active;
        public void Damage(SpaceObject source, int hp) => Parent.Damage(source, hp);
        public void Destroy(SpaceObject source) => Parent.Destroy(source);
        public void Update() {
        }
        [JsonIgnore] 
        public ColoredGlyph Tile => desc.tile.Glyph;
    }
}
