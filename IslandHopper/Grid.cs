using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	class Point2 {
		public double x;
		public double y;
		public int xi { get => (int)x; set => x = value; }
		public int yi { get => (int)y; set => y = value; }
		public Point2() {
			x = 0;
			y = 0;
		}
		public Point2(double x, double y) {
			this.x = x;
			this.y = y;
		}
		public static Point2 operator +(Point2 p, Point2 other) {
			return new Point2(p.x + other.x, p.y + other.y);
		}
		public Point2 clone() {
			return new Point2(x, y);
		}
		public Point2 PlusX(double x) {
			return new Point2(this.x + x, y);
		}
		public Point2 PlusY(double y) {
			return new Point2(x, this.y + y);
		}
	}
	public class Point3 {
		public double x, y, z;

		public int xi { get => (int)x; set => x = value; }
		public int yi { get => (int)y; set => y = value; }
		public int zi { get => (int)z; set => z = value; }
		public Point3() {
			x = 0;
			y = 0;
			z = 0;
		}
		public Point3(double x, double y, double z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public Point3(int x, int y) {
			this.x = x;
			this.y = y;
			this.z = 0;
		}

		public static Point3 operator +(Point3 p1, Point3 p2) => new Point3(p1.x + p2.x, p1.y + p2.y, p1.z + p2.z);
		public static Point3 operator -(Point3 p1, Point3 p2) => p1 + (-p2);
		public static Point3 operator -(Point3 p1) => new Point3(-p1.x, -p1.y, -p1.z);
		public static double operator *(Point3 p1, Point3 p2) => (p1.x * p2.x) + (p1.y * p2.y) + (p1.z * p2.z);

		public static explicit operator Point(Point3 p) => new Point(p.xi, p.yi);
		public Point3 PlusX(double x) => new Point3(this.x + x, y, z);
		public Point3 PlusY(double y) => new Point3(x, this.y + y, z);
		public Point3 PlusZ(double z) => new Point3(x, y, this.z + z);
		public static Point3 operator *(Point3 p, double s) => new Point3(p.x * s, p.y * s, p.z * s);
		public static Point3 operator /(Point3 p, double s) => new Point3(p.x / s, p.y / s, p.z / s);
		public double Magnitude() => Math.Sqrt(x * x + y * y + z * z);
		public Point3 Normal() {
			double magnitude = Magnitude();
			return new Point3(x / magnitude, y / magnitude, z / magnitude);
		}
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
		public T this[Point3 p] {
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
						this[new Point3(x, y, z)] = fill;
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
						this[new Point3(x, y, z)] = t.Invoke();
					}
				}
			}
		}
		public T Try(Point3 p) => InBounds(p) ? this[p] : default(T);
		public bool InBounds(Point3 p) => p.xi > -1 && p.xi < Width && p.yi > -1 && p.yi < Height && p.zi > -1 && p.zi < Depth;
	}
	//	3D array helper; allows multiple items per point and tracks all items globally
	public class Space<T> {
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int Depth { get; private set; }
		public HashSet<T> all { get; private set; }
		public HashSet<T>[,,] space { get; private set; }
		private Func<T, Point3> locator;
		public HashSet<T> this[Point3 p] {
			get => space[p.xi, p.yi, p.zi];
			set => space[p.xi, p.yi, p.zi] = value;
		}
		public Space(int Width, int Height, int Depth, Func<T, Point3> locator) {
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
				Point3 p = locator.Invoke(t);
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
		private void Place(Point3 p, T t) {
			if (Initialize(p)) {
				this[p].Add(t);
			}
		}
		public HashSet<T> Try(Point3 p) => Initialize(p) ? this[p] : null;
		private bool Initialize(Point3 p) {
			if (InBounds(p)) {
				if (this[p] == null) {
					this[p] = new HashSet<T>();
				}
				return true;
			} else {
				return false;
			}
		}
		public bool InBounds(Point3 p) => p.xi > -1 && p.xi < Width && p.yi > -1 && p.yi < Height && p.zi > -1 && p.zi < Depth;
	}
}
