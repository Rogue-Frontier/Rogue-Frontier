using Common;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrainWaves {
    class Light {
        World World;
        XY Position;
        HashSet<(int, int)> visible = new HashSet<(int, int)>();
        public Light(World World) {
            this.World = World;
        }
        public void UpdateLight() {
            visible.Clear();
            visible.Add(Position);
            HashSet<(int, int)> known = new HashSet<(int, int)>();
            SimplePriorityQueue<XY, double> q = new SimplePriorityQueue<XY, double>();
            q.Enqueue(Position, 0);
            while (q.Count > 0) {
                var p = q.Dequeue();

                if (IsVisible(p)) {
                    visible.Add(p);
                }
                foreach (var offset in new XY[] { new XY(-1, 0), new XY(1, 0), new XY(0, -1), new XY(0, 1) }) {
                    var next = p + offset;
                    if (known.Add(next)) {
                        q.Enqueue(next, (next - Position).Magnitude);
                    }
                }
            }
            bool IsVisible(XY p) {
                return visible.Contains(p - (Position - p).Normal);
            }

            foreach(var p in visible) {
                ref byte b = ref World.brightness[p];
                var d = (Position - p).Magnitude;
                if (d < 1) {
                    b = 255;
                } else {

                }
            }
        }
    }
}
