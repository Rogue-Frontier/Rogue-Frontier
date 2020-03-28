using Common;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class PlayerMessage {
        public ColoredString message;
        public int index;
        public int ticks;
        public int ticksRemaining;
        public int updateInterval;
        public int flashTicks;

        public PlayerMessage(string message) : this(new ColoredString(message, Color.White, Color.Black)) { }
        public PlayerMessage(ColoredString message, int updateInterval = 3) {
            this.message = message;
            index = 0;
            ticks = 0;
            ticksRemaining = 60 * 5;
            this.updateInterval = updateInterval;
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
        public bool Active => ticksRemaining > 0;
        public ColoredString Draw() {
            var result = message.SubString(0, index).SetOpacity((byte)Math.Min(255, ticksRemaining * 255 / 30));
            if(flashTicks > 0) {
                var value = 255 * Math.Min(1, ticks / 30f);
                result.SetBackground(new Color(value, 0, 0));
            }
            return result;
        }
    }
}
