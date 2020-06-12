using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class World {
        public TypeCollection types = new TypeCollection();
        public LocatorDict<Entity, (int, int)> entities = new LocatorDict<Entity, (int, int)>(e => (e.Position.xi, e.Position.yi));
        public List<Entity> entitiesAdded = new List<Entity>();
        public LocatorDict<Effect, (int, int)> effects = new LocatorDict<Effect, (int, int)>(e => (e.Position.xi, e.Position.yi));
        public List<Effect> effectsAdded = new List<Effect>();
        public Random karma = new Random();
        public Backdrop backdrop = new Backdrop();
        public void AddEffect(Effect e) {
            effectsAdded.Add(e);
        }
        public void AddEntity(Entity e) {
            entitiesAdded.Add(e);
        }
        public void RemoveAll() {
            entities.Clear();
            effects.Clear();
            entitiesAdded.Clear();
            effectsAdded.Clear();
        }
        public void UpdatePresent() {
            entities.all.UnionWith(entitiesAdded);
            effects.all.UnionWith(effectsAdded);
            entitiesAdded.Clear();
            effectsAdded.Clear();
            entities.all.RemoveWhere(e => !e.Active);
            effects.all.RemoveWhere(e => !e.Active);
        }
    }
}
