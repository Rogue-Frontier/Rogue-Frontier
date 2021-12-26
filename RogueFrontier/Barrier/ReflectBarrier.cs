using Common;
using SadConsole;
using SadRogue.Primitives;
using System.Collections.Generic;

namespace RogueFrontier;

//Surrounds the playership, reflects their projectiles back so that they bounce around
class ReflectBarrier : ProjectileBarrier {
    public bool active => lifetime > 0;
    public ColoredGlyph tile => new ColoredGlyph(Color.Goldenrod, Color.Black, '*');


    public int id { get; private set; }
    public PlayerShip owner;
    public XY offset;
    public int lifetime;
    public HashSet<Projectile> reflected;
    public XY position { get; set; }
    public ReflectBarrier(PlayerShip owner, XY offset, int lifetime, HashSet<Projectile> reflected) {
        this.id = owner.world.nextId++;
        this.owner = owner;
        this.offset = offset;
        this.lifetime = lifetime;
        this.reflected = reflected;
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
    public void UpdatePosition() {
        this.position = owner.position + offset;
    }
    public void Interact(Projectile other) {
        if (other.source == owner) {
            return;
        }
        if (reflected.Contains(other)) {
            return;
        }
        reflected.Add(other);
        if (other.maneuver?.target != null) {
            other.maneuver.target = other.source;
        }
        other.source = null;
        other.velocity = new XY() - other.velocity;

    }
}
