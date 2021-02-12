using Common;
using Newtonsoft.Json;
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
        public InfoMessage info;
        public Transmission() { }
        public Transmission(SpaceObject source, string message, int updateInterval = 3) {
            this.source = source;
            this.info = new InfoMessage(message, updateInterval);
        }
        public bool Active => info.Active;
        public ColoredString message => info.message;
        public void Reset() => info.Reset();
        public ColoredString Draw() => info.Draw();
        public void Update() => info.Update();
    }
    public class InfoMessage: IPlayerMessage {
        [JsonIgnore]
        public ColoredString message => new ColoredString(text, Color.White, Color.Transparent);
        public string text;
        public int index;
        public int ticks;
        public int ticksRemaining;
        public int updateInterval;
        public int flashTicks;
        public InfoMessage() { }
        public InfoMessage(string text, int updateInterval = 3) {
            this.text = text;
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
            if(index < message.Count()) {
                if(ticks++%updateInterval == 0)
                    index++;
            } else if(ticksRemaining > 0) {
                ticksRemaining--;
            }
            if(flashTicks > 0) {
                flashTicks--;
            }
        }
        public bool Scrolling => ticks < message.Count();
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
