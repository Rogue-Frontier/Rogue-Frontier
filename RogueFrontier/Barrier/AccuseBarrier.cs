using Common;
using Newtonsoft.Json;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;

namespace RogueFrontier;

//Surrounds the playership, any projectile that hits this barrier accelerates to extreme speed
class AccuseBarrier : ProjectileBarrier {
    [JsonIgnore]
    public bool active => lifetime > 0;
    [JsonIgnore]
    public ColoredGlyph tile => new ColoredGlyph(Color.OrangeRed, Color.Black, '*');

    public int Id { get; private set; }
    public PlayerShip owner;
    public XY offset;
    public int lifetime;
    public HashSet<Projectile> cloned;
    public XY position { get; set; }
    public AccuseBarrier() { }
    public AccuseBarrier(PlayerShip owner, XY offset, int lifetime, HashSet<Projectile> cloned) {
        this.Id = owner.world.nextId++;
        this.owner = owner;
        this.offset = offset;
        this.lifetime = lifetime;
        this.cloned = cloned;
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
        if (other.source != owner) {
            return;
        }
        if (cloned.Contains(other)) {
            return;
        }
        cloned.Add(other);

        //other.velocity = other.velocity.WithMagnitude(400);
        var world = owner.world;

        Clone(offset.angleRad + Math.PI / 8);
        Clone(offset.angleRad - Math.PI / 8);
        Clone(offset.angleRad + Math.PI * 2 / 8);
        Clone(offset.angleRad - Math.PI * 2 / 8);
        Clone(offset.angleRad + Math.PI * 3 / 8);
        Clone(offset.angleRad - Math.PI * 3 / 8);

        /*
        for(double angle = offset.angleRad - Math.PI / 2; angle = offset.angleRad + Math.PI / 2; angle++) {

        }
        */


        void Clone(double angle) {
            var velocity = other.velocity + XY.Polar(angle, other.velocity.magnitude / 2);
            var p = new Projectile(other.source, other.world, other.desc, other.position, velocity, other.maneuver);
            cloned.Add(p);
            world.AddEntity(p);
        }
        return;
    }
}
