using Common;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Linq;

namespace RogueFrontier;

public interface IPlayerMessage {
    bool Active { get; }
    ColoredString message { get; }
    string text => message.String;
    void Reset();
    void Update();
    ColoredString Draw();
    bool Equals(IPlayerMessage other);
}
public class Transmission : IPlayerMessage {
    public Entity source;
    public Message info;
    public Transmission() { }
    public Transmission(Entity source, string message, int updateInterval = 3) {
        this.source = source;
        this.info = new Message(message, updateInterval);
    }
    public Transmission(Entity source, ColoredString message, int updateInterval = 3) {
        this.source = source;
        this.info = new Message(message, updateInterval);
    }
    public bool Active => info.Active;
    public ColoredString message => info.message;
    public string text => message.String;
    public void Reset() => info.Reset();
    public ColoredString Draw() => info.Draw();
    public void Update() => info.Update();
    public bool Equals(IPlayerMessage other) {
        return other is Transmission t && t.source == source && t.text == text;
    }
}
public class Message : IPlayerMessage {
    [JsonProperty]
    public ColoredString message { get; private set; }
    public string text => message.String;
    public int index;
    public int ticks;
    public int ticksRemaining;
    public int updateInterval;
    public int flashTicks;
    public Message() { }
    public Message(string text, int updateInterval = 3) : this(
        new ColoredString(text, Color.White, Color.Transparent), updateInterval) {

    }
    public Message(ColoredString message, int updateInterval = 3) {
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
        if (index < message.Count()) {
            if (ticks++ % updateInterval == 0)
                index++;
        } else if (ticksRemaining > 0) {
            ticksRemaining--;
        }
        if (flashTicks > 0) {
            flashTicks--;
        }
    }
    public bool Scrolling => ticks < message.Count();
    public bool Active => ticksRemaining > 0;
    public ColoredString Draw() {
        var result = message.SubString(0, index).WithOpacity((byte)Math.Min(255, ticksRemaining * 255 / Program.TICKS_PER_SECOND));
        if (flashTicks > 0) {
            var value = 255 * Math.Min(1, ticks / (float)Program.TICKS_PER_SECOND);
            result.SetBackground(new Color(value, 0, 0));
        }
        return result;
    }
    public bool Equals(IPlayerMessage other) {
        return other is Message m && m.text == text;
    }
}
