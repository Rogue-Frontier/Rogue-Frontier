using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class World {
        public SetDict<Entity, (int, int)> entities = new SetDict<Entity, (int, int)>(e => (e.position.xi, e.position.yi));
        public List<Entity> entitiesAdded = new List<Entity>();
        public SetDict<Effect, (int, int)> effects = new SetDict<Effect, (int, int)>(e => (e.position.xi, e.position.yi));
        public List<Entity> effectsAdded = new List<Entity>();
        public Random karma = new Random();
        public void AddEffect(Effect e) {
            effects.Place(e);
        }
        public void AddEntity(Entity e) {
            entities.Place(e);
        }
    }
}
