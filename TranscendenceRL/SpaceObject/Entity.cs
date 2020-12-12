using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public interface Effect {
        XY Position { get; }
        bool Active { get; }
        ColoredGlyph Tile { get; }
        void Update();
    }
    public interface Entity : Effect {
    }

}
