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

        public PlayerMessage(string message) : this(new ColoredString(message, Color.White, Color.Black)) { }
        public PlayerMessage(ColoredString message) {
            this.message = message;
            index = 0;
            ticks = 0;
            ticksRemaining = 60 * 5;
        }
        public void Update() {
            if(index < message.Count) {
                if(ticks++%3 == 0)
                    index++;
            } else if(ticksRemaining > 0) {
                ticksRemaining--;
            }
        }
        public bool Active => ticksRemaining > 0;
        public ColoredString Draw() => message.SubString(0, index).SetOpacity((byte) Math.Min(255, ticksRemaining * 255 / 30));
    }
}
