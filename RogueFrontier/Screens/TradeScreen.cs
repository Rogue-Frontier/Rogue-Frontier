using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Console = SadConsole.Console;
using static UI;
using SadRogue.Primitives;
using Common;

namespace RogueFrontier;

public interface ITrader {
    string name { get; }
    HashSet<Item> cargo { get; }

    //public static implicit operator Dealer(ITrader r) => new(r.name, r.cargo);
}
public delegate int GetPrice(Item i);
public class TradeMenu : Console {
    Console prev;
    Player player;
    ExchangeModel model;
    GetPrice GetBuyPrice, GetSellPrice;
    public TradeMenu(Console prev, PlayerShip playerShip, ITrader docked, GetPrice GetBuyPrice, GetPrice GetSellPrice) : base(prev.Width, prev.Height) {
        this.prev = prev;
        this.player = playerShip.player;
        model = new(new(playerShip.name, playerShip.cargo), new(docked.name, docked.cargo), Transact, Transition);
        this.GetBuyPrice = GetBuyPrice;
        this.GetSellPrice = GetSellPrice;
    }
    public void Transact() {
        var item = model.currentItem;
        if (model.traderIndex == 0) {
            var price = GetSellPrice(item);
            if (price == -1) {
                return;
            }
            player.money += price;
        } else {
            var price = GetBuyPrice(item);
            if(price == -1) {
                return;
            }
            if (player.money < price) {
                return;
            }
            player.money -= price;
        }
        model.from.items.Remove(item);
        model.to.items.Add(item);
    }
    public void Transition() {
        var p = Parent;
        p.Children.Remove(this);
        if (prev != null) {
            p.Children.Add(prev);
            prev.IsFocused = true;
        } else {
            p.IsFocused = true;
        }
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
        int x = 16;
        int y = 10;
        var f = Color.White;
        var b = Color.Black;
        this.Print(x, y++, $"Money: {$"{player.money}".PadLeft(8)}", f, b);
        var item = model.currentItem;
        var value = item == null ? -1 : model.traderIndex == 0 ? GetSellPrice(item) : GetBuyPrice(item);
        if (value > -1) {
            f = Color.Yellow;
            this.Print(x, y++, $"       {$"{value}".PadLeft(8)}{(model.traderIndex == 0 ? '+' : '-')}", f, b);
        }
        if (item != null) {
            x = 33;
            y = 4;
            this.Print(x, y++, item.type.name, Color.White, Color.Black);
            y++;
            foreach(var line in item.type.desc.SplitLine(90)) {
                this.Print(x, y++, line, Color.White, Color.Black);
            }
        }
        base.Render(delta);
    }
}
