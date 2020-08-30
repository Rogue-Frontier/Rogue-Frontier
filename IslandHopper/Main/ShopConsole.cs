using Common;
using static SadConsole.Input.Keys;
using SadRogue.Primitives;
using SadConsole;
using SadConsole.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Xml.Linq;
using static IslandHopper.Constants;
using SadConsole.Input;
using System.IO;
using Console = SadConsole.Console;
using ArchConsole;

namespace IslandHopper {
    public class ShopConsole : Console {
        DictCounter<ItemType> items = new DictCounter<ItemType>();
        public ShopConsole(int Width, int Height) : base(Width, Height) {
            DefaultBackground = Color.Black;
            DefaultForeground = Color.White;

            int x = 16;
            int y = 16;
            foreach(var s in StandardTypes.stdWeapons) {

                var it = s;
                var length = s.name;
                Children.Add(new Label(length) { Position = new Point(x, y) });

                int x2 = x + 24;

                Label count = null;
                
                Children.Add(new LabelButton("-", () => {
                    items.Decrement(it);
                    UpdateCount();
                }) { Position = new Point(x2, y) });

                count = new Label("0") { Position = new Point(x2 + 2, y) };
                Children.Add(count);


                Children.Add(new LabelButton("+", () => {
                    items.Increment(it);
                    UpdateCount();
                }) { Position = new Point(x2 + 2 + 2 + 2, y) });
                void UpdateCount() => count.text = new ColoredString(items[it].ToString(), Color.White, Color.Black);
                y++;
            }
        }
    }
}
