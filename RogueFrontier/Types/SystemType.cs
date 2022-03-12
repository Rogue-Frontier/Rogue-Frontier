﻿using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RogueFrontier;

public record SystemType : IDesignType {
    public string codename;
    public SystemGroup systemGroup;
    public SystemType() {

    }
    public SystemType(SystemGroup system) {
        this.systemGroup = system;
    }
    public void Initialize(TypeCollection collection, XElement e) {
        codename = e.ExpectAtt("codename");
        if (e.HasElement("SystemGroup", out var xmlSystem)) {
            systemGroup = new SystemGroup(xmlSystem, SGenerator.ParseFrom(collection, SSystemElement.Create));
        }
    }
    public void Generate(System world) {
        systemGroup.Generate(new LocationContext() {
            pos = new XY(0, 0),
            focus = new XY(0, 0),
            world = world,
            angle = 0,
            radius = 0
        }, world.types);
    }
}
public record LocationContext {
    public System world;
    public XY pos;
    public double angle;
    public double angleRad => angle * Math.PI / 180;
    public double radius;
    public XY focus;
    public int index;
}
public record LocationMod {
    [Opt] public IDice radius = null;
    [Opt] public IDice radiusInc = new Constant(0);

    [Opt] public IDice angle = null;
    [Opt] public IDice angleInc = new Constant(0);

    [Opt] public IDice arcInc = new Constant(0);
    public LocationMod() { }
    public LocationMod(XElement e) {
        e.Initialize(this);
    }
    public void Get(LocationContext lc, out double r, out double a) {
        r = radius?.Roll() ?? (lc.radius + radiusInc.Roll());
        a = angle?.Roll() ?? (lc.angle + angleInc.Roll() + (r == 0 ? 0 : arcInc.Roll() / r));
    }
    public LocationContext Adjust(LocationContext lc) {
        Get(lc, out var r, out var a);
        var p = lc.focus + XY.Polar(a * Math.PI / 180, r);
        return lc with { radius = r, angle = a, pos = p };
    }
}

public interface SystemElement {
    void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null);
}
public static class SSystemElement {
    public static SystemElement Create(TypeCollection tc, XElement e) {
        var f = SGenerator.ParseFrom(tc, Create);
        switch (e.Name.LocalName) {
            case "System":
            case "Group":
                return new SystemGroup(e, f);
            case "Orbital":
                return new SystemOrbital(e, f);
            case "Planet":
                return new SystemPlanet(e);
            case "Asteroids":
                return new SystemAsteroids(e);
            case "Nebula":
                return new SystemNebula(e);
            case "Sibling":
                return new SystemSibling(e, f);
            case "Star":
                return new SystemStar(e);
            case "Stargate":
                return new SystemStargate(tc, e);
            case "Station":
                return new SystemStation(tc, e);
            case "Ship":
            case "Ships":
                return new SystemShips(tc, e);
            case "At":
                return new SystemAt(e, f);
            case "Marker":
                return new SystemMarker(e);
            default:
                throw new Exception($"Unknown system element <{e.Name}>");
        }
    }
}
public record SystemGroup() : SystemElement {
    public LocationMod loc;
    public List<SystemElement> subelements;
    public SystemGroup(XElement e, Parse<SystemElement> parse):this() {
        loc = new(e);
        subelements = e.Elements().Select(e => parse(e)).ToList();
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var sub_lc = loc.Adjust(lc);
        try {
            subelements.ForEach(g => g.Generate(sub_lc, tc, result));
        } catch (Exception e) {
            subelements.Reverse<SystemElement>().ToList().ForEach(g => g.Generate(sub_lc, tc, result));
        }
        
    }
}
public record SystemAt() : SystemElement {
    public List<SystemElement> subelements;
    public int index = -1;
    public SystemAt(XElement e, Parse<SystemElement> parse) : this() {
        subelements = e.Elements().Select(e => parse(e)).ToList();
        index = e.ExpectAttInt("index");
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        if (lc.index == index) {
            subelements.ForEach(g => g.Generate(lc, tc, result));
        }
    }
}

public record SystemOrbital() : SystemElement {
    public List<SystemElement> subelements;

    [Opt] public IDice count = new Constant(1);

    public IDice angle;
    public IDice angleInc;

    public IDice increment;
    public bool equidistant;

    public int radius;
    public SystemOrbital(XElement e, Parse<SystemElement> parse) : this() {
        subelements = e.Elements().Select(e => parse(e)).ToList();

        e.Initialize(this);

        if (e.TryAtt(nameof(angle), out var a)) {
            switch (a) {
                case "random":
                    angle = new IntRange(0, 360);
                    break;
                case var i when IDice.TryParse(i, out var d):
                    angle = d;
                    break;
                case var unknown:
                    throw new Exception($"Invalid angle {unknown}");
            }
        } else if (e.TryAtt(nameof(angleInc), out var ai)) {
            angleInc = IDice.Parse(ai);
        }
        switch (e.TryAttNullable(nameof(increment))) {
            case null:
                break;
            case "random":
                increment = new IntRange(0, 360);
                break;
            case "equidistant":
                equidistant = true;
                break;
            case var i when IDice.TryParse(i, out var d):
                increment = d;
                break;
            case var unknown:
                throw new Exception($"Invalid increment {unknown}");
        }
        radius = e.TryAttInt(nameof(radius), 0);
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var count = this.count.Roll();
        var angle = this.angle?.Roll() ?? (lc.angle + (angleInc?.Roll()??0));

        int equidistantInterval = 360 / (subelements.Count * count);
        for (int i = 0; i < count; i++) {
            foreach (var sub in subelements) {
                var loc = lc with {
                    angle = angle,
                    radius = radius,
                    focus = lc.pos,
                    pos = lc.pos + XY.Polar(angle * Math.PI / 180, radius),
                    index = i
                };
                sub.Generate(loc, tc, result);

                if (increment is IDice d) {
                    angle += d.Roll();
                } else if (equidistant) {
                    angle += equidistantInterval;
                }
            }
        }
    }
}
public record SystemMarker() : SystemElement {
    public string name;
    public SystemMarker(XElement e) : this() {
        name = e.ExpectAtt("name");
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var m = new Marker(name, lc.pos);
        lc.world.AddEntity(m);
        result?.Add(m);
    }
}
public record SystemPlanet() : SystemElement {
    public int radius;
    public bool showOrbit;
    public SystemPlanet(XElement e) : this() {
        radius = e.ExpectAttInt(nameof(radius));
        showOrbit = e.TryAttBool(nameof(showOrbit), true);
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var diameter = radius * 2;
        var radius2 = radius * radius;
        var center = new XY(radius, radius);

        var r = lc.world.karma;
        ColoredGlyph[,] tiles = new ColoredGlyph[diameter, diameter];
        for (int x = 0; x < diameter; x++) {
            var xOffset = Math.Abs(x - radius);
            var yRange = Math.Sqrt(radius2 - (xOffset * xOffset));
            var yStart = radius - (int)Math.Round(yRange, MidpointRounding.AwayFromZero);
            var yEnd = radius + yRange;
            for (int y = yStart; y < yEnd; y++) {
                var pos = lc.pos + (new XY(x, y) - center);

                var f = Color.LightBlue;
                f = f.Blend(Color.DarkBlue.SetAlpha((byte)r.NextInteger(0, 153)));
                f = f.Blend(Color.Gray.SetAlpha(102));

                var tile = new ColoredGlyph(f, Color.Black, '%');
                lc.world.backdrop.planets.tiles[pos] = tile;
                tiles[x, y] = tile;
                //lc.world.AddEffect(new FixedTile(tile, pos));
            }
        }
        var circ = radius * 2 * Math.PI;
        for (int x = 0; x < diameter; x++) {
            var xOffset = Math.Abs(x - radius);
            var yRange = Math.Sqrt(radius2 - (xOffset * xOffset));
            var yStart = radius - (int)Math.Round(yRange, MidpointRounding.AwayFromZero);
            var yEnd = radius + yRange;
            for (int y = yStart; y < yEnd; y++) {
                var loc = r.NextDouble() * circ * (radius - 2);
                var from = center + XY.Polar(loc % 2 * Math.PI, loc / circ);
                var t = tiles[x, y];
                t.Foreground = t.Foreground.Blend(tiles[from.xi, from.yi].Foreground.SetAlpha((byte)r.NextInteger(0, 51)));
            }
        }
        /*
        var orbitFocus = lc.focus;
        var orbitRadius = lc.radius;
        var orbitCirc = orbitRadius * 2 * Math.PI;
        for (int i = 0; i < orbitCirc; i++) {
            var angle = i / orbitRadius;
            lc.world.backdrop.orbits.tiles[orbitFocus + XY.Polar(angle, orbitRadius)] = new ColoredGlyph(Color.White, Color.Transparent, '.');
        }
        */
    }
}
public record SystemAsteroids() : SystemElement {
    public double angle;
    public int size;
    public SystemAsteroids(XElement e) : this() {
        angle = e.ExpectAttDouble(nameof(angle)) * Math.PI / 180;
        size = e.ExpectAttInt(nameof(size));
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        double arc = lc.radius * angle;
        double halfArc = arc / 2;
        for (double i = -halfArc; i < halfArc; i++) {

            int localSize = (int)(size * Math.Abs(Math.Abs(i) - halfArc) / halfArc);

            for (int j = -localSize / 2; j < localSize / 2; j++) {
                if (lc.world.karma.NextDouble() > 0.25) {
                    continue;
                }

                var p = XY.Polar(lc.angleRad + i / lc.radius, lc.radius + j);

                var tile = new ColoredGlyph(Color.Gray, Color.Black, '%');
                lc.world.backdrop.planets.tiles[p] = tile;
            }
        }
    }
}
public record SystemNebula() : SystemElement {
    public double angle;
    public int size;
    public SystemNebula(XElement e) : this() {
        angle = e.ExpectAttDouble(nameof(angle)) * Math.PI / 180;
        size = e.ExpectAttInt(nameof(size));
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        double arc = lc.radius * angle;
        double halfArc = arc / 2;
        for (double i = -halfArc; i < halfArc; i += 0.1) {
            int localSize = (int)(lc.world.karma.NextDouble() * 2 * size * Math.Abs(Math.Abs(i) - halfArc) / halfArc);

            for (int j = -localSize / 2; j < localSize / 2; j++) {
                var p = XY.Polar(lc.angleRad + i / lc.radius, lc.radius + j);

                var tile = new ColoredGlyph(Color.Violet.SetAlpha((byte)(64 + 128 * lc.world.karma.NextDouble())), Color.Transparent, '%');
                lc.world.backdrop.nebulae.tiles[p] = tile;
            }
        }
    }
}
public record SystemSibling() : SystemElement {
    [Opt] public IDice count = new Constant(1);
    [Opt] public IDice increment = new IntRange(0, 360);
    public LocationMod mod;
    public List<SystemElement> subelements;
    public SystemSibling(XElement e, Parse<SystemElement> parse) : this() {
        e.Initialize(this);
        mod = new(e);
        subelements = e.Elements().Select(e => parse(e)).ToList();
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var count = this.count.Roll();

        mod.Get(lc, out var r, out var a);
        var sub_lc = lc with {
            angle = a,
            radius = r,
            pos = lc.focus + XY.Polar(a * Math.PI / 180, r)
        };

        Generate:
        subelements.ForEach(s => s.Generate(sub_lc, tc));
        if (count > 1) {
            count--;

            a += increment.Roll();
            sub_lc = lc with {
                angle = a,
                radius = r,
                pos = lc.focus + XY.Polar(a * Math.PI / 180, r)
            };
            goto Generate;
        }
    }
}
public record LightGenerator() : IGridGenerator<Color> {
    public LocationContext lc;
    public int radius;
    public LightGenerator(LocationContext lc, int radius) : this() {
        this.lc = lc;
        this.radius = radius;
    }
    public Color Generate((int, int) p) {
        //var xy = new XY(p);
        return new Color(255, 255, 204, Math.Min(255, (int)(radius * 255 / ((lc.pos - p).magnitude + 1))));
    }
}
public record SystemStar() : SystemElement {
    public int radius;
    public SystemStar(XElement e) : this() {
        this.radius = e.ExpectAttInt("radius");
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        /*
        var diameter = radius * 2;
        var radius2 = radius * radius;
        var center = new XY(radius, radius);
        for (int x = 0; x < diameter; x++) {
            var xOffset = Math.Abs(x - radius);
            var yRange = Math.Sqrt(radius2 - (xOffset * xOffset));
            var yStart = Math.Round(yRange, MidpointRounding.AwayFromZero);
            for (int y = -(int)yStart; y < yRange; y++) {
                var pos = new XY(x, y);
                var offset = (pos - center);
                var tile = new ColoredGlyph(Color.Gray, Color.Black, '%');
                lc.world.AddEffect(new FixedTile(tile, lc.pos + offset));
            }
        }
        */
        lc.world.backdrop.starlight.layers.Insert(0, new GeneratedGrid<Color>(new LightGenerator(lc, radius)));
        lc.world.stars.Add(new Star(lc.pos, radius));
    }
}
public record SystemShips : SystemElement {
    //[Opt] public string targetId = "";
    [Req] public string sovereign;
    private Sovereign sov;
    public ShipGenerator ships;
    public SystemShips() { }
    public SystemShips(TypeCollection tc, XElement e) {
        e.Initialize(this);
        sov = tc.Lookup<Sovereign>(sovereign);
        ships = SGenerator.ShipFrom(tc, e);
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        //var target = (ActiveObject)lc.world.universe.named[targetId];
        var m = new ActiveMarker(lc.world, sov, lc.pos);
        ships.GenerateAndPlace(tc, m);
    }
}
public record SystemStation() : SystemElement {
    [Opt]public string id = "";
    [Req] public string codename;
    
    public ShipGroup ships;
    public StationType stationtype;
    public SystemStation(TypeCollection tc, XElement e) : this() {
        e.Initialize(this);

        ships = e.HasElement("Ships", out var xmlShips) ? new(xmlShips, SGenerator.ParseFrom(tc, SGenerator.ShipFrom)) : null;
        stationtype = tc.Lookup<StationType>(codename);
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var w = lc.world;
        var s = new Station(w, stationtype, lc.pos);
        if (id.Any() == true) {
            w.universe.named[id] = s;
        }
        w.AddEntity(s);
        s.CreateSegments();
        s.CreateGuards();
        s.CreateSatellites(lc);
        ((ShipGenerator)ships)?.GenerateAndPlace(tc, s);
    }
}
public record SystemStargate() : SystemElement {
    public string gateId;
    public string destGateId;
    public ShipGenerator ships;
    public SystemStargate(TypeCollection tc, XElement e) : this() {
        gateId = e.ExpectAtt(nameof(gateId));
        destGateId = e.TryAtt(nameof(destGateId));
        if (e.HasElement("Ships", out XElement xmlShips)) {
            ships = new ShipGroup(xmlShips, SGenerator.ParseFrom(tc, SGenerator.ShipFrom));
        }
    }
    public void Generate(LocationContext lc, TypeCollection tc, List<Entity> result = null) {
        var s = new Stargate(lc.world, lc.pos) { gateId = gateId, destGateId = destGateId };
        lc.world.AddEntity(s);
        s.CreateSegments();

        ships?.GenerateAndPlace(tc, s);
    }
}
