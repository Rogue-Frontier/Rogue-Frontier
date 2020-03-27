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
    public interface IShip : SpaceObject {
        World World { get; }
        ShipClass ShipClass { get; }
        double rotationDegrees { get; }
    }
    public class Ship : IShip {
        public World World { get; private set; }
        public ShipClass ShipClass { get; private set; }
        public Sovereign Sovereign { get; private set; }
        public XY Position { get; private set; }
        public XY Velocity { get; private set; }
        public double rotationDegrees { get; private set; }

        public bool thrusting;
        public Rotating rotating;
        public double rotatingSpeed;
        public bool decelerating;


        public List<Device> Devices;
        public List<Weapon> Weapons;

        public Ship(World world, ShipClass shipClass, Sovereign Sovereign, XY Position) {
            this.World = world;
            this.ShipClass = shipClass;

            this.Sovereign = Sovereign;

            this.Position = Position;
            Velocity = new XY();

            Devices = shipClass.devices.Generate(world.types);
            UpdateDevices();
        }
        public void UpdateDevices() {
            Weapons = Devices.OfType<Weapon>().ToList();
        }
        public void SetThrusting(bool thrusting = true) => this.thrusting = thrusting;
        public void SetRotating(Rotating rotating = Rotating.None) {
            this.rotating = rotating;
        }
        public void SetDecelerating(bool decelerating = true) => this.decelerating = decelerating;
        public void Update() {

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
                    rotatingSpeed += ShipClass.rotationAccel / 30;
                } else if (rotating == Rotating.CW) {
                    /*
                    if(rotatingSpeed > 0) {
                        rotatingSpeed -= Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    rotatingSpeed -= ShipClass.rotationAccel / 30;
                }
                rotatingSpeed = Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationMaxSpeed) * Math.Sign(rotatingSpeed);
                rotating = Rotating.None;
            } else {
                rotatingSpeed -= Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel / 30) * Math.Sign(rotatingSpeed);
            }
            rotationDegrees += rotatingSpeed;

            if (decelerating) {
                if (Velocity.Magnitude > 0.05) {
                    Velocity -= Velocity.Normal * Math.Min(Velocity.Magnitude, ShipClass.thrust / 2);
                } else {
                    Velocity = new XY();
                }
                decelerating = false;
            }

            Position += Velocity / 30;

            Devices.ForEach(d => d.Update(this));
        }
        public bool Active => true;
        public ColoredGlyph Tile => ShipClass.tile.Glyph;
    }
    public class AIShip : IShip {
        Ship Ship;
        public World World => Ship.World;
        public ShipClass ShipClass => Ship.ShipClass;
        public Sovereign Sovereign => Ship.Sovereign;

        public XY Position => Ship.Position;
        public XY Velocity => Ship.Velocity;
        public double rotationDegrees => Ship.rotationDegrees;
        public List<PlayerMessage> messages;

        public AIShip(Ship Ship) {
            this.Ship = Ship;
            Ship.World.AddEffect(new Heading(this));
            messages = new List<PlayerMessage>();
        }
        public void SetThrusting(bool thrusting = true) => Ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => Ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => Ship.SetDecelerating(decelerating);
        public void Update() {
            messages.ForEach(m => m.Update());
            messages.RemoveAll(m => !m.Active);
            Ship.Update();
        }
        public bool Active => Ship.Active;
        public ColoredGlyph Tile => Ship.Tile;
    }
    public class PlayerShip : IShip {
        public World World => ship.World;
        public ShipClass ShipClass => ship.ShipClass;
        public Sovereign Sovereign => ship.Sovereign;
        public XY Position => ship.Position;
        public XY Velocity => ship.Velocity;
        public double rotationDegrees => ship.rotationDegrees;

        public bool firingPrimary;
        private Ship ship;
        public List<PlayerMessage> messages;
        private int selectedPrimary;


        public PlayerShip(Ship ship) {
            this.ship = ship;
            ship.World.AddEffect(new Heading(this));
            messages = new List<PlayerMessage>();
        }
        public void SetThrusting(bool thrusting = true) => ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => ship.SetDecelerating(decelerating);
        public void SetFiringPrimary(bool firingPrimary = true) => this.firingPrimary = firingPrimary;
        public void NextWeapon() {
            selectedPrimary++;
            if(selectedPrimary >= ship.Weapons.Count) {
                selectedPrimary = 0;
            }
        }
        public void Update() {
            messages.ForEach(m => m.Update());
            messages.RemoveAll(m => !m.Active);
            if(firingPrimary && selectedPrimary < ship.Weapons.Count) {
                ship.Weapons[selectedPrimary].SetFiring(true);
                firingPrimary = false;
            }
            
            ship.Update();
        }
        public bool Active => ship.Active;
        public ColoredGlyph Tile => ship.Tile;
    }
}
