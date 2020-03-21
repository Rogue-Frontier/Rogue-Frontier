using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    interface Effect {
        XY position { get; }
        bool Active { get; }
        ColoredGlyph Tile { get; }
        void Update();
    }
    interface Entity : Effect {
    }

}
