using Common;
using IslandHopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public static class SGrid {
        public static T Get<T>(this GridTree<T> g, XY xy) {
            return g.Get(xy.xi, xy.yi);
        }
        public static T At<T>(this GridTree<T> g, XY xy) {
            return g.At(xy.xi, xy.yi);
        }
        public static void Set<T>(this GridTree<T> g, XY xy, T t) {
            g.Set(xy.xi, xy.yi, t);
        }
        public static bool IsInit<T>(this GeneratedGrid<T> g, XY xy, out T t) {
            if(g.IsInit(xy.xi, xy.yi)) {
                t = g.Get(xy.xi, xy.yi);
                return true;
            }
            t = default(T);
            return false;
        }
    }
    public interface GridTree<T> {
        T Get(int x, int y);
        ref T At(int x, int y);
        void Set(int x, int y, T t);
    }
    class QTree<T> : GridTree<T> {
        T center;
        Dictionary<(uint, uint), Section> q1, q2, q3, q4;
        private uint level;
        private uint scale;
        public uint size => (uint)Math.Pow(scale, level);
        public QTree(uint level = 1, uint scale = 8) {
            q1 = new Dictionary<(uint, uint), Section>();
            q2 = new Dictionary<(uint, uint), Section>();
            q3 = new Dictionary<(uint, uint), Section>();
            q4 = new Dictionary<(uint, uint), Section>();
            this.level = level;
            this.scale = scale;
        }
        public T Get(int x, int y) {
            if(x == 0 && y == 0) {
                return center;
            }
            var quadrant = GetQuadrant(x, y);
            uint xa = (uint)Math.Abs(x);
            uint ya = (uint)Math.Abs(y);

            uint xIndex = xa / size;
            uint yIndex = ya / size;
            if(quadrant.TryGetValue((xIndex, yIndex), out Section section)) {
                return section.Get(xa - xIndex * size, ya - yIndex * size);
            } else {
                return default(T);
            }
        }
        public ref T At(int x, int y) {
            if (x == 0 && y == 0) {
                return ref center;
            }
            var quadrant = GetQuadrant(x, y);
            uint xa = (uint)Math.Abs(x);
            uint ya = (uint)Math.Abs(y);

            uint xIndex = xa / size;
            uint yIndex = ya / size;
            if (!quadrant.TryGetValue((xIndex, yIndex), out Section section)) {
                Initialize(out section);
                quadrant[(xIndex, yIndex)] = section;
            }
            return ref section.At(xa - xIndex * size, ya - yIndex * size);
        }
        public void Set(int x, int y, T t) {
            if (x == 0 && y == 0) {
                center = t;
            }
            var quadrant = GetQuadrant(x, y);
            uint xa = (uint)Math.Abs(x);
            uint ya = (uint)Math.Abs(y);

            uint xIndex = xa / size;
            uint yIndex = ya / size;
            if (!quadrant.TryGetValue((xIndex, yIndex), out Section section)) {
                Initialize(out section);
                quadrant[(xIndex, yIndex)] = section;
            }
            section.Set(xa - xIndex * size, ya - yIndex * size, t);
        }
        private void Initialize(out Section section) {
            if(level == 1) {
                section = new Leaf(scale);
            } else {
                section = new Quadrant(level - 1, scale);
            }
        }
        private ref Dictionary<(uint, uint), Section> GetQuadrant(int x, int y) {
            if(x > 0 && y > 0) {
                return ref q1;
            } else if(x < 0 && y > 0) {
                return ref q2;
            } else if(x < 0 && y < 0) {
                return ref q3;
            } else {
                return ref q4;
            }
        }
        interface Section {
            T Get(uint x, uint y);
            ref T At(uint x, uint y);
            void Set(uint x, uint y, T t);
        }
        class Quadrant : Section {
            private Section[,] sections;
            private uint scale;
            private uint level;
            public uint size => (uint)Math.Pow(scale, level);
            public Quadrant(uint level, uint scale = 8) {
                sections = new Section[scale, scale];
                this.level = level;
                this.scale = scale;
            }
            public T Get(uint x, uint y) {
                uint xIndex = x / size;
                uint yIndex = y / size;
                var section = sections[xIndex, yIndex];
                if(section == null) {
                    return default(T);
                } else {
                    return section.Get(x - xIndex * size, y - yIndex * size);
                }
            }
            public ref T At(uint x, uint y) {
                uint xIndex = x / size;
                uint yIndex = y / size;

                ref Section section = ref sections[xIndex, yIndex];
                if (section == null) {
                    Initialize(out section);
                }
                return ref section.At(x - xIndex * size, y - yIndex * size);
            }
            public void Set(uint x, uint y, T t) {
                uint xIndex = x / size;
                uint yIndex = y / size;

                ref Section section = ref sections[xIndex, yIndex];
                if(section == null) {
                    Initialize(out section);
                }
                section.Set(x - xIndex * size, y - yIndex * size, t);
            }
            private void Initialize(out Section section) {
                if (level == 1) {
                    section = new Leaf(scale);
                } else {
                    section = new Quadrant(level - 1, scale);
                }
            }
            
        }
        class Leaf : Section {
            private T[,] items;
            private uint scale;
            public Leaf(uint scale) {
                this.scale = scale;
                items = new T[scale, scale];
            }
            public T Get(uint x, uint y) {
                return items[x, y];
            }
            public ref T At(uint x, uint y) {
                return ref items[x, y];
            }
            public void Set(uint x, uint y, T t) {
                items[x, y] = t;
            }
        }
    }
}
