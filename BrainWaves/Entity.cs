using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrainWaves {
    public interface Entity {
        XY Position { get; set; }
        ColoredGlyph Tile { get; }
        bool Active { get; }
        void UpdateStep();
    }
}
