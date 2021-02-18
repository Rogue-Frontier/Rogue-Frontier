using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    interface ProjectileBarrier : Entity {
        void Interact(Projectile projectile);
    }
}
