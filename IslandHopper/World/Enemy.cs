using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Microsoft.Xna.Framework;
using Priority_Queue;
using SadConsole;

namespace IslandHopper.World {
    class Enemy : Entity, Actor, Damageable {
        public Island World { get; set; }
        public XYZ Position { get; set; }
        public XYZ Velocity { get; set; }
        public ColoredString Name => new ColoredString("Enemy");
        public ColoredGlyph SymbolCenter => new ColoredGlyph('E');

        public bool Active { get; set; } = true;

        private AI controller;
        public HashSet<EntityAction> actions { get; private set; } = new HashSet<EntityAction>();
        private HashSet<IItem> inventory = new HashSet<IItem>();
        private Health health;
        private int tick = 0;
        public Enemy(Island World, XYZ Position) {
            this.World = World;
            this.Position = Position;
            this.Velocity = new XYZ();
            this.controller = new AI(this);
            this.health = new Health();
        }

        public void OnRemoved() {
        }

        public void UpdateRealtime(TimeSpan delta) {
        }

        public void UpdateStep() {
            tick++;
            this.UpdateGravity();
            this.UpdateMotion();

            health.UpdateStep();
            if (health.bleeding > 0 && tick % 5 == 0) {
                World.AddEffect(new Trail(Position, 150, new ColoredGlyph('+', Color.Red, Color.Black)));
            }
            if (health.bloodHP < 1 || health.bodyHP < 1) {
                Active = false;
            }

            controller.Update();
            foreach(var a in actions) {
                a.Update();
            }
            actions.RemoveWhere(a => a.Done());
            foreach (var i in inventory) {
                i.Position = Position;
                i.Velocity = Velocity;
                i.UpdateStep();
            }
            inventory.RemoveWhere(i => !i.Active);
        }
        public void OnDamaged(Damager source) {
            if (source is Bullet b) {
                health.Damage(b.damage);
            } else if (source is ExplosionDamage e) {
                health.Damage(e.damage);
                Velocity += e.knockback;
            }
        }
    }
    class AI {
        private Enemy actor;
        private EntityAction order;
        public AI(Enemy actor) {
            this.actor = actor;
        }
        public void Update() {
            if(order?.Done() != false) {
                HashSet<(int, int, int)> known = new HashSet<(int, int, int)>();
                HashSet<XYZ> accessible = new HashSet<XYZ>();
                Dictionary<(int, int, int), (XYZ, int)> prev = new Dictionary<(int, int, int), (XYZ, int)>();
                Queue<XYZ> points = new Queue<XYZ>();

                var start = actor.Position.i;
                points.Enqueue(start);
                prev[start] = (null, 0);
                int seen = 0;
                while (points.Count > 0 && seen < 500) {
                    var point = points.Dequeue().i;
                    known.Add(point);
                    seen++;
                    if(CanOccupy(point)) {
                        accessible.Add(point);
                        foreach(var offset in new XYZ[] { new XYZ(0, 1), new XYZ(1, 0), new XYZ(0, -1), new XYZ(-1, 0) }) {
                            var next = point + offset;
                            var dist = prev[point].Item2 + 1;
                            if (known.Add(next)) {
                                prev[next] = (point, dist);
                                points.Enqueue(next);
                            } else if(prev.TryGetValue(next, out (XYZ, int) v) && v.Item2 > dist) {
                                prev[next] = (point, dist);
                            }
                        }
                    }
                }

                var dest = accessible.OrderByDescending(xyz => (actor.Position - xyz).Magnitude2).ElementAt(new Random().Next(0, 4));
                var path = new LinkedList<XYZ>();
                while(dest != null) {
                    path.AddFirst(dest);
                    XYZ next;
                    (next, _) = prev[dest];
                    dest = next;
                }
                order = new FollowPath(actor, path);
                actor.actions.Add(order);
            }
            bool CanOccupy(XYZ position) {
                var v = actor.World.voxels.Try(position);
                var below = actor.World.voxels.Try(position.PlusZ(-1));
                return v is Air || v is Floor || below?.Collision == VoxelType.Solid;
            }
        }
    }
}
