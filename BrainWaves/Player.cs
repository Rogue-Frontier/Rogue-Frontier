using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using Priority_Queue;

namespace BrainWaves {
    class Player : Entity {
        public HashSet<(int, int)> seen = new HashSet<(int, int)>();
        public HashSet<(int, int)> visible = new HashSet<(int, int)>();
        public DictCounter<string> messages = new DictCounter<string>();


        World World;
        public XY Position { get; set; }


        public ColoredGlyph Tile { get => new ColoredGlyph(World.brightness.Get(Position) < 128 ? Color.White : Color.Black, Color.Transparent, '@'); }

        public bool Active { get; set; }
        public Player(World World) {
            this.World = World;
        }
        public void UpdateStep() {
            messages.dict.Clear();
            visible.Clear();

            visible.Add(Position);

            HashSet<(int, int)> known = new HashSet<(int, int)>();
            SimplePriorityQueue<XY, double> q = new SimplePriorityQueue<XY, double>();
            q.Enqueue(Position, 0);
            while(q.Count > 0) {
                var p = q.Dequeue();

                if(IsVisible(p)) {
                    visible.Add(p);
                }
                foreach (var offset in new XY[] { new XY(-1, 0), new XY(1, 0), new XY(0, -1), new XY(0, 1) }) {
                    var next = p + offset;
                    if(known.Add(next)) {
                        q.Enqueue(next, (next - Position).Magnitude);
                    }
                }
            }
            bool IsVisible(XY p) {
                return visible.Contains(p - (Position - p).Normal);
            }
        }
        public void Move(XY dest) {

        }
    }
}
