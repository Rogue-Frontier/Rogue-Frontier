using Common;
using SadConsole;
using SadConsole.Input;
using SadConsole.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SadConsole.Input.Keys;
using Console = SadConsole.Console;

namespace TranscendenceRL {
    class ShipSelector : Console {
        World World;
        List<ShipClass> playable;
        int index;

        public ShipSelector(int width, int height, World World) : base(width, height) {
            this.World = World;
            this.playable = World.types.shipClass.Values.Where(sc => sc.playerSettings?.startingClass == true).ToList();
            this.index = 0;
        }
        public override void Draw(TimeSpan drawTime) {
            this.Clear();

            var current = playable[index];
            
            var map = current.playerSettings.map;
            var mapWidth = map.Select(line => line.Length).Max();
            var mapX = 0;
            var mapY = 3;
            foreach(var line in current.playerSettings.map) {
                this.Print(mapX, mapY, line);
                mapY++;
            }

            string s = "[Image is for promotional use only]";
            var strX = Width/4 - s.Length / 2;
            this.Print(strX, mapY, s);

            var nameX = Width / 4 - current.name.Length/2;
            var nameY = 2;
            this.Print(nameX, nameY, current.name);

            var descX = Width / 2;
            var descY = 2;
            foreach(var line in current.playerSettings.description.Wrap(Width/2)) {
                this.Print(descX, descY, line);
                descY++;
            }

            descY++;

            //Show installed devices on the right pane
            this.Print(descX, descY, "Installed Devices:");
            descY++;
            foreach (var device in current.devices.Generate(World.types)) {
                this.Print(descX+4, descY, device.source.type.name);
            }

            if (index > 0) {
                string leftArrow = "<===  [Left Arrow]";
                this.Print(Width / 3 - leftArrow.Length - 1, 0, leftArrow);
            }
            if(index < playable.Count - 1) {
                string rightArrow = "[Right Arrow] ===>";
                this.Print(Width * 2 / 3 + 1, 0, rightArrow);
            }

            string start = "[Enter] Start";
            this.Print(Width - start.Length, Height - 1, start);

            base.Draw(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(Right)) {
                index = (index+1)%playable.Count;
            }
            if(info.IsKeyPressed(Left)){
                index = (playable.Count + index - 1) % playable.Count;
            }
            if(info.IsKeyPressed(Escape)) {
                SadConsole.Game.Instance.Screen = new TitleConsole(Width, Height) { IsFocused = true };
            }
            if(info.IsKeyPressed(Enter)) {
                SadConsole.Game.Instance.Screen = new CrawlScreen(Width, Height, World, playable[index]) { IsFocused = true };
            }
            return base.ProcessKeyboard(info);
        }
    }
}
