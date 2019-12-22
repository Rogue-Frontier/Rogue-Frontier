using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
    public interface Effect {
        XYZ Position { get; set; }
        ColoredGlyph SymbolCenter { get; }
        bool Active { get; }
        void UpdateRealtime(TimeSpan delta);                //	For step-independent effects
        void UpdateStep();					//	The number of steps per one in-game second is defined in Constants as STEPS_PER_SECOND
    }
    public class Reticle : Effect {
        public XYZ Position { get; set; }
        public Color Color;
        public ColoredGlyph SymbolCenter => new ColoredGlyph('+', Color, Color.Black);
        private Func<bool> active;
        public Reticle(Func<bool> active, XYZ Position, Color? Color = null) {
            this.active = active;
            this.Position = Position;
            this.Color = Color ?? new Color(255, 255, 255);
        }
        public bool Active => active();
        public void UpdateRealtime(TimeSpan delta) {}
        public void UpdateStep() {}
    }
    public class BulletTrail : Effect {
        public XYZ Position { get; set; }

        public ColoredGlyph SymbolCenter => new ColoredGlyph('-', new Color(255, 255, 255, (int) (255 * (lifetime > 5 ? 1 : (lifetime + 5)/10f))), Color.Black);
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
