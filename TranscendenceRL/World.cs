using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class World {
        public static World empty = new World();

        public TypeCollection types;
        public LocatorDict<Entity, (int, int)> entities = new LocatorDict<Entity, (int, int)>(e => (e.Position.xi, e.Position.yi));
        public List<Entity> entitiesAdded = new List<Entity>();
        public List<Entity> entitiesRemoved = new List<Entity>();
        public LocatorDict<Effect, (int, int)> effects = new LocatorDict<Effect, (int, int)>(e => (e.Position.xi, e.Position.yi));
        public List<Effect> effectsAdded = new List<Effect>();
        public List<Effect> effectsRemoved = new List<Effect>();
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
        public void RemoveEffect(Effect e) {
            effectsRemoved.Add(e);
        }
        public void RemoveEntity(Entity e) {
            entitiesRemoved.Add(e);
        }
        public void RemoveAll() {
            entities.Clear();
            effects.Clear();
            entitiesAdded.Clear();
            effectsAdded.Clear();
            entitiesRemoved.Clear();
            effectsRemoved.Clear();
        }
        public void UpdateAdded() {
            entities.all.UnionWith(entitiesAdded);
            effects.all.UnionWith(effectsAdded);
            entitiesAdded.Clear();
            effectsAdded.Clear();
        }
        public void UpdateRemoved() {
            entities.all.ExceptWith(entitiesRemoved);
            effects.all.ExceptWith(effectsRemoved);
            entitiesRemoved.Clear();
            effectsRemoved.Clear();

            entities.all.RemoveWhere(e => !e.Active);
            effects.all.RemoveWhere(e => !e.Active);
        }
        public void UpdatePresent() {
            UpdateAdded();
            UpdateRemoved();
        }
        public void UpdateSpace() {
            //Place everything in the grid
            entities.UpdateSpace();
            effects.UpdateSpace();
        }
        public void UpdateActive() {
            UpdateSpace();

            //Update everything
            foreach (var e in entities.all) {
                e.Update();
            }
            foreach (var e in effects.all) {
                e.Update();
            }
        }
        public void UpdateActive(Dictionary<(int, int), ColoredGlyph> tiles) {
            UpdateSpace();
            foreach (var e in entities.all) {
                e.Update();

                var p = e.Position.RoundDown;
                if (e.Tile != null && !tiles.ContainsKey(p)) {
                    tiles[p] = e.Tile;
                }
            }
            foreach (var e in effects.all) {
                e.Update();
                var p = e.Position.RoundDown;
                if (e.Tile != null && !tiles.ContainsKey(p)) {
                    tiles[p] = e.Tile;
                }
            }
        }
    }
}
