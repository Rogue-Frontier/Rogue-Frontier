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

namespace TranscendenceRL {
    public class Wreck : Dockable {
        public string Name => $"Wreck of {creator.Name}";
        public SpaceObject creator;
        public World World { get; private set; }
        public Sovereign Sovereign { get; private set; }
        public XY Position { get; private set; }
        public XY Velocity { get; private set; }
        public bool Active { get; private set; }
        public HashSet<Item> Items { get; private set; }
        public ColoredGlyph Tile => new ColoredGlyph(new Color(128, 128, 128), Color.Transparent, creator.Tile.GlyphCharacter);
        public IDockViewDesc MainView => DockScreenDesc.WreckScreen;
        public Wreck(SpaceObject creator) {
            this.creator = creator;
            this.World = creator.World;
            this.Sovereign = Sovereign.Inanimate;
            this.Position = creator.Position;
            this.Velocity = creator.Velocity;
            this.Active = true;
            Items = new HashSet<Item>();
        }
        public void Damage(SpaceObject source, int hp) {
        }

        public void Destroy(SpaceObject source) {
            Active = false;
        }

        public void Update() {
            Position += Velocity / TranscendenceRL.TICKS_PER_SECOND;
        }
    }
    public class Station : SpaceObject {
        public string Name => StationType.name;
        public World World { get; private set; }
        public StationType StationType { get; private set; }

        public Sovereign Sovereign { get; private set; }
        public XY Position { get; private set; }
        public XY Velocity { get; private set; }
        public bool Active { get; private set; }
        private List<Segment> segments;
        DamageSystem DamageSystem;
        public HashSet<Item> Items { get; private set; }
        public List<Weapon> weapons;
        public List<AIShip> guards;
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
                    //Add the order
                    var ship = new AIShip(guard, new GuardOrder(this));

                    guards.Add(ship);
                    World.AddEntity(ship);
                    World.AddEffect(new Heading(ship));
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
                foreach(var guard in World.entities.all.OfType<AIShip>()) {
                    if(guard.controller is GuardOrder order && order.guard == this && order.target == null) {
                        order.target = source;
                    }
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
        }
        public void Update() {
            weapons?.ForEach(w => w.Update(this));
        }
        public ColoredGlyph Tile => StationType.tile.Glyph;

    }
    public class Segment : SpaceObject {
        //The segment essentially impersonates its parent station but with a different tile
        public string Name => Parent.Name;
        public World World => Parent.World;
        public XY Position => Parent.Position + desc.offset;
        public XY Velocity => Parent.Velocity;
        public Sovereign Sovereign => Parent.Sovereign;
        public SpaceObject Parent;
        public SegmentDesc desc;
        public Segment(SpaceObject Parent, SegmentDesc desc) {
            this.Parent = Parent;
            this.desc = desc;
        }
        
        public bool Active => Parent.Active;
        public void Damage(SpaceObject source, int hp) => Parent.Damage(source, hp);
        public void Destroy(SpaceObject source) => Parent.Destroy(source);
        public void Update() {
        }
        public ColoredGlyph Tile => desc.tile.Glyph;
    }
}
