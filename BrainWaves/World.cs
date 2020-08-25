using Common;
using SadConsole;
using System;
using System.Collections.Generic;

namespace BrainWaves {
    public class World {

        public LocatorDict<Entity, (int, int)> entities = new LocatorDict<Entity, (int, int)>(e => (e.Position.xi, e.Position.yi));
        public List<Entity> entitiesAdded = new List<Entity>();
        public List<Entity> entitiesRemoved = new List<Entity>();
        public Random karma = new Random();

        public QTree<Voxel> voxels = new QTree<Voxel>();
        public QTree<byte> brightness = new QTree<byte>();
        
        public void AddEntity(Entity e) => entitiesAdded.Add(e);
        public void RemoveEntity(Entity e) => entitiesRemoved.Add(e);
        public void RemoveAll() {
            entities.Clear();
            entitiesAdded.Clear();
            entitiesRemoved.Clear();
        }
        public void UpdateAdded() {
            entities.all.UnionWith(entitiesAdded);
            entitiesAdded.Clear();
        }
        public void UpdateRemoved() {
            entities.all.ExceptWith(entitiesRemoved);
            entitiesRemoved.Clear();

            entities.all.RemoveWhere(e => !e.Active);
        }
        public void UpdatePresent() {
            UpdateAdded();
            UpdateRemoved();
        }
        public void UpdateSpace() {
            //Place everything in the grid
            entities.UpdateSpace();
        }
        public void UpdateActive() {
            UpdateSpace();

            //Update everything
            foreach (var e in entities.all) {
                e.UpdateStep();
            }
        }
        public void UpdateActive(Dictionary<(int, int), ColoredGlyph> tiles) {
            UpdateSpace();
            foreach (var e in entities.all) {
                e.UpdateStep();

                var p = e.Position.RoundDown;
                if (e.Tile != null && !tiles.ContainsKey(p)) {
                    tiles[p] = e.Tile;
                }
            }
        }
    }
}
