using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
    public enum VoxelDefaults {
        Air, Grass
    }
	static class VoxelHelp {
		public static Voxel FromString(string name, Island World) {
			switch(name) {
				case "Air": return new Air();
				case "Grass": return new Grass(World);
				default: throw new Exception("Unknown voxel type");
			}
		}
        public static Voxel Create(VoxelDefaults v, Island World) {
            switch (v) {
                case VoxelDefaults.Air: return new Air();
                case VoxelDefaults.Grass: return new Grass(World);
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
		ColoredGlyph CharAbove { get; }
		ColoredGlyph CharCenter { get; }
	}
	public class Air : Voxel {
		public VoxelType Collision => VoxelType.Empty;

        public ColoredGlyph CharAbove => new ColoredGlyph(Color.White, Color.Transparent, 176);
        public ColoredGlyph CharCenter => CharAbove;
	}
	public class Grass : Voxel {
		public VoxelType Collision => VoxelType.Solid;
		public Color foreground { get; private set; }
        public Color background { get; private set; }
        private char s;
		private char[] symbols = {
			'"', '\'', 'w', 'v', ',', '.', '`',
		};
		public Grass(Island World) {
			foreground = new Color(World.karma.Next(102), 153, World.karma.Next(102));
            int r = World.karma.Next(26);
            background = new Color(r, World.karma.Next(26) + 13, 26 - r);
			s = symbols[World.karma.Next(symbols.Length)];
		}
		public ColoredGlyph CharAbove => new ColoredGlyph(foreground, background, s);
		public ColoredGlyph CharCenter => new ColoredGlyph(Color.Transparent, foreground, ' ');
	}
	public class Floor : Voxel {
		public VoxelType Collision => VoxelType.Floor;
		public Color color { get; private set; }
		public Floor(Color c) {
			this.color = c;
		}
		public ColoredGlyph CharAbove => new ColoredGlyph(color, Color.Transparent, '.');
		public ColoredGlyph CharCenter => new ColoredGlyph(Color.Transparent, color, '+');
	}
}
