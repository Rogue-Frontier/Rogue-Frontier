using Common;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public enum Rotating {
        None, CCW, CW
    }
    public static class SShip {
        public static bool CanTarget(this IShip owner, SpaceObject target) {

            { owner = (owner is AIShip s) ? s.Ship : owner; }
            { owner = (owner is PlayerShip s) ? s.Ship : owner; }
            { target = (target is AIShip s) ? s.Ship : target; }
            { target = (target is PlayerShip s) ? s.Ship : target; }

            return owner != target && owner.Sovereign.IsEnemy(target) && !(target is Wreck);
        }
    }
    public interface IShip : SpaceObject {
        ShipClass ShipClass { get; }
        double rotationDegrees { get; }
    }
    public class Ship : IShip {
        public string Name => ShipClass.name;
        public World World { get; private set; }
        public ShipClass ShipClass { get; private set; }
        public Sovereign Sovereign { get; private set; }
        public XY Position { get; set; }
        public XY Velocity { get; set; }
        public bool Active { get; private set; }
        public HashSet<Item> Items;
        public DeviceSystem Devices { get; private set; }
        public DamageSystem DamageSystem;

        public Random destiny;

        public double rotationDegrees { get; private set; }
        public double stoppingRotation { get {
                var stoppingTime = TranscendenceRL.TICKS_PER_SECOND * Math.Abs(rotatingVel) / (ShipClass.rotationDecel);
                return rotationDegrees + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((ShipClass.rotationDecel / TranscendenceRL.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
        }}

        public bool thrusting;
        public Rotating rotating;
        public double rotatingVel;
        public bool decelerating;
        public Ship(World world, ShipClass shipClass, Sovereign Sovereign, XY Position) {
            this.World = world;
            this.ShipClass = shipClass;

            this.Sovereign = Sovereign;

            this.Position = Position;
            Velocity = new XY();

            this.Active = true;

            Items = new HashSet<Item>();

            Devices = new DeviceSystem();
            Devices.Add(shipClass.devices.Generate(world.types));

            DamageSystem = shipClass.damageDesc.Create(this);
            this.destiny = new Random(world.karma.Next());
        }
        public void SetThrusting(bool thrusting = true) => this.thrusting = thrusting;
        public void SetRotating(Rotating rotating = Rotating.None) {
            this.rotating = rotating;
        }
        public void SetDecelerating(bool decelerating = true) => this.decelerating = decelerating;

        public void Damage(SpaceObject source, int hp) => DamageSystem.Damage(source, hp);

        public void Destroy(SpaceObject source) {
            var wreck = new Wreck(this);
            wreck.Items.UnionWith(Items);
            World.AddEntity(wreck);
            Active = false;
        }

        public void Update() {
            UpdateControls();
            UpdateMotion();
            Devices.Update(this);
        }
        public void UpdateControls() {
            if (thrusting) {
                var rotationRads = rotationDegrees * Math.PI / 180;

                var exhaust = new EffectParticle(Position + XY.Polar(rotationRads, -1),
                    Velocity + XY.Polar(rotationRads, -ShipClass.thrust),
                    new ColoredGlyph('.', Color.Yellow, Color.Transparent),
                    4);
                World.AddEffect(exhaust);

                Velocity += XY.Polar(rotationRads, ShipClass.thrust);
                if (Velocity.Magnitude > ShipClass.maxSpeed) {
                    Velocity = Velocity.Normal * ShipClass.maxSpeed;
                }

                thrusting = false;
            }
            if (rotating != Rotating.None) {
                if (rotating == Rotating.CCW) {
                    /*
                    if (rotatingSpeed < 0) {
                        rotatingSpeed += Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if(rotatingVel < 0) {
                        Decel();
                    }
                    rotatingVel += ShipClass.rotationAccel / TranscendenceRL.TICKS_PER_SECOND;
                } else if (rotating == Rotating.CW) {
                    /*
                    if(rotatingSpeed > 0) {
                        rotatingSpeed -= Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if (rotatingVel > 0) {
                        Decel();
                    }
                    rotatingVel -= ShipClass.rotationAccel / TranscendenceRL.TICKS_PER_SECOND;
                }
                rotatingVel = Math.Min(Math.Abs(rotatingVel), ShipClass.rotationMaxSpeed) * Math.Sign(rotatingVel);
                rotating = Rotating.None;
            } else {
                Decel();
            }
            void Decel() => rotatingVel -= Math.Min(Math.Abs(rotatingVel), ShipClass.rotationDecel / TranscendenceRL.TICKS_PER_SECOND) * Math.Sign(rotatingVel); ;
            rotationDegrees += rotatingVel;

            if (decelerating) {
                if (Velocity.Magnitude > 0.05) {
                    Velocity -= Velocity.Normal * Math.Min(Velocity.Magnitude, ShipClass.thrust / 2);
                } else {
                    Velocity = new XY();
                }
                decelerating = false;
            }
        }
        public void UpdateMotion() {
            Position += Velocity / TranscendenceRL.TICKS_PER_SECOND;
        }
        public ColoredGlyph Tile => ShipClass.tile.Glyph;
    }
    public class AIShip : IShip {

        public static int ID = 0;
        public int Id = ID++;
        public string Name => Ship.Name;
        public World World => Ship.World;
        public ShipClass ShipClass => Ship.ShipClass;
        public Sovereign Sovereign => Ship.Sovereign;
        public XY Position => Ship.Position;
        public XY Velocity => Ship.Velocity;
        public double rotationDegrees => Ship.rotationDegrees;
        public DeviceSystem Devices => Ship.Devices;

        public DamageSystem DamageSystem => Ship.DamageSystem;

        public Ship Ship;
        public Order controller;
        public Docking docking;

        public AIShip(Ship ship, Order controller) {
            this.Ship = ship;
            this.controller = controller;

            //To do: Don't add anything to world in the constructor
            ship.World.AddEffect(new Heading(this));
        }
        public void SetThrusting(bool thrusting = true) => Ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => Ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => Ship.SetDecelerating(decelerating);
        public void Damage(SpaceObject source, int hp) => Ship.Damage(source, hp);
        public void Destroy(SpaceObject source) {
            if (source is PlayerShip ps) {
                ps.shipsDestroyed.Add(ShipClass);
            }
            Ship.Destroy(source);
        }
        public void Update() {

            controller.Update();

            docking?.Update();

            Ship.UpdateControls();
            Ship.UpdateMotion();

            //We update the ship's devices as ourselves because they need to know who the exact owner is
            //In case someone other than us needs to know who we are through our devices
            Ship.Devices.Update(this);
        }
        public bool Active => Ship.Active;
        public ColoredGlyph Tile => Ship.Tile;
    }
    public class PlayerShip : IShip {
        public string Name => Ship.Name;
        public World World => Ship.World;
        public ShipClass ShipClass => Ship.ShipClass;
        public Sovereign Sovereign => Ship.Sovereign;
        public XY Position => Ship.Position;
        public XY Velocity => Ship.Velocity;
        public double rotationDegrees => Ship.rotationDegrees;
        public HashSet<Item> Items => Ship.Items;

        public int targetIndex = -1;
        public List<SpaceObject> targetList = new List<SpaceObject>();

        public bool firingPrimary = false;
        private int selectedPrimary = 0;

        public Ship Ship;
        public PowerSystem power;

        public Docking docking;

        public List<IPlayerMessage> messages = new List<IPlayerMessage>();

        public HashSet<Entity> visible = new HashSet<Entity>();
        public HashSet<Station> known = new HashSet<Station>();
        int ticks = 0;

        public DictCounter<ShipClass> shipsDestroyed = new DictCounter<ShipClass>();

        public PlayerShip(Ship ship) {
            this.Ship = ship;

            //To do: Don't add anything to world in the constructor
            ship.World.AddEffect(new Heading(this));
            power = new PowerSystem(ship.Devices);
        }
        public void SetThrusting(bool thrusting = true) => Ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => Ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => Ship.SetDecelerating(decelerating);
        public void SetFiringPrimary(bool firingPrimary = true) => this.firingPrimary = firingPrimary;
        public void NextWeapon() {
            selectedPrimary++;
            if(selectedPrimary >= Ship.Devices.Weapons.Count) {
                selectedPrimary = 0;
            }
        }
        public void NextTarget() {
            bool canRefresh = true;
            if(targetIndex >= targetList.Count - 1) {
                Refresh();
            }

        CheckTarget:
            targetIndex++;
            if(targetIndex < targetList.Count) {
                var target = targetList[targetIndex];
                if(!this.CanTarget(target)) {
                    goto CheckTarget;
                } else if (!target.Active) {
                    goto CheckTarget;
                } else if((target.Position - Position).Magnitude > 100) {
                    goto CheckTarget;
                } else {
                    //Found target
                }
            } else {
                targetIndex = -1;
                if (canRefresh) {
                    Refresh();
                    goto CheckTarget;
                } else {
                    //Could not find target
                }
            }

            void Refresh() {
                targetList = World.entities.all.OfType<SpaceObject>().Where(e => this.CanTarget(e)).ToList();
                canRefresh = false;
            }
        }
        public void ForgetTarget() {
            targetList = targetList.GetRange(targetIndex, targetList.Count - targetIndex);
            targetIndex = -1;
        }
        public bool GetTarget(out SpaceObject target) {
            if (targetIndex != -1) {
                target = targetList[targetIndex];
                if (target.Active) {
                    return true;
                } else {
                    ForgetTarget();
                    target = null;
                    return false;
                }
            } else {
                target = null;
                return false;
            }
        }
        public Weapon GetPrimary() {
            if(selectedPrimary < Ship.Devices.Weapons.Count) {
                return Ship.Devices.Weapons[selectedPrimary];
            }
            return null;
        }
        public void Damage(SpaceObject source, int hp) => Ship.Damage(source, hp);
        public void Destroy(SpaceObject source) => Ship.Destroy(source);
        public void Update() {
            messages.ForEach(m => m.Update());
            messages.RemoveAll(m => !m.Active);

            if(GetTarget(out SpaceObject target)) {
                Heading.Crosshair(World, target.Position);
            }

            if (firingPrimary && selectedPrimary < Ship.Devices.Weapons.Count) {
                if(!power.disabled.Contains(Ship.Devices.Weapons[selectedPrimary])) {
                    Ship.Devices.Weapons[selectedPrimary].SetFiring(true, target);
                }
                firingPrimary = false;
            }

            ticks++;
            visible = new HashSet<Entity>(World.entities.GetAll(p => (Position - p).MaxCoord < 50));
            if (ticks%30 == 0) {
                foreach (var s in visible.OfType<Station>().Where(s => !known.Contains(s))) {
                    messages.Add(new Transmission(s, $"Discovered: {s.StationType.name}"));
                    known.Add(s);
                }
            }

            docking?.Update();

            Ship.UpdateControls();
            Ship.UpdateMotion();

            //We update the ship's devices as ourselves because they need to know who the exact owner is
            //In case someone other than us needs to know who we are through our devices
            foreach (var enabled in Ship.Devices.Installed.Where(i => !power.disabled.Contains(i))) {
                enabled.Update(this);
            }
            power.Update();
        }
        public void AddMessage(IPlayerMessage message) {
            var existing = messages.FirstOrDefault(m => m.message.String.Equals(message.message.String));
            if (existing != null) {
                existing.Reset();
            } else {
                messages.Add(message);
            }
        }
        public bool Active => Ship.Active;
        public ColoredGlyph Tile => Ship.Tile;
    }
}
