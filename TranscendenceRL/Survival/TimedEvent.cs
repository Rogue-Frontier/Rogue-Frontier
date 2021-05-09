using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    class TimedEvent : Entity {
        public string Name => "Spawner";
        public XY position => new XY(double.NaN, double.NaN);
        public bool active => delay > 0;
        public ColoredGlyph tile => null;
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
