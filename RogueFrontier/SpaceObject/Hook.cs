using Common;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;

public class Cable : Entity {
    Hook parent;
    public int id { get; set; }
    public XY position { get; set; }
    public bool active => parent.active;
    public ColoredGlyph tile => new(Color.White, Color.Black, '*');
    public Cable(Hook parent, XY position) {
        this.parent = parent;
        id = parent.attached.world.nextId++;
        this.position = position;
    }
    public void Update() {}
}
public class Hook : Entity {
    public StructureObject attached, source;
    List<Cable> segments;
    public Hook(StructureObject attached, StructureObject source) {
        this.attached = attached;
        this.source = source;
        id = attached.world.nextId++;
        var p = source.position;
        var dest = attached.position;
        var offset = dest - p;
        var distance = (int)offset.magnitude;
        var direction = offset.normal;
        segments = new(Enumerable.Range(0, distance).Select(i => new Cable(this, p + direction * i)));
        segments.ForEach(attached.world.AddEntity);
    }

    public int id { get; set; }

    public XY position => attached.position;

    public bool active => throw new NotImplementedException();

    public ColoredGlyph tile => new(Color.White, Color.Black, '?');

    public void Update() {
    }
}
