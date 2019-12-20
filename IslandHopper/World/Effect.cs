using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
    public interface Effect {
        XYZ Position { get; }
        ColoredGlyph SymbolCenter {get;}
        bool Active { get; }
        void UpdateRealtime(TimeSpan delta);                //	For step-independent effects
        void UpdateStep();					//	The number of steps per one in-game second is defined in Constants as STEPS_PER_SECOND
    }
    public class BulletTrail : Effect {
        public XYZ Position { get; private set; }

        public ColoredGlyph SymbolCenter => new ColoredGlyph('*', new Color(255, 255, 255, (int) (255 * (lifetime > 5 ? 1 : lifetime/5f))), Color.Black);
        public int lifetime;
        public bool Active => lifetime > 0;
        public BulletTrail(XYZ Position, int lifetime) {
            this.Position = Position;
            this.lifetime = lifetime;
        }
        public void UpdateRealtime(TimeSpan delta) { }
        public void UpdateStep() {
            lifetime--;
        }

        
    }
}
