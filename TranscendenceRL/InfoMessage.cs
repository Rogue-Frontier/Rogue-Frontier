using Common;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public interface IPlayerMessage {
        bool Active { get; }
        ColoredString message { get; }
        void Reset();
        void Update();
        ColoredString Draw();
    }
    public class Transmission : IPlayerMessage {
        public SpaceObject source;
        InfoMessage info;
        public Transmission(SpaceObject source, ColoredString message, int updateInterval = 3) {
            this.source = source;
            this.info = new InfoMessage(message, updateInterval);
        }
        public Transmission(SpaceObject source, string message) {
            this.source = source;
            this.info = new InfoMessage(message);
        }
        public bool Active => info.Active;
        public ColoredString message => info.message;
        public void Reset() => info.Reset();
        public ColoredString Draw() => info.Draw();
        public void Update() => info.Update();
    }
    public class InfoMessage: IPlayerMessage {
        public ColoredString message { get; private set; }
        public int index;
        public int ticks;
        public int ticksRemaining;
        public int updateInterval;
        public int flashTicks;

        public InfoMessage(string message) : this(new ColoredString(message, Color.White, Color.Black)) { }
        public InfoMessage(ColoredString message, int updateInterval = 3) {
            this.message = message;
            index = 0;
            ticks = 0;
            ticksRemaining = 60 * 5;
            this.updateInterval = updateInterval;
        }

        public void Reset() {
            ticksRemaining = 150;
            flashTicks = 15;
        }
        public void Update() {
            if(index < message.Count) {
                if(ticks++%updateInterval == 0)
                    index++;
            } else if(ticksRemaining > 0) {
                ticksRemaining--;
            }
            if(flashTicks > 0) {
                flashTicks--;
            }
        }
        public bool Scrolling => ticks < message.Count;
        public bool Active => ticksRemaining > 0;
        public ColoredString Draw() {
            var result = message.SubString(0, index).WithOpacity((byte)Math.Min(255, ticksRemaining * 255 / TranscendenceRL.TICKS_PER_SECOND));
            if(flashTicks > 0) {
                var value = 255 * Math.Min(1, ticks / (float)TranscendenceRL.TICKS_PER_SECOND);
                result.SetBackground(new Color(value, 0, 0));
            }
            return result;
        }
    }
}
