using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class Star {
        public XY position;
        public int radius;
        public Star(XY position, int radius) {
            this.position = position;
            this.radius = radius;
        }
    }
}
