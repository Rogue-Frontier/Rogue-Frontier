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
        public string name => $"Wreck of {creator.name}";
        public SpaceObject creator;
        [JsonProperty]
        public World world { get; private set; }
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
        [JsonIgnore]
        public ColoredGlyph tile => new ColoredGlyph(new Color(128, 128, 128), Color.Transparent, creator.tile.GlyphCharacter);
        public Wreck() { }
        public Wreck(SpaceObject creator) {
            this.creator = creator;
            this.world = creator.world;
            this.sovereign = Sovereign.Inanimate;
            this.position = creator.position;
            this.velocity = creator.velocity;
            this.active = true;
            cargo = new HashSet<Item>();
        }
        public Console GetScene(Console prev, PlayerShip playerShip) => new WreckScene(prev, playerShip, this);
        public void Damage(SpaceObject source, int hp) {
        }

        public void Destroy(SpaceObject source) {
            active = false;
        }

        public void Update() {
            position += velocity / Program.TICKS_PER_SECOND;
        }
    }
    public class Station : SpaceObject, Dockable, ITrader {
        [JsonIgnore]
        public string name => type.name;
        [JsonProperty]
        public World world { get; set; }
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
        public List<Segment> segments;
        public HullSystem damageSystem;
        [JsonProperty]
        public HashSet<Item> cargo { get; set; }
        public List<Weapon> weapons;
        public List<AIShip> guards;
        public Station() { }
        public Station(World World, StationType Type, XY Position) {
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
        }
        public void CreateSegments() {
            segments = new List<Segment>();
            foreach(var segmentDesc in type.segments) {
                var s = new Segment(this, segmentDesc);
                segments.Add(s);
                world.AddEntity(s);
            }
        }
        public void CreateGuards() {
            guards = new List<AIShip>();
            if(type.guards != null) {
                //Suppose we should pass in the owner object
                var generated = type.guards.Generate(world.types, this);
                foreach(var guard in generated) {
                    guards.Add(guard);
                    world.AddEntity(guard);
                    world.AddEffect(new Heading(guard));
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
            damageSystem.Damage(this, source, hp);
            if(source.sovereign != sovereign) {

                var guards = from guard in world.entities.all.OfType<AIShip>()
                             where guard.controller is GuardOrder order && order.GuardTarget == this
                             select (GuardOrder)guard.controller;
                foreach(var order in guards) {
                    order.attackTime = 300;
                    order.attackOrder = new AttackOrder(source);
                }
            }
        }
        public void Destroy(SpaceObject source) {
            active = false;
            var wreck = new Wreck(this);

            var drop = weapons?.Select(w => w.source);
            if(drop != null) {
                foreach (var item in drop) {
                    item.RemoveWeapon();
                    wreck.cargo.Add(item);
                }
            }
            
            world.AddEntity(wreck);
            foreach(var segment in segments) {
                var offset = segment.desc.offset;
                var tile = new ColoredGlyph(new Color(128, 128, 128), Color.Transparent, segment.desc.tile.Glyph.GlyphCharacter);
                world.AddEntity(new Segment(wreck, new SegmentDesc(offset, new StaticTile(tile))));
            }

            if (source.sovereign != sovereign) {
                var guards = from guard in world.entities.all.OfType<AIShip>()
                             where guard.controller is GuardOrder order && order.GuardTarget == this
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

        public ColoredGlyph tile => type.tile.Glyph;

    }
    public class Segment : SpaceObject {
        //The segment essentially impersonates its parent station but with a different tile
        [JsonIgnore]
        public string name => parent.name;
        [JsonIgnore] 
        public World world => parent.world;
        [JsonIgnore] 
        public XY position => parent.position + desc.offset;
        [JsonIgnore] 
        public XY velocity => parent.velocity;
        [JsonIgnore] 
        public Sovereign sovereign => parent.sovereign;
        public SpaceObject parent;
        public SegmentDesc desc;
        public Segment() { }
        public Segment(SpaceObject parent, SegmentDesc desc) {
            this.parent = parent;
            this.desc = desc;
        }

        [JsonIgnore] 
        public bool active => parent.active;
        public void Damage(SpaceObject source, int hp) => parent.Damage(source, hp);
        public void Destroy(SpaceObject source) => parent.Destroy(source);
        public void Update() {
        }
        [JsonIgnore] 
        public ColoredGlyph tile => desc.tile.Glyph;
    }
}
