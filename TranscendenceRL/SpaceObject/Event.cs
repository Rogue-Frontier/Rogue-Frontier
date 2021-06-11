using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    public interface Event {
        bool active { get; }
        void Update();
    }
}
