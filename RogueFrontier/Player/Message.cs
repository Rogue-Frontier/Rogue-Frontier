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
    void Update(double delta);
    ColoredString Draw();
    bool Equals(IPlayerMessage other);
}
public class Transmission : IPlayerMessage {
    public Entity source;
    public Message info;
    public Transmission() { }
    public Transmission(Entity source, string message) {
        this.source = source;
        this.info = new Message(message);
    }
    public Transmission(Entity source, ColoredString message) {
        this.source = source;
        this.info = new Message(message);
    }
    public bool Active => info.Active;
    public ColoredString message => info.message;
    public string text => message.String;
    public void Reset() => info.Reset();
    public ColoredString Draw() => info.Draw();
    public void Update(double delta) => info.Update(delta);
    public bool Equals(IPlayerMessage other) {
        return other is Transmission t && t.source == source && t.text == text;
    }
}
public class Message : IPlayerMessage {
    [JsonProperty]
    public ColoredString message { get; private set; }
    public string text => message.String;
    public int index;
    public double ticks;
    public double ticksRemaining;
    public double flashTicks;
    public Message() { }
    public Message(string text) : this(
        new ColoredString(text, Color.White, Color.Black)) {

    }
    public Message(ColoredString message) {
        this.message = message;
        index = 0;
        ticks = 0;
        ticksRemaining = 5;
    }

    public void Reset() {
        ticksRemaining = 2.5;
        flashTicks = 0.25;
    }
    public void Update(double delta) {
        if (index < message.Length) {
            ticks += delta;
            index = Math.Min((int)(ticks * 20), message.Length);
        } else if (ticksRemaining > 0) {
            ticksRemaining -= delta;
        }
        if (flashTicks > 0) {
            flashTicks -= delta;
        }
    }
    public bool Scrolling => ticks < message.Count();
    public bool Active => ticksRemaining > 0;
    public ColoredString Draw() {
        var a = (byte)Math.Min(255, ticksRemaining * 255);
        var result = message.SubString(0, Math.Min(index, message.Length)).WithOpacity(a, a);
        if (flashTicks > 0) {
            var value = 255;
            result.SetBackground(new Color(value, 0, 0));
        }
        return result;
    }
    public bool Equals(IPlayerMessage other) {
        return other is Message m && m.text == text;
    }
}
