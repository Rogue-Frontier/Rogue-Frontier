using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    public enum DisruptMode {
        NONE, FORCE_ON, FORCE_OFF
    }
    public class Disrupt {
        public int ticksLeft;
        public bool active => ticksLeft > 0;
        public DisruptMode thrustMode;
        public DisruptMode turnMode;
        public DisruptMode brakeMode;
        public DisruptMode fireMode;

        public void Update() {
            ticksLeft--;
        }
    }
}
