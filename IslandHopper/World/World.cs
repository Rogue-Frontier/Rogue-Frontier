using IslandHopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	public class Island {
		public Random karma;
		public SetDict<Entity, (int, int, int)> entities { get; set; }     //	3D entity grid used for collision detection
        public SetDict<Effect, (int, int, int)> effects { get; set; }
		public ArraySpace<Voxel> voxels { get; set; }   //	3D voxel grid used for collision detection
		public XYZ camera { get; set; }              //	Point3 representing the location of the center of the screen
		public Player player { get; set; }              //	Player object that controls the game
        public TypeCollection types;

        public void AddEffect(Effect e) => effects.Place(e);
		public void AddEntity(Entity e) => entities.Place(e);
		public void RemoveEntity(Entity e) => entities.Remove(e);
	}
}
