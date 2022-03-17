using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using SadRogue.Primitives;
using System;
using SadConsole;
using static UI;
using Console = SadConsole.Console;

namespace RogueFrontier;

public class WreckScene : Console {
    ScreenSurface prev;
    ExchangeModel model;
    public WreckScene(ScreenSurface prev, PlayerShip player, Wreck docked) : base(prev.Surface.Width, prev.Surface.Height) {
        this.prev = prev;
        model = new(new(player.name, player.cargo), new(docked.name, docked.cargo), Enter, Exit);
    }
    public void Enter() {
        var i = model.currentItem;
        model.from.items.Remove(i);
        model.to.items.Add(i);
    }
    public void Exit() {
        Parent.Children.Remove(this);
        prev.IsFocused = true;
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        model.ProcessKeyboard(keyboard);
        return base.ProcessKeyboard(keyboard);
    }
    public override void Update(TimeSpan delta) {
        model.Update();
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        model.Render(this);
        base.Render(delta);
    }
}
