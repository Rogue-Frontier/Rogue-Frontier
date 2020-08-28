using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using Priority_Queue;

namespace BrainWaves {
    public class Player : Entity {
        public HashSet<(int, int)> seen = new HashSet<(int, int)>();
        public HashSet<(int, int)> visible = new HashSet<(int, int)>();
        public DictCounter<string> messages = new DictCounter<string>();


        World World;
        public XY Position { get; set; }


        public ColoredGlyph Tile { get => new ColoredGlyph(World.brightness.Get(Position) < 128 ? Color.White : Color.Black, Color.Transparent, '@'); }

        public bool Active { get; set; } = true;
        public Player(World World) {
            this.World = World;
        }
        public void UpdateVisible() {
            visible.Clear();
            visible.Add(Position);
            HashSet<(int, int)> known = new HashSet<(int, int)>();
            known.Add(Position);
            SimplePriorityQueue<XY, double> q = new SimplePriorityQueue<XY, double>();
            q.Enqueue(Position, 0);
            while (q.Count > 0) {
                var p = q.Dequeue();

                if (IsVisible(p)) {
                    visible.Add(p);
                    foreach (var offset in new XY[] { new XY(-1, 0), new XY(1, 0), new XY(0, -1), new XY(0, 1) }) {
                        var next = p + offset;
                        if (known.Add(next)) {
                            q.Enqueue(next, (next - Position).Magnitude);
                        }
                    }
                }
            }
            bool IsVisible(XY p) {
                var displacement = (Position - p);
                var direction = displacement.Normal;
                var dist = displacement.Magnitude;
                bool result = true;


                var v = World.voxels.Get(p);
                if(v == null) {
                    result = false;
                } else if(v is Floor) {
                    //Looking down a hallway at an angle
                    for (int i = 1; i < dist / 2 + 1; i++) {
                        var behind = p + direction * i;
                        behind = behind.Round;
                        result = result && visible.Contains(behind) && World.voxels.Get(behind) is Floor;
                    }
                } else if(v is Wall) {
                    //Looking down a hallway at an angle
                    for (int i = 1; i < dist / 2 + 1; i++) {
                        var behind = p + direction * i;
                        behind = behind.Round;

                        var left = behind + direction.Rotate(90 * Math.PI / 180);
                        var right = behind + direction.Rotate(90 * Math.PI / 180);

                        result = result && visible.Contains(behind) &&
                            ((visible.Contains(left) /* && World.voxels.Get(left) is Floor */)
                                || (visible.Contains(right) /* && World.voxels.Get(right) is Floor */));
                    }
                }
                
                return result;
            }
        }
        public void UpdateStep() {
            messages.dict.Clear();
            UpdateVisible();
        }
        public void Move(XY dest) {
            if(World.voxels.Get(dest) is Floor) {
                Position = dest;
            }
        }
    }
}
