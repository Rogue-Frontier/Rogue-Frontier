using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    class TimedEvent : Entity {
        public string Name => "Spawner";
        public XY Position => new XY(double.NaN, double.NaN);
        public bool Active => delay > 0;
        public ColoredGlyph Tile => null;
        public XY Velocity => new XY();

        public int delay;
        public Action next;
        public TimedEvent(int delay, Action next) {
            this.delay = delay;
            this.next = next;
        }
        public void Update() {
            delay--;
            if (delay == 0) {
                next();
            }
        }
    }
}
