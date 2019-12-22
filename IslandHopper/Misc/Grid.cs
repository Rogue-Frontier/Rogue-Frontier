using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	public class XY {
		public double x;
		public double y;
		public int xi { get => (int)x; set => x = value; }
		public int yi { get => (int)y; set => y = value; }
		public XY() {
			x = 0;
			y = 0;
		}
		public XY(double x, double y) {
			this.x = x;
			this.y = y;
		}
		public static XY operator +(XY p, XY other) => new XY(p.x + other.x, p.y + other.y);
		public XY clone {
			get => new XY(x, y);
		}
		public XY PlusX(double x) => new XY(this.x + x, y);
		public XY PlusY(double y) => new XY(x, this.y + y);

		public double Magnitude => Math.Sqrt(x * x + y * y);
		public XY Normal {
			get {
				double magnitude = Magnitude;
				return new XY(x / magnitude, y / magnitude);
			}
		}
		public double Angle => Math.Atan2(y, x);
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
		public XYZ(int x, int y) {
			this.x = x;
			this.y = y;
			this.z = 0;
		}
        public XYZ(double x, double y) {
            this.x = x;
            this.y = y;
            this.z = 0;
        }
		public XYZ(double x, double y, double z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}
        public XYZ copy => new XYZ(x, y, z);
		public double xyAngle => xy.Angle;
		public double zAngle => Math.Atan2(z, xy.Magnitude);
		public XY xy => new XY(x, y);
        public XYZ i => new XYZ(xi, yi, zi);
		public static XYZ operator +(XYZ p1, XYZ p2) => new XYZ(p1.x + p2.x, p1.y + p2.y, p1.z + p2.z);
		public static XYZ operator -(XYZ p1, XYZ p2) => p1 + (-p2);
		public static XYZ operator -(XYZ p1) => new XYZ(-p1.x, -p1.y, -p1.z);
		public static double operator *(XYZ p1, XYZ p2) => (p1.x * p2.x) + (p1.y * p2.y) + (p1.z * p2.z);

		public static implicit operator double(XYZ p) => p.Magnitude;

		public static explicit operator Point(XYZ p) => new Point(p.xi, p.yi);
		public XYZ PlusX(double x) => new XYZ(this.x + x, y, z);
		public XYZ PlusY(double y) => new XYZ(x, this.y + y, z);
		public XYZ PlusZ(double z) => new XYZ(x, y, this.z + z);
		public static XYZ operator *(XYZ p, double s) => new XYZ(p.x * s, p.y * s, p.z * s);
		public static XYZ operator /(XYZ p, double s) => new XYZ(p.x / s, p.y / s, p.z / s);
		public double Magnitude => Math.Sqrt(x * x + y * y + z * z);
		public XYZ Normal {
			get {
                if(x == 0 && y == 0 && z == 0) {
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
			if(Initialize(p)) {
				this[p].Add(t);
			}
		}
		public HashSet<T> Try(Point p) => Initialize(p) ? this[p] : null;
		private bool Initialize(Point p) {
			if(InBounds(p)) {
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
			for(int x = 0; x < Width; x++) {
				for(int y = 0; y < Height; y++) {
					for(int z = 0; z < Depth; z++) {
						this[new XYZ(x, y, z)] = fill;
					}
				}
			}
		}

		public void Clear() => Array.Clear(space, 0, space.Length);
		public Grid<T> GetGrid(int z) {
			Grid<T> grid = new Grid<T>(Width, Height, null);
			for(int x = 0; x < Width; x++) {
				for(int y = 0; y < Height; y++) {

				}
			}
			return grid;
		}
		public void Fill(Func<T> t) {
			for(int x = 0; x < Width; x++) {
				for(int y = 0; y < Height; y++) {
					for(int z = 0; z < Depth; z++) {
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
				if(p.zi == z) {
					grid.Place((Point) p, t);
				}
			});
			return grid;
		}
		public void UpdateSpace() {
			Array.Clear(space, 0, space.Length);
			all.ToList().ForEach(t => Place(locator.Invoke(t), t));
		}
		public void Place(T t) {
			if(all.Add(t))
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
}
