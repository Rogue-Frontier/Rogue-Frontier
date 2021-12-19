using Common;
using Newtonsoft.Json;
using SadConsole;
using System.Collections.Generic;

namespace RogueFrontier;

class EffectLocator : ILocator<Effect, (int, int)> {
    public (int, int) Locate(Effect e) => (e.position.xi, e.position.yi);
}
class EntityLocator : ILocator<Entity, (int, int)> {
    public (int, int) Locate(Entity e) => (e.position.xi, e.position.yi);
}

public class System {
    [JsonIgnore]
    public static readonly System empty = new System(new Universe());

    public string id, name;

    public HashSet<Event> events = new HashSet<Event>();
    public List<Event> eventsAdded = new List<Event>();
    public List<Event> eventsRemoved = new List<Event>();

    public LocatorDict<Entity, (int, int)> entities = new LocatorDict<Entity, (int, int)>(new EntityLocator());
    public List<Entity> entitiesAdded = new List<Entity>();
    public List<Entity> entitiesRemoved = new List<Entity>();
    public LocatorDict<Effect, (int, int)> effects = new LocatorDict<Effect, (int, int)>(new EffectLocator());
    public List<Effect> effectsAdded = new List<Effect>();
    public List<Effect> effectsRemoved = new List<Effect>();

    public List<Star> stars = new List<Star>();

    public Universe universe;
    [JsonIgnore]
    public TypeCollection types => universe.types;
    [JsonIgnore]
    public Rand karma => universe.karma;
    public Backdrop backdrop;

    public int tick;
    public int nextId;

    public System() {
        this.universe = new Universe();
        backdrop = new Backdrop();
    }
    public System(Universe universe) {
        this.universe = universe;
        backdrop = new Backdrop();
    }
    public void AddEvent(Event e) => eventsAdded.Add(e);
    public void AddEffect(Effect e) => effectsAdded.Add(e);
    public void AddEntity(Entity e) => entitiesAdded.Add(e);
    public void RemoveEvent(Event e) => eventsRemoved.Add(e);
    public void RemoveEffect(Effect e) => effectsRemoved.Add(e);
    public void RemoveEntity(Entity e) => entitiesRemoved.Add(e);
    public void RemoveAll() {
        events.Clear();
        entities.Clear();
        effects.Clear();
        eventsAdded.Clear();
        entitiesAdded.Clear();
        effectsAdded.Clear();
        eventsRemoved.Clear();
        entitiesRemoved.Clear();
        effectsRemoved.Clear();
    }
    public void UpdateAdded() {
        events.UnionWith(eventsAdded);
        entities.all.UnionWith(entitiesAdded);
        effects.all.UnionWith(effectsAdded);
        eventsAdded.Clear();
        entitiesAdded.Clear();
        effectsAdded.Clear();
    }
    public void UpdateRemoved() {
        events.ExceptWith(eventsRemoved);
        entities.all.ExceptWith(entitiesRemoved);
        effects.all.ExceptWith(effectsRemoved);
        eventsRemoved.Clear();
        entitiesRemoved.Clear();
        effectsRemoved.Clear();

        entities.all.RemoveWhere(e => !e.active);
        effects.all.RemoveWhere(e => !e.active);
        events.RemoveWhere(e => !e.active);
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
        foreach (var e in events) {
            e.Update();
        }
        tick++;
    }

    public void PlaceTiles(Dictionary<(int, int), ColoredGlyph> tiles) {
        foreach (var e in entities.all) {
            var p = e.position.roundDown;
            if (e.tile != null && (!tiles.ContainsKey(p) || e is Segment)) {
                tiles[p] = e.tile;
            }
        }
        foreach (var e in effects.all) {
            var p = e.position.roundDown;
            if (e.tile != null && !tiles.ContainsKey(p)) {
                tiles[p] = e.tile;
            }
        }
    }
    public Stargate FindGateTo(System to) => universe.FindGateTo(this, to);
}
