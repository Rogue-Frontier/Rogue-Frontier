using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using SadRogue.Primitives;
using System;
using SadConsole;
using Console = SadConsole.Console;
using Common;
namespace RogueFrontier;
public class WreckScene : Console {
    Player player;
    ScreenSurface prev;
    ExchangeModel model;
    public WreckScene(ScreenSurface prev, PlayerShip playerShip, Wreck docked) : base(prev.Surface.Width, prev.Surface.Height) {
        this.player = playerShip.person;
        this.prev = prev;
        model = new(new(playerShip.name, playerShip.cargo), new(docked.name, docked.cargo), Transfer, Exit);
    }
    public void Transfer() {
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
        model.Render(this); int x = 6;
        int y = 4;
        var f = Color.White;
        var b = Color.Black;
        this.Print(x, y++, $"Money: {$"{player.money}".PadLeft(8)}", f, b);
        var item = model.currentItem;

        if (item == null) {
            goto Done;
        }
        x = 27;
        y = 4;
        this.Print(x, y++, item.type.name, Color.Yellow, b);
        y++;
        foreach (var line in item.type.desc.SplitLine(92)) {
            this.Print(x, y++, line, f, b);
        }
    Done:
        base.Render(delta);
    }
}
