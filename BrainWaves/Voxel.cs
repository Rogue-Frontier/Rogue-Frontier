using System;
using System.Collections.Generic;
using System.Text;

namespace BrainWaves {
    public interface Voxel {
        char Symbol { get; }
    }
    public class Wall : Voxel {
        public char Symbol => '#';
    }
    public class Floor : Voxel {
        public char Symbol => '+';
    }
}
