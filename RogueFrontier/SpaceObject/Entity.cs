using Common;
using SadConsole;

namespace RogueFrontier;

public interface Effect {
    XY position { get; }
    bool active { get; }
    ColoredGlyph tile { get; }
    void Update();
}
public interface Entity : Effect {
    int Id { get; }
}
