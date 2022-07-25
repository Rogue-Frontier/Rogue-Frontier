using Newtonsoft.Json;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Common;

[JsonObject(MemberSerialization.OptIn)]
public class XY {
    [JsonProperty]
    public double x;
    [JsonProperty]
    public double y;
    [JsonIgnore]
    public int xi { get => (int)x; set => x = value; }
    [JsonIgnore]
    public int yi { get => (int)y; set => y = value; }
    [JsonIgnore]
    public static readonly XY Zero = new XY(0, 0);
    public XY() {
        x = 0;
        y = 0;
    }
    public XY(XY xy) {
        this.x = xy.x;
        this.y = xy.y;
    }
    public XY(Point p) {
        this.x = p.X;
        this.y = p.Y;
    }
    public static XY TryParse(XElement e, XY fallback) {
        var p = (string s) => double.Parse(s);
        if (e.TryAtt("posAngle", out var posAngle) && e.TryAtt("posRadius", out var posRadius)) {
            return Polar(p(posAngle) * Math.PI / 180, p(posRadius));
        }
        if (e.TryAtt("posX", out var posX) && e.TryAtt("posY", out var posY)) {
            return new(p(posX), p(posY));
        }
        return fallback;
    }
    public XY(double x, double y) {
        this.x = x;
        this.y = y;
    }
    public static XY operator +(XY p, XY other) => new XY(p.x + other.x, p.y + other.y);
    public static XY operator +(XY p, Point other) => new XY(p.x + other.X, p.y + other.Y);
    public static XY operator +(XY p, (int x, int y) other) => new XY(p.x + other.x, p.y + other.y);
    public static XY operator -(XY p) => new XY(-p.x, -p.y);
    public static XY operator -(XY p, XY other) => new XY(p.x - other.x, p.y - other.y);
    public static XY operator -(XY p, (int x, int y) other) => new XY(p.x - other.x, p.y - other.y);
    public static XY operator -(XY p, (long x, long y) other) => new XY(p.x - other.x, p.y - other.y);
    public static XY operator -((int x, int y) p, XY other) => new XY(p.x - other.x, p.y - other.y);
    public static XY operator *(XY p, XY other) => new XY(p.x * other.x, p.y * other.y);
    public static XY operator *(XY p, double scalar) => new XY(p.x * scalar, p.y * scalar);
    public static XY operator *(XY p, int scalar) => new XY(p.x * scalar, p.y * scalar);
    public static XY operator /(XY p, double scalar) => new XY(p.x / scalar, p.y / scalar);
    public static XY operator /(XY p, int scalar) => new XY(p.x / scalar, p.y / scalar);
    public static XY operator /(XY p, Point pt) => new XY(p.x / pt.X, p.y / pt.Y);
    public static XY operator %(XY p, XY limit) {
        XY result = new XY(p);
        while (result.x < 0) result.x += limit.x;
        while (result.y < 0) result.y += limit.y;
        while (result.x >= limit.x) result.x -= limit.x;
        while (result.y >= limit.y) result.y -= limit.y;
        return result;
    }
    public void Deconstruct(out int x, out int y) {
        x = (int)this.x;
        y = (int)this.y;
    }
    [JsonIgnore]
    public XY clone {
        get => new XY(x, y);
    }
    public XY PlusX(double x) => new XY(this.x + x, y);
    public XY PlusY(double y) => new XY(x, this.y + y);
    [JsonIgnore]
    public XY abs => new XY(Math.Abs(xi), Math.Abs(yi));
    [JsonIgnore]
    public XY truncate => new XY(xi, yi);
    [JsonIgnore]
    public XY flipX => new XY(-x, y);
    [JsonIgnore]
    public XY flipY => new XY(x, -y);
    [JsonIgnore]
    public XY round => new XY(Math.Round(x), Math.Round(y));
    [JsonIgnore]
    public XY roundDown => new XY(Math.Round(x, MidpointRounding.ToNegativeInfinity), Math.Round(y, MidpointRounding.ToNegativeInfinity));
    [JsonIgnore]
    public XY roundAway => new XY(Math.Round(x, MidpointRounding.AwayFromZero), Math.Round(y, MidpointRounding.AwayFromZero));
    public XY Step(XY other, int length = 1) {
        XY offset = other - this;
        if(offset.magnitude <= length) {
            return other;
        }
        return this + offset.WithMagnitude(length);
    }
    public IEnumerable<XY> LineTo(XY other) {
        var p = this;
        while(p != other) {
            yield return p;
            p = p.Step(other);
        }
        yield return p;
    }
    public static XY Polar(double angleRad, double magnitude = 1) {
        return new XY(Math.Cos(angleRad) * magnitude, Math.Sin(angleRad) * magnitude);
    }
    public XY Snap(int gridSize) => (this / gridSize).roundDown * gridSize;

    public XY Snap(double gridSize) => (this / gridSize).roundDown * gridSize;

    public static implicit operator (int, int)(XY p) => (p.xi, p.yi);
    public static implicit operator (double, double)(XY p) => (p.x, p.y);
    public static explicit operator XY((int x, int y) p) => new XY(p.x, p.y);
    public static explicit operator XY((double x, double y) p) => new XY(p.x, p.y);

    public double Dot(XY other) => x * other.x + y * other.y;
    [JsonIgnore]
    public bool isZero => magnitude < 0.1;
    public XY Scale(XY origin, double scale) => (this - origin) * scale + origin;
    public XY IncMagnitude(double inc) => WithMagnitude(magnitude + inc);
    public XY WithMagnitude(double magnitude) {
        var a = angleRad;
        return new XY(Math.Cos(a) * magnitude, Math.Sin(a) * magnitude);
    }
    public override string ToString() => $"({x}, {y})";

    [JsonIgnore]
    public double maxCoord => Math.Max(Math.Abs(x), Math.Abs(y));
    [JsonIgnore]
    public double manhattan => Math.Abs(x) + Math.Abs(y);
    [JsonIgnore]
    public double length => Math.Sqrt(magnitude2);
    [JsonIgnore]
    public double magnitude => Math.Sqrt(magnitude2);
    [JsonIgnore]
    public double magnitude2 => (x * x + y * y);
    [JsonIgnore]
    public XY normal {
        get {
            double magnitude = this.magnitude;
            if (magnitude > 0) {
                return new XY(x / magnitude, y / magnitude);
            } else {
                return new XY(0, 0);
            }
        }
    }
    [JsonIgnore]
    public double angleRad => Math.Atan2(y, x);
    public double angleDeg => angleRad * 180 / Math.PI;

    public XY Rotate(double angle) {
        if (angle == 0) {
            return new XY(x, y);
        }
        var sin = Math.Sin(angle);
        var cos = Math.Cos(angle);
        return new XY(x * cos - y * sin, x * sin + y * cos);
    }
    public static implicit operator Point(XY xy) => new Point(xy.xi, xy.yi);
    public static implicit operator XY(Point p) => new XY(p.X, p.Y);
}
public class XYZGridComparer : IEqualityComparer<XYZ> {
    public bool Equals(XYZ p1, XYZ p2) => (p1.xi == p2.xi && p1.yi == p2.yi && p1.zi == p2.zi);

    public int GetHashCode(XYZ p) => p.i.GetHashCode();
}
public class XYZ {
    public double x, y, z;

    public int xi { get => (int)x; set => x = value; }
    public int yi { get => (int)y; set => y = value; }
    public int zi { get => (int)z; set => z = value; }
    public XYZ() {
        x = 0;
        y = 0;
        z = 0;
    }
    public XYZ(XY xy) : this(xy.x, xy.y) { }
    public XYZ(int x, int y) : this(x, y, 0) { }
    public XYZ(double x, double y) : this(x, y, 0) { }
    public XYZ(int x, int y, int z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public XYZ(double x, double y, double z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public XYZ copy => new XYZ(x, y, z);
    public double xyAngle => xy.angleRad;
    public double zAngle => Math.Atan2(z, xy.magnitude);
    public XY xy => new XY(x, y);
    public XYZ i => new XYZ(xi, yi, zi);
    public static XYZ operator +(XYZ p1, XYZ p2) => new XYZ(p1.x + p2.x, p1.y + p2.y, p1.z + p2.z);
    public static XYZ operator +(XYZ p1, XY p2) => new XYZ(p1.x + p2.x, p1.y + p2.y, p1.z);
    public static XYZ operator -(XYZ p1, XYZ p2) => p1 + (-p2);
    public static XYZ operator -(XYZ p1) => new XYZ(-p1.x, -p1.y, -p1.z);
    public static double operator *(XYZ p1, XYZ p2) => (p1.x * p2.x) + (p1.y * p2.y) + (p1.z * p2.z);

    public static implicit operator (int, int, int)(XYZ p) => (p.xi, p.yi, p.zi);
    public static implicit operator XYZ((int, int, int) p) => new XYZ(p.Item1, p.Item2, p.Item3);

    public static implicit operator double(XYZ p) => p.Magnitude;

    public static explicit operator Point(XYZ p) => new Point(p.xi, p.yi);
    public XYZ PlusX(double x) => new XYZ(this.x + x, y, z);
    public XYZ PlusY(double y) => new XYZ(x, this.y + y, z);
    public XYZ PlusZ(double z) => new XYZ(x, y, this.z + z);
    public static XYZ operator *(XYZ p, double s) => new XYZ(p.x * s, p.y * s, p.z * s);
    public static XYZ operator /(XYZ p, double s) => new XYZ(p.x / s, p.y / s, p.z / s);
    public double Magnitude => Math.Sqrt(x * x + y * y + z * z);
    public double Magnitude2 => x * x + y * y + z * z;
    public XYZ Normal {
        get {
            if (x == 0 && y == 0 && z == 0) {
                return new XYZ();
            }
            double magnitude = Magnitude;
            return new XYZ(x / magnitude, y / magnitude, z / magnitude);
        }
    }

    internal XYZ RotateZ(double angle) {
        var sin = Math.Sin(angle);
        var cos = Math.Cos(angle);
        return new XYZ(x * cos - y * sin, x * sin + y * cos, z);
    }

    internal XYZ Extend(double length) => Normal * (Magnitude + length);
    public double Dot(XYZ other) => x * other.x + y * other.y + z * other.z;
    public bool Equals(XYZ other) => x == other.x && y == other.y && z == other.z;
}
//	2D array wrapper; allows one item per point
public class ArrayGrid<T> {
    public int Width { get; private set; }
    public int Height { get; private set; }
    public T[,] grid { get; private set; }

    public T this[Point p] {
        get => grid[p.X, p.Y];
        private set => grid[p.X, p.Y] = value;
    }

    public ArrayGrid(int Width, int Height) {
        this.Width = Width;
        this.Height = Height;
        grid = new T[Width, Height];
    }
    public void Clear() => Array.Clear(grid, 0, grid.Length);
    public T Try(Point p) => InBounds(p) ? this[p] : default(T);
    public bool InBounds(Point p) => (p.X > -1 && p.X < Width && p.Y > -1 && p.Y < Height);
}
//	2D array helper; allows multiple items per point and tracks all items globally
public class Grid<T> {
    public int Width { get; private set; }
    public int Height { get; private set; }
    public HashSet<T> all { get; private set; }
    public HashSet<T>[,] grid { get; private set; }
    private Func<T, Point> locator;

    public HashSet<T> this[Point p] {
        get => grid[p.X, p.Y];
        private set => grid[p.X, p.Y] = value;
    }
    public Grid(int width, int height, Func<T, Point> locator) {
        this.Width = width;
        this.Height = height;
        grid = new HashSet<T>[width, height];
        this.locator = locator;
    }
    public void Clear() {
        all.Clear();
        Array.Clear(grid, 0, grid.Length);
    }
    public void UpdateGrid() {
        Array.Clear(grid, 0, grid.Length);
        all.ToList().ForEach(t => Place(locator.Invoke(t), t));
    }
    public void Place(T t) {
        all.Add(t);
        Place(locator.Invoke(t), t);
    }
    public bool Contains(T t) => all.Contains(t);
    public void Place(Point p, T t) {
        if (Initialize(p)) {
            this[p].Add(t);
        }
    }
    public HashSet<T> Try(Point p) => Initialize(p) ? this[p] : null;
    private bool Initialize(Point p) {
        if (InBounds(p)) {
            if (this[p] == null) {
                this[p] = new HashSet<T>();
            }
            return true;
        } else {
            return false;
        }
    }
    public bool InBounds(Point p) => (p.X > -1 && p.X < Width && p.Y > -1 && p.Y < Height);
}
//	3D array wrapper; allows one item per point
public class ArraySpace<T> {
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Depth { get; private set; }
    public T[,,] space { get; private set; }
    public T this[XYZ p] {
        get => space[p.xi, p.yi, p.zi];
        set => space[p.xi, p.yi, p.zi] = value;
    }
    public ArraySpace(int Width, int Height, int Depth) {
        this.Width = Width;
        this.Height = Height;
        this.Depth = Depth;
        space = new T[Width, Height, Depth];
    }
    public ArraySpace(int Width, int Height, int Depth, T fill) : this(Width, Height, Depth) {
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                for (int z = 0; z < Depth; z++) {
                    this[new XYZ(x, y, z)] = fill;
                }
            }
        }
    }

    public void Clear() => Array.Clear(space, 0, space.Length);
    public Grid<T> GetGrid(int z) {
        Grid<T> grid = new Grid<T>(Width, Height, null);
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {

            }
        }
        return grid;
    }
    public void Fill(Func<T> t) {
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                for (int z = 0; z < Depth; z++) {
                    this[new XYZ(x, y, z)] = t.Invoke();
                }
            }
        }
    }
    public T Try(XYZ p) => InBounds(p) ? this[p] : default(T);
    public bool InBounds(XYZ p) => p.xi > -1 && p.xi < Width && p.yi > -1 && p.yi < Height && p.zi > -1 && p.zi < Depth;
}
//	3D array helper; allows multiple items per point and tracks all items globally
public class Space<T> {
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Depth { get; private set; }
    public HashSet<T> all { get; private set; }
    public HashSet<T>[,,] space { get; private set; }
    private Func<T, XYZ> locator;
    public HashSet<T> this[XYZ p] {
        get => space[p.xi, p.yi, p.zi];
        set => space[p.xi, p.yi, p.zi] = value;
    }
    public Space(int Width, int Height, int Depth, Func<T, XYZ> locator) {
        this.Width = Width;
        this.Height = Height;
        this.Depth = Depth;
        all = new HashSet<T>();
        space = new HashSet<T>[Width, Height, Depth];
        this.locator = locator;
    }

    public void Clear() {
        all.Clear();
        Array.Clear(space, 0, space.Length);
    }
    public Grid<T> GetGrid(int z) {
        Grid<T> grid = new Grid<T>(Width, Height, null);
        all.ToList().ForEach(t => {
            XYZ p = locator.Invoke(t);
            if (p.zi == z) {
                grid.Place((Point)p, t);
            }
        });
        return grid;
    }
    public void UpdateSpace() {
        Array.Clear(space, 0, space.Length);
        all.ToList().ForEach(t => Place(locator.Invoke(t), t));
    }
    public void Place(T t) {
        if (all.Add(t))
            Place(locator.Invoke(t), t);
    }
    public void Remove(T t) {
        all.Remove(t);
        UpdateSpace();
    }
    public bool Contains(T t) => all.Contains(t);
    private void Place(XYZ p, T t) {
        if (Initialize(p)) {
            this[p].Add(t);
        }
    }
    public HashSet<T> Try(XYZ p) => Initialize(p) ? this[p] : null;
    public bool Try(XYZ p, out HashSet<T> result) {
        if (Initialize(p)) {
            result = this[p];
            return true;
        } else {
            result = null;
            return false;
        }
    }
    private bool Initialize(XYZ p) {
        if (InBounds(p)) {
            if (this[p] == null) {
                this[p] = new HashSet<T>();
            }
            return true;
        } else {
            return false;
        }
    }
    public bool InBounds(XYZ p) => p.xi > -1 && p.xi < Width && p.yi > -1 && p.yi < Height && p.zi > -1 && p.zi < Depth;
}
public interface ILocator<T, U> {
    U Locate(T t);
}
public class LocatorDict<TValue, TKey> {
    public HashSet<TValue> all=new();

    public Dictionary<TKey, HashSet<TValue>> space { get; private set; } = new();
    public ILocator<TValue, TKey> locator;
    public HashSet<TValue> this[TKey u] => space.TryGetValue(u, out var value) ? value : new HashSet<TValue>();
    public LocatorDict(ILocator<TValue, TKey> locator) {
        this.locator = locator;
    }
    public LocatorDict(ILocator<TValue, TKey> locator, IEnumerable<TValue> items) {
        this.locator = locator;
        foreach (var i in items) Add(i);
    }
    public void Clear() => all.Clear();
    public void UpdateSpace() {
        space.Clear();
        foreach (var t in all) {
            Initialize(locator.Locate(t)).Add(t);
        }
    }
    private HashSet<TValue> Initialize(TKey u) 
        => space.TryGetValue(u, out var result) ? result : space[u] = new();
    
    public void Add(TValue t) {
        if (all.Add(t))
            Place(locator.Locate(t), t);
    }
    private void Place(TKey u, TValue t) =>
        Initialize(u).Add(t);
    public void Remove(TValue t) {
        all.Remove(t);
        UpdateSpace();
    }
    public bool Contains(TValue t) => all.Contains(t);
    public IEnumerable<TValue> FilterKey(Predicate<TKey> keySelector) =>
        space.Where(pair => keySelector(pair.Key)).SelectMany(pair => pair.Value);
    public IEnumerable<V> FilterKeySelect<V>(Predicate<TKey> keySelector, Func<TValue, V> valueSelector, Func<V, bool> valueFilter) =>
    space.Where(pair => keySelector(pair.Key)).SelectMany(pair => pair.Value.Select(valueSelector).Where(valueFilter));
    public Dictionary<TKey, HashSet<TValue>> Transform(Func<TKey, TKey> scale) =>
        LocateAll(all, t => scale(locator.Locate(t)));
    public Dictionary<TKey, HashSet<TValue>> Relocate(Func<TValue, TKey> locate) =>
        LocateAll(all, locate);
    public static Dictionary<TKey, HashSet<TValue>> LocateAll(IEnumerable<TValue> all, Func<TValue, TKey> locate) {
        var space = new Dictionary<TKey, HashSet<TValue>>();
        HashSet<TValue> Initialize(TKey u) =>
            space.TryGetValue(u, out var result) ? result : space[u] = new();
        foreach (var t in all) {
            Initialize(locate(t)).Add(t);
        }
        return space;
    }
    public Dictionary<TKey, HashSet<TValue>> Transform(Func<TValue, TKey> locate, Func<TKey, bool> posFilter) =>
        Transform(all, locate, posFilter);
    public static Dictionary<TKey, HashSet<TValue>> Transform(IEnumerable<TValue> all, Func<TValue, TKey> locate, Func<TKey, bool> posFilter) {
        var space = new Dictionary<TKey, HashSet<TValue>>();
        HashSet<TValue> Initialize(TKey u) =>
            space.TryGetValue(u, out var result) ? result : space[u] = new();
        foreach (var t in all) {
            var u = locate(t);
            if(posFilter(u))
                Initialize(u).Add(t);
        }
        return space;
    }
    public Dictionary<TKey, HashSet<TResult>> TransformSelect<TResult>(Func<TValue, TKey> locate, Func<TKey, bool> posFilter, Func<TValue, TResult> select) =>
        TransformSelect(all, locate, posFilter, select);
    public static Dictionary<TKey, HashSet<TItem>> TransformSelect<TItem>(IEnumerable<TValue> all, Func<TValue, TKey> locate, Func<TKey, bool> posFilter, Func<TValue, TItem?> select) {
        var space = new Dictionary<TKey, HashSet<TItem>>();
        HashSet<TItem> Initialize(TKey u) =>
            space.TryGetValue(u, out var result) ? result : space[u] = new();
        foreach (var t in all) {
            var u = locate(t);
            if (posFilter(u)) {
                var v = select(t);
                if (v != null) {
                    Initialize(u).Add(v);
                }
            }
        }
        return space;
    }
#nullable enable
    public Dictionary<TKey, List<TResult>> TransformSelectList<TResult>(Func<TValue, TKey> locate, Func<TKey, bool> posFilter, Func<TValue, TResult?> select) =>
        TransformSelectList(all, locate, posFilter, select);
    public static Dictionary<TKey, List<TResult>> TransformSelectList<TResult>(IEnumerable<TValue> all, Func<TValue, TKey> locate, Func<TKey, bool> posFilter, Func<TValue, TResult?> select) {
        var space = new Dictionary<TKey, List<TResult>>();
        List<TResult> Initialize(TKey u) =>
            space.TryGetValue(u, out var result) ? result : space[u] = new();
        foreach (var t in all) {
            var u = locate(t);
            if (posFilter(u)) {
                TResult? v = select(t);
                if (v != null) {
                    Initialize(u).Add(v);
                }
            }
        }
        return space;
    }
#nullable restore
}

public class SetDict<U, T> {
    HashSet<T> all = new();
    public Dictionary<U, HashSet<T>> space { get; private set; } = new();
    public HashSet<T> this[U u] => space.TryGetValue(u, out var value) ? value : new HashSet<T>();
    public SetDict() {
    }
    public void Clear() {
        all.Clear();
        space.Clear();
    }
    public bool Contains(T t) => all.Contains(t);
    public HashSet<T> GetAll(Predicate<U> keySelector) {
        var result = new HashSet<T>();
        foreach (var pair in space) {
            if (keySelector(pair.Key)) {
                result.UnionWith(pair.Value);
            }
        }
        return result;
    }
    public void Add(U u, T t) {
        Initialize(u).Add(t);
        all.Add(t);
    }
    public void AddRange(U u, IEnumerable<T> t) {
        Initialize(u).UnionWith(t);
        all.UnionWith(t);
    }
    private HashSet<T> Initialize(U u) => space.TryGetValue(u, out var t) ? t : space[u] = new();
}
