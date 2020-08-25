using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    public enum HijackMode {
        NONE, FORCE_ON, FORCE_OFF
    }
    public class ControlHijack {
        public int ticksLeft;
        public bool active => ticksLeft > 0;
        public HijackMode thrustMode;
        public HijackMode turnMode;
        public HijackMode brakeMode;
        public HijackMode fireMode;

        public void Update() {
            ticksLeft--;
        }
    }
}
