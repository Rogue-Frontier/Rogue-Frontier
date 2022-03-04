﻿using Common;
using RogueFrontier;
using SadConsole;
using SadRogue.Primitives;
using System.Linq;
using System.Xml.Linq;
using Sys = RogueFrontier.System;
public record FlashDesc(){
    [Req] public int intensity;
    public FlashDesc(XElement e) : this() {
        e.Initialize(this);
    }
    public void Create(Sys world, XY position) {
        var center = new Center(position, intensity, 60);
        world.AddEffect(center);
        int radius = 25;
        var particles = Enumerable.Range(-radius*2, radius * 2*2)
            .SelectMany(x => Enumerable.Range(-radius*2, radius * 2*2).Select(y => new XY(x, y)))
            .Where(p => (p - position).magnitude > radius)
            .Select(p => new Particle(center, center.position + p));
        particles.ToList().ForEach(world.AddEffect);
    }
    public class Center : Effect {
        public XY position { get; set; }
        public int maxBrightness;
        public int maxLifetime;
        public int lifetime;
        public int brightness => maxBrightness * lifetime / maxLifetime;
        public bool active => brightness>128;
        public ColoredGlyph tile => new(new(255, 255, 255, brightness), Color.Transparent, '*');
        public Center(XY position, int brightness, int lifetime) {
            this.position = position;
            this.maxBrightness = brightness;
            this.maxLifetime = lifetime;
            this.lifetime = lifetime;
        }
        public void Update() {
            lifetime--;
        }
    }
    public class Particle : Effect {
        public Center parent;
        public XY position { get; set; }
        public double distance2;
        public int brightness => (int)(parent.brightness / distance2);
        public bool active => brightness> 128;
        public ColoredGlyph tile => new(new(255, 255, 255, brightness), Color.Transparent, '*');
        public Particle(Center parent, XY position) {
            this.parent = parent;
            this.position = position;
            distance2 = (parent.position - position).magnitude;
        }
        public void Update() {}
    }
}