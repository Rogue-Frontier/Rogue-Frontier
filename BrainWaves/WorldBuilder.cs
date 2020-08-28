using BrainWaves;
using Common;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrainWaves {
    class WorldBuilder {
        World World;
        HashSet<Rectangle> rooms = new HashSet<Rectangle>();
        HashSet<(int, int)> built = new HashSet<(int, int)>();
        public WorldBuilder(World World) {
            this.World = World;
        }
        public void Build() {
            Random rnd = new Random(0);
            int gridSize = 6;

            RoomSource mainRoom = new RoomSource() {
                Position = new XY(0, 0),
                Size = new XY(gridSize * 3, gridSize * 3)
            };
            BuildRoom(mainRoom);

            Queue<HallwaySource> hallways = new Queue<HallwaySource>();
            {
                var direction = XY.Polar((rnd.Next(0, 4) * 90) * Math.PI / 180);
                HallwaySource mainHall = new HallwaySource() {
                    Position = mainRoom.Position + direction * gridSize * 3 / 2,
                    Direction = direction,
                    Size = gridSize
                };
                hallways.Enqueue(mainHall);

            }
            int i = 0;
            while(hallways.Count > 0) {
                var h = hallways.Dequeue();

                BuildHall(h);
                /*
                if(rnd.Next(0, 10) > 0) {
                    hallways.Enqueue(h);
                }
                */
                if(h.StepsFromBranch > 2) {

                    if (rnd.Next(0, 4) > 0) {
                        if (rnd.Next(0, 2) == 0) {
                            var direction = h.Direction.Rotate(90 * Math.PI / 180);
                            var branch = new HallwaySource() { Position = h.Position, Direction = direction, Size = h.Size };
                            TryBuildBranch(branch);
                        } else if (rnd.Next(0, 2) == 0) {
                            var direction = h.Direction.Rotate(90 * Math.PI / 180);
                            BuildSideRoom(direction);
                        }

                        if (rnd.Next(0, 2) == 0) {
                            var direction = h.Direction.Rotate(-90 * Math.PI / 180);
                            var branch = new HallwaySource() { Position = h.Position, Direction = direction, Size = h.Size };
                            TryBuildBranch(branch);
                        } else if (rnd.Next(0, 2) == 0) {
                            var direction = h.Direction.Rotate(-90 * Math.PI / 180);
                            BuildSideRoom(direction);
                        }
                        h.StepsFromBranch = 0;
                        void BuildSideRoom(XY direction) {
                            //Go to the side, and then backtrack
                            var points = Enumerable.Range(0, h.StepsFromBranch)
                                .Select(i => h.Position + direction * h.Size - h.Direction * i);
                            if (points.Select(p => (p.xi, p.yi)).All(IsOpen)) {

                                BuildRoom(new RoomSource() {
                                    Position = h.Position + direction * h.Size - new XY(h.Size/2, h.Size/2),
                                    Size = direction * gridSize - h.Direction * gridSize * h.StepsFromBranch
                                });
                            }
                            var branch = new HallwaySource() { Position = h.Position, Direction = direction, Size = h.Size };
                            TryBuildDoorway(branch);
                        }
                    } else if (rnd.Next(0, 2) > 0) {
                        var p = h.Position + h.Direction * gridSize * 1.5;

                        if(IsOpen(p)) {
                            var r = new RoomSource() {
                                Position = p,
                                Size = new XY(gridSize * 3, gridSize * 3)
                            };
                            BuildRoom(r);
                            for (int j = 0; j < 2; j++) {
                                h.Step();
                            }
                        }
                    }
                }

                if (i++ < 70) {
                    h.Step();
                    h.StepsFromBranch++;
                    hallways.Enqueue(h);
                } else {
                    var p = h.Position + h.Direction * gridSize * 1.5;
                    if(IsOpen(p)) {
                        var r = new RoomSource() {
                            Position = p,
                            Size = new XY(gridSize * 3, gridSize * 3)
                        };
                        BuildRoom(r);
                    } else {
                        BuildDeadEnd(h);
                    }
                }

                void TryBuildDoorway(HallwaySource branch) {
                    if (IsOpen(branch.Position + branch.Direction * h.Size)) {
                        branch.Position += branch.Direction * (h.Size / 2);
                        BuildHall(branch);
                    }
                }
                void TryBuildBranch(HallwaySource branch) {
                    if (IsOpen(branch.Position + branch.Direction * h.Size)) {
                        branch.Position += branch.Direction * (h.Size / 2);
                        BuildHall(branch);
                        branch.Position += branch.Direction * (h.Size / 2);
                        hallways.Enqueue(branch);
                    }
                }
            }
            void BuildRoom(RoomSource r) {
                var rect = r.rect;
                foreach (var p in rect.PerimeterPositions()) {
                    BuildIfOpen(p, new Wall());
                }

                rect = rect.WithPosition(rect.Position + new Point(1, 1)).WithSize(rect.Size - new Point(2, 2));
                foreach (var p in rect.Positions()) {
                    Build(p, new Floor());
                }
            }
            void BuildHall(HallwaySource h) {
                for (int indexForward = -h.Size/2; indexForward <= h.Size/2 /* 0 */; indexForward++) {
                    var forward = h.Direction;
                    var left = h.Direction.Rotate(90 * Math.PI / 180);
                    var right = h.Direction.Rotate(-90 * Math.PI / 180);

                    BuildIfOpen(h.Position + forward * indexForward + left * h.Size / 2, new Wall());
                    BuildIfOpen(h.Position + forward * indexForward + right * h.Size / 2, new Wall());
                    for (int indexSide = 0; indexSide < h.Size / 2; indexSide++) {
                        Build(h.Position + forward * indexForward + left * indexSide, new Floor());
                        Build(h.Position + forward * indexForward + right * indexSide, new Floor());
                    }
                }
            }
            void BuildDeadEnd(HallwaySource h) {
                var forward = h.Direction;
                var left = h.Direction.Rotate(90 * Math.PI / 180);
                var right = h.Direction.Rotate(-90 * Math.PI / 180);
                for (int indexSide = 0; indexSide < h.Size; indexSide++) {
                    Build(h.Position + forward * h.Size/2 - left * h.Size/2 + right * indexSide, new Floor());
                }
            }
            void BuildIfOpen((int x, int y) point, Voxel v) {
                if(IsOpen(point)) {
                    Build(point, v);
                }
            }
            bool IsOpen((int x, int y) point) => World.voxels.Get(point.x, point.y) == null;
            void Build((int x, int y) point, Voxel v) {
                World.voxels.Set(point.x, point.y, v);
                built.Add(point);
            }

        }
        class RoomSource {
            public XY Position;
            public XY Size;
            public Rectangle rect => new Rectangle(Position - Size/2, Position + Size/2);
        }
        class HallwaySource {
            public XY Position;
            public XY Direction;
            public int Size;
            public int StepsFromBranch;
            public void Step() => Position += Direction * Size;
        }
    }
}
