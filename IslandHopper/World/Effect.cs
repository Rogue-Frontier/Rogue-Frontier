using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper.World {
    interface Effect {
        XYZ Position { get; }
        ColoredGlyph SymbolCenter {get;}
        bool Active { get; }
        void Update();
    }
}
