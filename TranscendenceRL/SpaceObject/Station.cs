using Common;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranscendenceRL;

namespace TranscendenceRL {
    public interface IStation : SpaceObject {
        StationType StationType { get; }
    }
    public class Wreck : IStation {
        public SpaceObject creator;
        public string Name => $"Wreck of {creator.Name}";
        public World World => creator.World;

        public StationType StationType => null;

        public Sovereign Sovereign { get; private set; }
        public XY Position { get; private set; }
        public XY Velocity { get; private set; }


        public bool Active { get; private set; }

        public ColoredGlyph Tile => new ColoredGlyph(creator.Tile.GlyphCharacter, new Color(128, 128, 128), Color.Transparent);
        public Wreck(SpaceObject creator) {
            this.creator = creator;
            this.Sovereign = Sovereign.Inanimate;
            this.Position = creator.Position;
            this.Velocity = creator.Velocity;
            this.Active = true;
        }
        public void Damage(SpaceObject source, int hp) {
        }

        public void Destroy() {
            Active = false;
        }

        public void Update() {
            Position += Velocity / 30;
        }
    }
    public class Station : IStation {
        public string Name => StationType.name;
        public World World { get; private set; }
        public StationType StationType { get; private set; }

        public Sovereign Sovereign { get; private set; }
        public XY Position { get; private set; }
        public XY Velocity { get; private set; }
        public bool Active { get; private set; }
        private List<Segment> segments;
        DamageSystem DamageSystem;
        public List<Weapon> weapons;
        public List<AIShip> guards;
        public Station(World World, StationType Type, XY Position) {
            this.World = World;
            this.StationType = Type;
            this.Position = Position;
            this.Velocity = new XY();
            this.Active = true;
            this.Sovereign = Type.Sovereign;
            segments = new List<Segment>();
            CreateSegments();
            DamageSystem = new HPSystem(this, Type.hp);
            weapons = StationType.weapons?.Generate(World.types);
            guards = new List<AIShip>();
            CreateGuards();

        }
        private void CreateSegments() {
            foreach(var segmentDesc in StationType.segments) {
                var s = new Segment(this, segmentDesc.offset, segmentDesc.tile);
                segments.Add(s);
                World.AddEntity(s);
            }
        }
        private void CreateGuards() {
            if(StationType.guards != null) {
                //Suppose we should pass in the owner object
                var generated = StationType.guards.Generate(World.types, this);
                foreach(var guard in generated) {
                    //Add the order
                    var ship = new AIShip(guard, new GuardOrder(guard, this));

                    guards.Add(ship);
                    World.AddEntity(ship);
                }
            }
            
        }
        public void Damage(SpaceObject source, int hp) {
            DamageSystem.Damage(source, hp);
        }
        public void Destroy() {
            Active = false;
            World.AddEntity(new Wreck(this));
            foreach(var segment in segments) {
                World.AddEntity(new Wreck(segment));
            }
        }
        public void Update() {
            weapons?.ForEach(w => w.Update(this));
        }
        public ColoredGlyph Tile => StationType.tile.Glyph;

    }
    public class Segment : IStation {
        //The segment essentially impersonates its parent station but with a different tile
        public string Name => Parent.Name;
        public World World => Parent.World;
        public XY Position => Parent.Position + Offset;
        public XY Velocity => Parent.Velocity;
        public Sovereign Sovereign => Parent.Sovereign;
        public XY Offset { get; private set; }
        private StaticTile _Tile;
        public Station Parent;

        public Segment(Station Parent, XY Offset, StaticTile tile) {
            this.Parent = Parent;
            this.Offset = Offset;
            this._Tile = tile;
        }

        public StationType StationType => Parent.StationType;
        
        public bool Active => Parent.Active;
        public void Damage(SpaceObject source, int hp) => Parent.Damage(source, hp);
        public void Destroy() => Parent.Destroy();
        public void Update() {
        }
        public ColoredGlyph Tile => _Tile.Glyph;
    }
}
