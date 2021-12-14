using SadRogue.Primitives;
using SadConsole;

namespace IslandHopper;

public interface PlayerMessage {
    ColoredString Desc { get; }
    int ScreenTime { get; set; }
}
public class InfoEvent : PlayerMessage {
    public int ScreenTime { get; set; } = 150;
    public ColoredString Desc { get; }
    public InfoEvent(ColoredString Desc) {
        this.Desc = Desc;
    }
    public InfoEvent(string Desc, Color? foreground = null) {
        this.Desc = new ColoredString(Desc, foreground ?? Color.White, Color.Black);
    }
}
