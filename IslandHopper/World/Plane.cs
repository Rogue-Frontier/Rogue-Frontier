using Common;
using IslandHopper.World;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IslandHopper {
    interface Standable : Entity {

    }
    class Plane : Entity, Standable {
        string plane =
            "           ###           \n" +
            "          #####          \n" +
            "          #####          \n" +
            "        #########        \n" +
            "      #############      \n" +
            "    #################    \n" +
            "  ####    #####    ####  \n" +
            "          #####          \n" +
            "          #####          \n" +
            "         #######         \n" +
            "        #########        \n";

        public Island World { get; set; }
        public XYZ Position { get; set; }
        public XYZ Velocity { get; set; }
        public bool Active { get; set; }
        public Plane(Island World, XYZ Position, XYZ Velocity) {
            this.World = World;
            this.Position = Position;
            this.Velocity = Velocity;
        }

        public void UpdateStep() {

        }
        public void UpdateRealtime(TimeSpan timeSpan) {

        }

        public void OnAdded() {
            HashSet<Segment> segments = new HashSet<Segment>();
            var grid = plane.Split('\n').Select(line => line.Split().ToArray()).ToArray();
            var gridCenter = new XY(grid[0].Length / 2, grid.Length / 2);
            for(int y = 0; y < grid.Length; y++) {
                for(int x = 0; x < grid[y].Length; x++) {
                    XY offset = new XY(x, y) - gridCenter;
                    var s = new Segment(this, Position + offset);
                    segments.Add(s);
                    World.AddEntity(s);
                }
            }
        }
        public void OnRemoved() {

        }
        public ColoredGlyph SymbolCenter => new ColoredGlyph(Color.Green, Color.Black, '#');
        public ColoredGlyph SymbolAbove => new ColoredGlyph(Color.Green, Color.Black, '+');
        public ColoredString Name => new ColoredString("Plane", Color.White, Color.Black);

    }
}
