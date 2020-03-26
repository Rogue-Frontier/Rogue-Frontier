using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public interface SpaceObject : Entity {
        Sovereign Sovereign { get; }
        XY Velocity { get; }
    }
}
