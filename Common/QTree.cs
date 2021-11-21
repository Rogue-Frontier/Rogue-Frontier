using Common;
using System;
using System.Collections.Generic;
namespace Common {
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
    public class QTree<T> : GridTree<T> {
        public T center;
        public Dictionary<(uint, uint), Section> q1, q2, q3, q4;
        public Dictionary<uint, Segment> xPositive, xNegative, yPositive, yNegative;
        public uint level;
        public uint scale;
        public int segmentCount => q1.Count + q2.Count + q3.Count + q4.Count;
        public uint size => (uint)Math.Pow(scale, level);
        public QTree(uint level = 1, uint scale = 32) {
            q1 = new Dictionary<(uint, uint), Section>();
            q2 = new Dictionary<(uint, uint), Section>();
            q3 = new Dictionary<(uint, uint), Section>();
            q4 = new Dictionary<(uint, uint), Section>();
            xPositive = new Dictionary<uint, Segment>();
            xNegative = new Dictionary<uint, Segment>();
            yPositive = new Dictionary<uint, Segment>();
            yNegative = new Dictionary<uint, Segment>();
            this.level = level;
            this.scale = scale;
        }
        public void Clear() {
            center = default(T);
            foreach(var d in new Dictionary<(uint, uint), Section>[] { q1, q2, q3, q4}) {
                d.Clear();
            }
            foreach(var d in new Dictionary<uint, Segment>[] { xPositive, xNegative, yPositive, yNegative }) {
                d.Clear();
            }
        }
        public const int CODE_OFFSET = 2;
        public const int CODE_SHIFT = 2;
        public const int CODE_ORIGIN =      (0  + CODE_OFFSET) | ((0  + CODE_OFFSET) << CODE_SHIFT);
        public const int CODE_X_POSITIVE =  (1  + CODE_OFFSET) | ((0  + CODE_OFFSET) << CODE_SHIFT);
        public const int CODE_X_NEGATIVE =  (-1 + CODE_OFFSET) | ((0  + CODE_OFFSET) << CODE_SHIFT);
        public const int CODE_Y_POSITIVE =  (0  + CODE_OFFSET) | ((1  + CODE_OFFSET) << CODE_SHIFT);
        public const int CODE_Y_NEGATIVE =  (0  + CODE_OFFSET) | ((-1 + CODE_OFFSET) << CODE_SHIFT);

        public const int CODE_QUADRANT_1 =  (1  + CODE_OFFSET) | ((1  + CODE_OFFSET) << CODE_SHIFT);
        public const int CODE_QUADRANT_2 =  (-1 + CODE_OFFSET) | ((1  + CODE_OFFSET) << CODE_SHIFT);
        public const int CODE_QUADRANT_3 =  (-1 + CODE_OFFSET) | ((-1 + CODE_OFFSET) << CODE_SHIFT);
        public const int CODE_QUADRANT_4 =  (1  + CODE_OFFSET) | ((-1 + CODE_OFFSET) << CODE_SHIFT);
        public static int SignCode(int x, int y) => (Math.Sign(x) + CODE_OFFSET) | ((Math.Sign(y) + CODE_OFFSET) << CODE_SHIFT);
        
        public ref T this[(int x, int y) p] => ref At(p.x, p.y);
        public T Get(int x, int y) {
            switch(SignCode(x, y)) {
                case CODE_ORIGIN:
                    return center;
                case CODE_X_NEGATIVE:
                    return GetSegment((uint)Math.Abs(x), xNegative);
                case CODE_X_POSITIVE:
                    return GetSegment((uint)Math.Abs(x), xPositive);
                case CODE_Y_NEGATIVE:
                    return GetSegment((uint)Math.Abs(y), yNegative);
                case CODE_Y_POSITIVE:
                    return GetSegment((uint)Math.Abs(y), yPositive);
                case CODE_QUADRANT_1:
                    return GetSection(q1);
                case CODE_QUADRANT_2:
                    return GetSection(q2);
                case CODE_QUADRANT_3:
                    return GetSection(q3);
                case CODE_QUADRANT_4:
                    return GetSection(q4);
                default:
                    throw new ArgumentOutOfRangeException("Unknown location");
            }
            T GetSegment(uint ia, Dictionary<uint, Segment> strip) {
                uint index = ia / size;
                if(strip.TryGetValue(index, out Segment segment)) {
                    return segment.Get(ia - index * size);
                } else {
                    return default(T);
                }
            }
            T GetSection(Dictionary<(uint, uint), Section> quadrant) {
                uint xa = (uint)Math.Abs(x);
                uint ya = (uint)Math.Abs(y);

                uint xIndex = xa / size;
                uint yIndex = ya / size;
                if (quadrant.TryGetValue((xIndex, yIndex), out Section section)) {
                    return section.Get(xa - xIndex * size, ya - yIndex * size);
                } else {
                    return default(T);
                }
            }
        }
        public ref T At(int x, int y) {
            var code = SignCode(x, y);
            switch (code) {
                case CODE_ORIGIN:
                    return ref center;
                case CODE_X_NEGATIVE:
                    return ref AtSegment((uint)Math.Abs(x), xNegative);
                case CODE_X_POSITIVE:
                    return ref AtSegment((uint)Math.Abs(x), xPositive);
                case CODE_Y_NEGATIVE:
                    return ref AtSegment((uint)Math.Abs(y), yNegative);
                case CODE_Y_POSITIVE:
                    return ref AtSegment((uint)Math.Abs(y), yPositive);
                case CODE_QUADRANT_1:
                    return ref AtSection(q1);
                case CODE_QUADRANT_2:
                    return ref AtSection(q2);
                case CODE_QUADRANT_3:
                    return ref AtSection(q3);
                case CODE_QUADRANT_4:
                    return ref AtSection(q4);
                default:
                    throw new ArgumentOutOfRangeException("Unknown location");
            }
            ref T AtSegment(uint ia, Dictionary<uint, Segment> strip) {
                uint index = ia / size;
                if (!strip.TryGetValue(index, out Segment segment)) {
                    Initialize(out segment);
                    strip[index] = segment;
                }
                return ref segment.At(ia - index * size);
            }
            ref T AtSection(Dictionary<(uint, uint), Section> quadrant) {
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
        }
        public void Set(int x, int y, T t) {

            switch (SignCode(x, y)) {
                case CODE_ORIGIN:
                    center = t;
                    break;
                case CODE_X_NEGATIVE:
                    SetSegment((uint)Math.Abs(x), xNegative);
                    break;
                case CODE_X_POSITIVE:
                    SetSegment((uint)Math.Abs(x), xPositive);
                    break;
                case CODE_Y_NEGATIVE:
                    SetSegment((uint)Math.Abs(y), yNegative);
                    break;
                case CODE_Y_POSITIVE:
                    SetSegment((uint)Math.Abs(y), yPositive);
                    break;
                case CODE_QUADRANT_1:
                    SetSection(q1);
                    break;
                case CODE_QUADRANT_2:
                    SetSection(q2);
                    break;
                case CODE_QUADRANT_3:
                    SetSection(q3);
                    break;
                case CODE_QUADRANT_4:
                    SetSection(q4);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown location");
            }
            void SetSegment(uint ia, Dictionary<uint, Segment> strip) {
                uint index = ia / size;
                if (!strip.TryGetValue(index, out Segment segment)) {
                    Initialize(out segment);
                    strip[index] = segment;
                }
                segment.Set(ia - index * size, t);
            }
            void SetSection(Dictionary<(uint, uint), Section> quadrant) {
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
        }
        private void Initialize(out Section section) {
            if(level == 1) {
                section = new Leaf(scale);
            } else {
                section = new Quadrant(level - 1, scale);
            }
        }
        private void Initialize(out Segment segment) {
            if (level == 1) {
                segment = new Slice(scale);
            } else {
                segment = new Strip(level - 1, scale);
            }
        }

        public interface Segment {
            T Get(uint i);
            ref T At(uint i);
            void Set(uint x, T t);
        }
        public class Strip : Segment {
            public Dictionary<uint, Segment> segments;
            public uint scale;
            public uint level;
            public uint size => (uint)Math.Pow(scale, level);
            public Strip(uint level, uint scale = 8) {
                segments = new Dictionary<uint, Segment>();
                this.level = level;
                this.scale = scale;
            }
            public T Get(uint i) {
                uint index = i / size;
                if (segments.TryGetValue(index, out var segment)) {
                    return segment.Get(i - index * size);
                } else {
                    return default(T);
                }
            }
            public ref T At(uint i) {
                uint index = i / size;

                if (!segments.TryGetValue(index, out var segment)) {
                    Initialize(out segment);
                    segments[index] = segment;
                }
                return ref segment.At(i - index * size);
            }
            public void Set(uint i, T t) {
                uint index = i / size;

                if (!segments.TryGetValue(index, out var section)) {
                    Initialize(out section);
                    segments[index] = section;
                }
                section.Set(i - index * size, t);
            }
            private void Initialize(out Segment section) {
                if (level == 1) {
                    section = new Slice(scale);
                } else {
                    section = new Strip(level - 1, scale);
                }
            }
        }
        public class Slice : Segment {
            public T[] items;
            public uint scale;
            public Slice(uint scale) {
                this.scale = scale;
                items = new T[scale];
            }
            public T Get(uint i) {
                return items[i];
            }
            public ref T At(uint i) {
                return ref items[i];
            }
            public void Set(uint i, T t) {
                items[i] = t;
            }
        }

        public interface Section {
            T Get(uint x, uint y);
            ref T At(uint x, uint y);
            void Set(uint x, uint y, T t);
        }
        public class Quadrant : Section {
            public Dictionary<(uint, uint), Section> sections;
            public uint scale;
            public uint level;
            public uint size => (uint)Math.Pow(scale, level);
            public Quadrant(uint level, uint scale = 8) {
                sections = new Dictionary<(uint, uint), Section>();
                this.level = level;
                this.scale = scale;
            }
            public T Get(uint x, uint y) {
                uint xIndex = x / size;
                uint yIndex = y / size;
                if(sections.TryGetValue((xIndex, yIndex), out var section)) {
                    return section.Get(x - xIndex * size, y - yIndex * size);
                } else {
                    return default(T);
                }
            }
            public ref T At(uint x, uint y) {
                uint xIndex = x / size;
                uint yIndex = y / size;

                if(!sections.TryGetValue((xIndex, yIndex), out var section)) {
                    Initialize(out section);
                    sections[(xIndex, yIndex)] = section;
                }
                return ref section.At(x - xIndex * size, y - yIndex * size);
            }
            public void Set(uint x, uint y, T t) {
                uint xIndex = x / size;
                uint yIndex = y / size;

                if (!sections.TryGetValue((xIndex, yIndex), out var section)) {
                    Initialize(out section);
                    sections[(xIndex, yIndex)] = section;
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
            public T[,] items;
            public uint scale;
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
