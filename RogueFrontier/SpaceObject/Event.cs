using System;
using System.Collections.Generic;
using System.Text;

namespace RogueFrontier {
    public interface Event {
        bool active { get; }
        void Update();
    }
}
