using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public interface IStation : Entity {
        World World { get; }
        StationType Type { get; }
    }
    public class Station : IStation {
        public World World { get; private set; }
        public StationType Type { get; private set; }

        public XY Position { get; private set; }
        public bool Active => true;
        private List<Segment> segments;
        public Station(World World, StationType Type, XY Position) {
            this.World = World;
            this.Type = Type;
            this.Position = Position;
            CreateSegments();
        }
        private void CreateSegments() {
            segments = new List<Segment>();
            foreach(var segmentDesc in Type.segments) {
                var s = new Segment(this, Position + segmentDesc.offset, segmentDesc.tile);
                segments.Add(s);
                World.AddEntity(s);
            }

        }
        public void Update() {

        }
        public ColoredGlyph Tile => Type.tile.Glyph;

    }
    public class Segment : IStation {
        public World World { get; private set; }
        private Station Parent;
        public XY Position { get; private set; }
        private StaticTile _Tile;
        
        public Segment(Station Parent, XY Position, StaticTile tile) {
            this.Parent = Parent;
            this.Position = Position;
            this._Tile = tile;
        }

        public StationType Type => Parent.Type;
        
        public bool Active => Parent.Active;

        public void Update() => Parent.Update();
        public ColoredGlyph Tile => _Tile.Glyph;
    }
}
