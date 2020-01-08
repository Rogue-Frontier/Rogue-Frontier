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
    public class Trail : Effect {
        public XYZ Position { get; set; }

        public ColoredGlyph SymbolCenter => new ColoredGlyph(symbol.Glyph, new Color(symbol.Foreground.R, symbol.Foreground.G, symbol.Foreground.B, (int) (255 * (lifetime > 5 ? 1 : (lifetime + 5)/10f))), Color.Black);
        public int lifetime;
        ColoredGlyph symbol;
        public bool Active => lifetime > 0;
        public Trail(XYZ Position, int lifetime, char symbol) {
            this.Position = Position;
            this.lifetime = lifetime;
            this.symbol = new ColoredGlyph(symbol, Color.White, Color.Black);
        }
        public Trail(XYZ Position, int lifetime, ColoredGlyph symbol) {
            this.Position = Position;
            this.lifetime = lifetime;
            this.symbol = symbol;
        }
        public void UpdateRealtime(TimeSpan delta) { }
        public void UpdateStep() {
            lifetime--;
        }
    }
    public class RealtimeTrail : Effect {
        public XYZ Position { get; set; }

        public ColoredGlyph SymbolCenter => new ColoredGlyph(symbol.Glyph, new Color(symbol.Foreground.R, symbol.Foreground.G, symbol.Foreground.B, (int)(255 * Math.Min(Math.Max(lifetime * 2, 0.5), 1))), Color.Black);
        public double lifetime;
        ColoredGlyph symbol;
        public bool Active => lifetime > 0;
        public RealtimeTrail(XYZ Position, double lifetime, char symbol) {
            this.Position = Position;
            this.lifetime = lifetime;
            this.symbol = new ColoredGlyph(symbol, Color.White, Color.Black);
        }
        public RealtimeTrail(XYZ Position, double lifetime, ColoredGlyph symbol) {
            this.Position = Position;
            this.lifetime = lifetime;
            this.symbol = symbol;
        }
        public void UpdateRealtime(TimeSpan delta) {
            lifetime -= delta.TotalSeconds;
        }
        public void UpdateStep() {
        }
    }
    public class Decal : Effect {
        public XYZ Position { get; set; }

        public ColoredGlyph SymbolCenter => symbol;
        public ColoredGlyph symbol;
        public bool Active => true;
        public Decal(XYZ Position, ColoredGlyph symbol) {
            this.Position = Position;
            this.symbol = symbol;
        }
        public void UpdateRealtime(TimeSpan delta) { }
        public void UpdateStep() { }
    }
}
