using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public interface SpaceObject : Entity {
        string Name { get; }
        World World { get; }
        Sovereign Sovereign { get; }
        XY Velocity { get; }
        void Damage(SpaceObject source, int hp);
        void Destroy();
    }
}
