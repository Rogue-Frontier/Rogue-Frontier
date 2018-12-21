using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	static class VoxelHelp {
		public static Voxel FromString(string name) {
			switch(name) {
				case "Air": return new Air();
				case "Grass": return new Grass();
				default: throw new Exception("Unknown voxel type");
			}
		}
	}
	public enum VoxelType {
		Empty,
		Floor,
		Solid
	};
	public interface Voxel {
		VoxelType Collision { get; }
		ColoredString GetCharAbove();
		ColoredString GetCharCenter();
	}
	public class Air : Voxel {
		public VoxelType Collision => VoxelType.Empty;
		ColoredString s = new ColoredString("" + (char) 176, new Cell(Color.White, Color.Transparent));
		public ColoredString GetCharAbove() => s;
		public ColoredString GetCharCenter() => s;
	}
	public class Grass : Voxel {
		public VoxelType Collision => VoxelType.Solid;
		public Color color { get; private set; }
		private string s;
		private string[] symbols = {
			"\"", "'", "w", "v", ",", ".", "`",
		};
		public Grass() {
			color = new Color(Global.Random.Next(102), 153, Global.Random.Next(102));
			s = symbols[Global.Random.Next(symbols.Length)];
		}
		public ColoredString GetCharAbove() => new ColoredString(s, new Cell(color, Color.Transparent));
		public ColoredString GetCharCenter() => new ColoredString(" ", new Cell(Color.Transparent, color));
	}
	public class Floor : Voxel {
		public VoxelType Collision => VoxelType.Floor;
		public Color color { get; private set; }
		public Floor(Color c) {
			this.color = c;
		}
		public ColoredString GetCharAbove() => new ColoredString(".", new Cell(color, Color.Transparent));
		public ColoredString GetCharCenter() => new ColoredString("+", new Cell(Color.Transparent, color));
	}
}
