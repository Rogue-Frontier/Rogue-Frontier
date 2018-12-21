using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	class World {
		public Random karma;
		public Space<Entity> entities { get; set; }     //	3D entity grid used for collision detection
		public ArraySpace<Voxel> voxels { get; set; }   //	3D voxel grid used for collision detection
		public Point3 camera { get; set; }              //	Point3 representing the location of the center of the screen
		public Player player { get; set; }              //	Player object that controls the game

		public void AddEntity(Entity e) => entities.all.Add(e);
		public void RemoveEntity(Entity e) => entities.all.Remove(e);
	}
}
