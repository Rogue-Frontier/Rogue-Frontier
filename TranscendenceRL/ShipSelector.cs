using Common;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Xna.Framework.Input.Keys;

namespace TranscendenceRL {
    class ShipSelector : Window {
        TypeCollection types;
        List<ShipClass> playable;
        int index;

        public ShipSelector(int width, int height, TypeCollection types) : base(width, height) {
            this.types = types;
            this.playable = types.shipClass.Values.Where(sc => sc.playerSettings?.startingClass == true).ToList();
            this.index = 0;
        }
        public override void Draw(TimeSpan drawTime) {
            Clear();
            var current = playable[index];
            
            var map = current.playerSettings.map;
            var mapWidth = map.Select(line => line.Length).Max();
            var mapX = 0;
            var mapY = 3;
            foreach(var line in current.playerSettings.map) {
                Print(mapX, mapY, line);
                mapY++;
            }

            string s = "[Image is for promotional use only]";
            var strX = Width/4 - s.Length / 2;
            Print(strX, mapY, s);

            var nameX = Width / 4 - current.name.Length/2;
            var nameY = 2;
            Print(nameX, nameY, current.name);

            var descX = Width / 2;
            var descY = 2;
            foreach(var line in current.playerSettings.description.Wrap(Width/2)) {
                Print(descX, descY, line);
                descY++;
            }

            descY++;

            //Show installed devices on the right pane
            Print(descX, descY, "Installed Devices:");
            descY++;
            foreach (var device in current.devices.Generate(types)) {
                Print(descX+4, descY, device.source.type.name);
            }

            if (index > 0) {
                string leftArrow = "<===  [Left Arrow]";
                Print(Width / 3 - leftArrow.Length - 1, 0, leftArrow);
            }
            if(index < playable.Count - 1) {
                string rightArrow = "[Right Arrow] ===>";
                Print(Width * 2 / 3 + 1, 0, rightArrow);
            }

            string start = "[Enter] Start";
            Print(Width - start.Length, Height - 1, start);

            base.Draw(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(Right)) {
                index = (index+1)%playable.Count;
            }
            if(info.IsKeyPressed(Left)){
                index = (playable.Count + index - 1) % playable.Count;
            }
            if(info.IsKeyPressed(Enter)) {
                Hide();
                new CrawlScreen(Width, Height, types, playable[index]).Show(true);
            }
            return base.ProcessKeyboard(info);
        }
    }
}
