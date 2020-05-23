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
    class Enemy : ICharacter {
        public Island World { get; set; }
        public XYZ Position { get; set; }
        public XYZ Velocity { get; set; }
        public ColoredString Name => new ColoredString("Enemy");
        public ColoredGlyph SymbolCenter => new ColoredGlyph('E');

        public bool Active { get; set; } = true;

        private AI controller;
        public HashSet<EntityAction> Actions { get; private set; } = new HashSet<EntityAction>();
        public HashSet<IItem> Inventory { get; private set; } = new HashSet<IItem>();
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
            foreach(var a in Actions) {
                a.Update();
            }
            Actions.RemoveWhere(a => a.Done());
            foreach (var i in Inventory) {
                i.Position = Position;
                i.Velocity = Velocity;
                i.UpdateStep();
            }
            Inventory.RemoveWhere(i => !i.Active);
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
        private IItem weapon;
        private EntityAction movement;
        private EntityAction attack;
        public AI(Enemy actor) {
            this.actor = actor;
        }
        public void Update() {
            UpdateAttack();
            UpdateMovement();

            void UpdateWeapon() {
                if(weapon == null || !actor.Inventory.Contains(weapon)) {
                    weapon = actor.Inventory.FirstOrDefault(i => i.Gun.AmmoLeft > 0 || i.Gun.ClipLeft > 0);
                }
            }
            void UpdateAttack() {
                if (attack == null || attack.Done()) {
                    UpdateWeapon();
                    if (weapon == null) {
                        return;
                    }
                    var enemies = new List<Entity>();
                    foreach (var point in actor.World.entities.space.Keys) {
                        if (((XYZ)point - actor.Position).Magnitude < 100) {
                            enemies.AddRange(actor.World.entities[point].Where(e => !(e is Item)));
                        }
                    }
                    if (!enemies.Any()) {
                        return;
                    }
                    var target = enemies.First();
                    attack = new ShootAction(actor, weapon, new TargetEntity(target));
                    actor.Actions.Add(attack);
                }
            }
            void UpdateMovement() {
                if (movement == null || movement.Done()) {
                    UpdateWeapon();
                    if(weapon == null) {

                        var weapons = new List<IItem>();
                        foreach (var point in actor.World.entities.space.Keys) {
                            if (((XYZ)point - actor.Position).Magnitude < 100) {
                                weapons.AddRange(actor.World.entities[point].OfType<IItem>());
                            }
                        }
                        if (!weapons.Any()) {
                            return;
                        }
                        var target = weapons.First();

                        Dictionary<(int, int, int), (int, int, int)> prev = new Dictionary<(int, int, int), (int, int, int)>();
                        Queue<XYZ> points = new Queue<XYZ>();
                        var start = target.Position;
                        prev[start] = start;
                        points.Enqueue(start);
                        int seen = 0;
                        bool success = true;
                        while(points.Any() && seen < 500 && !success) {
                            var point = points.Dequeue();
                            
                            foreach (var offset in new XYZ[] { new XYZ(0, 1), new XYZ(1, 0), new XYZ(0, -1), new XYZ(-1, 0) }) {
                                var next = point + offset;
                                if(prev.ContainsKey(next)) {
                                    continue;
                                } else if(CanOccupy(next)) {
                                    prev[next] = point;
                                    if(next == actor.Position) {
                                        success = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if(success) {
                            LinkedList<XYZ> path = new LinkedList<XYZ>();
                            
                            XYZ p = prev[actor.Position];
                            path.AddLast(p);
                            while(p != start) {
                                p = prev[p];
                                path.AddLast(p);
                            }

                            movement = new CompoundAction(new FollowPath(actor, null), new TakeItem(actor, weapons.First()));
                        } else {
                            UpdateWander();
                        }
                    } else {
                        UpdateWander();
                    }
                    
                }
            }
            void UpdateWander() {
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
                    if (CanOccupy(point)) {
                        accessible.Add(point);
                        foreach (var offset in new XYZ[] { new XYZ(0, 1), new XYZ(1, 0), new XYZ(0, -1), new XYZ(-1, 0) }) {
                            var next = point + offset;
                            var dist = prev[point].Item2 + 1;
                            if (known.Add(next)) {
                                prev[next] = (point, dist);
                                points.Enqueue(next);
                            } else if (prev.TryGetValue(next, out (XYZ, int) v) && v.Item2 > dist) {
                                prev[next] = (point, dist);
                            }
                        }
                    }
                }

                var dest = accessible.OrderByDescending(xyz => (actor.Position - xyz).Magnitude2).ElementAt(new Random().Next(0, 4));
                var path = new LinkedList<XYZ>();
                while (dest != null) {
                    path.AddFirst(dest);
                    XYZ next;
                    (next, _) = prev[dest];
                    dest = next;
                }
                movement = new FollowPath(actor, path);
                actor.Actions.Add(movement);
            }
            bool CanOccupy(XYZ position) {
                var v = actor.World.voxels.Try(position);
                var below = actor.World.voxels.Try(position.PlusZ(-1));
                return v is Air || v is Floor || below?.Collision == VoxelType.Solid;
            }
        }
    }
}
