using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper.World {
    interface Segment {
        Entity parent { get; }
    }
}
