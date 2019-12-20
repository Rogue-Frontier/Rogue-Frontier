using IslandHopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	public class Island {
		public Random karma;
		public Space<Entity> entities { get; set; }     //	3D entity grid used for collision detection
        public Space<Effect> effects { get; set; }
		public ArraySpace<Voxel> voxels { get; set; }   //	3D voxel grid used for collision detection
		public XYZ camera { get; set; }              //	Point3 representing the location of the center of the screen
		public Player player { get; set; }              //	Player object that controls the game

        public void AddEffect(Effect e) => effects.all.Add(e);
		public void AddEntity(Entity e) => entities.all.Add(e);
		public void RemoveEntity(Entity e) => entities.all.Remove(e);
	}
}
