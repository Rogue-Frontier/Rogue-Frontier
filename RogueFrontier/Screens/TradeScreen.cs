using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Console = SadConsole.Console;
using static UI;
using SadRogue.Primitives;

namespace RogueFrontier;

interface ITrader {
    string name { get; }
    HashSet<Item> cargo { get; }

    //public static implicit operator Dealer(ITrader r) => new(r.name, r.cargo);
}
class TradeScene : Console {
    Console next;
    Player player;
    PlayerShip playerShip;
    ITrader docked;
    ExchangeModel model;

    public TradeScene(Console next, PlayerShip playerShip, ITrader docked) : this(next, next, playerShip, docked) { }
    public TradeScene(Console prev, Console next, PlayerShip playerShip, ITrader docked) : base(prev.Width, prev.Height) {
        this.next = next;
        this.player = playerShip.player;
        this.playerShip = playerShip;
        this.docked = docked;
        model = new(new(playerShip.name, playerShip.cargo), new(docked.name, docked.cargo), Transact, Transition);
    }
    public void Transact() {
        var item = model.currentItem;
        if (model.traderIndex == 0) {
            player.money += item.type.value;
        } else if (player.money >= item.type.value) {
            player.money -= item.type.value;
        } else {
            return;
        }
        model.from.items.Remove(item);
        model.to.items.Add(item);
    }
    public void Transition() {
        var p = Parent;
        p.Children.Remove(this);
        if (next != null) {
            p.Children.Add(next);
            next.IsFocused = true;
        } else {
            p.IsFocused = true;
        }
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        model.ProcessKeyboard(keyboard);
        return base.ProcessKeyboard(keyboard);
    }
    public override void Render(TimeSpan delta) {
        
        model.Render(this);
        
        int x = 16;
        int y = 16 + 26 + 2;
        var f = Color.White;
        var b = Color.Black;
        this.Print(x, y++, $"Money: {$"{player.money}".PadLeft(8)}", f, b);
        var item = model.currentItem;
        if (item != null) {
            f = Color.Yellow;

            int d = model.traderIndex == 0 ? item.type.value : -item.type.value;

            this.Print(x, y++, $"       {$"{item.type.value}".PadLeft(8)}{(model.traderIndex == 0 ? '+' : '-')}", f, b);
            //y++;
            //this.Print(x, y++, $"       {$"{player.money + d}".PadLeft(8)}", f, b);
        }
        base.Render(delta);
    }
}
