using Common;
using SadConsole;
using SadRogue.Primitives;
using System.Collections.Generic;

namespace RogueFrontier;
//Blocks projectiles from anyone but the player
class ShieldBarrier : ProjectileBarrier {
    public bool active => lifetime > 0;
    public ColoredGlyph tile => new ColoredGlyph(Color.DarkCyan, Color.Black, '*');
    public int id { get; private set; }
    public PlayerShip owner;
    public XY offset;
    public int lifetime;
    public HashSet<Projectile> blocked;
    public XY position { get; set; }
    public ShieldBarrier(PlayerShip owner, XY offset, int lifetime, HashSet<Projectile> blocked) {
        this.id = owner.world.nextId++;
        this.owner = owner;
        this.offset = offset;
        this.lifetime = lifetime;
        this.blocked = blocked;
        UpdatePosition();
    }
    public void Update() {
        if (owner.active) {
            lifetime--;
            UpdatePosition();
        } else {
            lifetime = 0;
        }
    }
    public void UpdatePosition() => position = owner.position + offset;
    public void Interact(Projectile other) {
        if (other.source == owner) {
            return;
        }
        if (blocked.Contains(other)) {
            return;
        }
        blocked.Add(other);
        other.lifetime = 0;
        other.damageHP = 0;

    }
}


class BubbleBarrier : ProjectileBarrier {
    public bool active => lifetime > 0;
    public ColoredGlyph tile => new ColoredGlyph(Color.DarkCyan, Color.Black, '*');
    public int id { get; private set; }
    public PlayerShip owner;
    public XY offset;
    public int lifetime;
    public HashSet<Projectile> blocked;
    public XY position { get; set; }
    public BubbleBarrier(PlayerShip owner, XY offset, int lifetime, HashSet<Projectile> blocked) {
        this.id = owner.world.nextId++;
        this.owner = owner;
        this.offset = offset;
        this.lifetime = lifetime;
        this.blocked = blocked;
        UpdatePosition();
    }
    public void Update() {
        if (owner.active) {
            lifetime--;
            UpdatePosition();
        } else {
            lifetime = 0;
        }
    }
    public void UpdatePosition() => position = owner.position + offset;
    public void Interact(Projectile other) {
        if (blocked.Contains(other)) {
            return;
        }
        blocked.Add(other);
        other.lifetime = 0;
        other.damageHP = 0;

    }
}