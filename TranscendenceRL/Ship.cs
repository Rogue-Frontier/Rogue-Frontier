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
    public interface IShip : Entity {
        World world { get; }
        ShipClass shipClass { get; }
        XY velocity { get; }
        double rotationDegrees { get; }
    }
    public class Ship : IShip {
        public World world { get; private set; }
        public ShipClass shipClass { get; private set; }
        public XY Position { get; private set; }
        public XY velocity { get; private set; }
        public double rotationDegrees { get; private set; }

        public bool thrusting;
        public Rotating rotating;
        public double rotatingSpeed;
        public bool decelerating;

        List<Item> devices;
        public Ship(World world, ShipClass shipClass, XY Position) {
            this.world = world;
            this.shipClass = shipClass;
            this.Position = Position;
            velocity = new XY();
        }
        public void SetThrusting(bool thrusting = true) => this.thrusting = thrusting;
        public void SetRotating(Rotating rotating = Rotating.None) {
            this.rotating = rotating;
        }
        public void SetDecelerating(bool decelerating = true) => this.decelerating = decelerating;
        public void Update() {

            if (thrusting) {
                velocity += XY.Polar(rotationDegrees * Math.PI / 180, shipClass.thrust);
                if (velocity.Magnitude > shipClass.maxSpeed) {
                    velocity = velocity.Normal * shipClass.maxSpeed;
                }
                thrusting = false;
            }
            if (rotating != Rotating.None) {
                if (rotating == Rotating.CCW) {
                    rotatingSpeed += shipClass.rotationAccel;
                } else if (rotating == Rotating.CW) {
                    rotatingSpeed -= shipClass.rotationAccel;
                }
                rotatingSpeed = Math.Min(Math.Abs(rotatingSpeed), shipClass.rotationMaxSpeed) * Math.Sign(rotatingSpeed);
                rotating = Rotating.None;
            } else {
                rotatingSpeed -= Math.Min(Math.Abs(rotatingSpeed), shipClass.rotationDecel) * Math.Sign(rotatingSpeed);
            }
            rotationDegrees += rotatingSpeed;

            if (decelerating) {
                if (velocity.Magnitude > 0.05) {
                    velocity -= velocity.Normal * Math.Min(velocity.Magnitude, shipClass.thrust / 2);
                } else {
                    velocity = new XY();
                }
                decelerating = false;
            }

            Position += velocity / 30;
        }
        public bool Active => true;
        public ColoredGlyph Tile => new ColoredGlyph('y', Color.Purple, Color.Transparent);
    }

    public class PlayerShip : IShip {
        Ship ship;
        public World world => ship.world;
        public ShipClass shipClass => ship.shipClass;
        
        public XY Position => ship.Position;
        public XY velocity => ship.velocity;
        public double rotationDegrees => ship.rotationDegrees;
        public List<PlayerMessage> messages;

        public PlayerShip(Ship ship) {
            this.ship = ship;
            ship.world.AddEffect(new Heading(this));
            messages = new List<PlayerMessage>();
        }
        public void SetThrusting(bool thrusting = true) => ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => ship.SetDecelerating(decelerating);
        public void Update() {
            messages.ForEach(m => m.Update());
            messages.RemoveAll(m => !m.Active);
            ship.Update();
        }
        public bool Active => ship.Active;
        public ColoredGlyph Tile => ship.Tile;
    }
}
