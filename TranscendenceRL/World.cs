using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class World {
        public TypeCollection types;
        public LocatorDict<Entity, (int, int)> entities = new LocatorDict<Entity, (int, int)>(e => (e.Position.xi, e.Position.yi));
        public List<Entity> entitiesAdded = new List<Entity>();
        public LocatorDict<Effect, (int, int)> effects = new LocatorDict<Effect, (int, int)>(e => (e.Position.xi, e.Position.yi));
        public List<Effect> effectsAdded = new List<Effect>();
        public Random karma;
        public Backdrop backdrop;
        public World(TypeCollection types, Random karma, Backdrop backdrop) {
            this.types = types;
            this.karma = karma;
            this.backdrop = backdrop;
        }
        public World() {
            types = new TypeCollection();
            karma = new Random();
            backdrop = new Backdrop();
        }
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
        public void UpdateAll() {
            //Place everything in the grid
            entities.UpdateSpace();
            effects.UpdateSpace();

            //Update everything
            foreach (var e in entities.all) {
                e.Update();
            }
            foreach (var e in effects.all) {
                e.Update();
            }
        }
    }
}
