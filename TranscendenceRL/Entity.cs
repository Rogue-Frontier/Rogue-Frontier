using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public interface Effect {
        XY position { get; }
        bool Active { get; }
        ColoredGlyph tile { get; }
        void Update();
    }
    public interface Entity : Effect {
    }

}
