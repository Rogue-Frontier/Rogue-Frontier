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
    public bool active { get; set; } = true;
    public ColoredGlyph tile => new(Color.White, Color.Black, '.');
    public Cable(Hook parent, XY position) {
        this.parent = parent;
        id = parent.attached.world.nextId++;
        this.position = position;
    }
    public void Update() {
        active &= parent.active;
    }
}
public class Hook : Entity {
    public StructureObject attached, source;
    List<Cable> segments;
    public Hook(StructureObject attached, StructureObject source) {
        this.attached = attached;
        this.source = source;
        id = attached.world.nextId++;

        var offset = attached.position - source.position;
        var distance = (int)offset.magnitude - 1;

        if (distance < 1) {
            active = false;
            return;
        }

        var direction = offset.normal;
        segments = new(Enumerable.Range(1, distance).Select(
            i => new Cable(this, source.position + direction * i)));
        segments.ForEach(attached.world.AddEntity);
    }
    public int id { get; set; }
    public XY position => attached.position - offset.normal;
    public XY offset => attached.position - source.position;
    public bool active { get; set; } = true;
    public ColoredGlyph tile => new(Color.White, Color.Black, '?');
    public void Update() {
        active &= attached.active && source.active;
        var offset = attached.position - source.position;
        segments.ForEach(attached.world.AddEntity);
        var length = segments.Count;
        for (int i = 0; i < length; i++) {
            segments[i].position = source.position + offset * (i+1) / (length+1);
        }
        var stretch = offset.magnitude - length;
        if (stretch > 0) {
            var direction = offset.normal;
            source.velocity += direction * stretch / 30;
            attached.velocity -= direction * stretch / 30;
            if(stretch > 5) {
                active = false;
            }
        } else {
            int remove = (int)-stretch;
            segments.GetRange(length - remove, remove).ForEach(s=>s.active=false);
            segments.RemoveRange(length - remove, remove);
        }
    }
}
