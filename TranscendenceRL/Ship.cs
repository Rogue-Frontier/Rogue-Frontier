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
    public class Ship : Entity {
        public World world;
        public ShipClass shipClass = new ShipClass() { thrust = 5, maxSpeed = 20, rotationAccel = 4, rotationDecel = 2, rotationMaxSpeed = 3};
        public XY position { get; private set; }
        public XY velocity;
        public double rotationDegrees;

        public bool thrusting;
        public Rotating rotating;
        public double rotatingSpeed;
        public bool decelerating;
        public Ship(World world) {
            this.world = world;
            position = new XY();
            velocity = new XY();
        }
        public void SetThrusting(bool thrusting = true) => this.thrusting = thrusting;
        public void SetRotating(Rotating rotating = Rotating.None) {
            this.rotating = rotating;
        }
        public void SetDecelerating(bool decelerating = true) => this.decelerating = decelerating;
        public void Update() {
            
            if(thrusting) {
                velocity += XY.Polar(rotationDegrees * Math.PI / 180, shipClass.thrust);
                if (velocity.Magnitude > shipClass.maxSpeed) {
                    velocity = velocity.Normal * shipClass.maxSpeed;
                }
                thrusting = false;
            }
            if(rotating != Rotating.None) {
                if(rotating == Rotating.CCW) {
                    rotatingSpeed += shipClass.rotationAccel;
                } else if(rotating == Rotating.CW) {
                    rotatingSpeed -= shipClass.rotationAccel;
                }
                rotatingSpeed = Math.Min(Math.Abs(rotatingSpeed), shipClass.rotationMaxSpeed) * Math.Sign(rotatingSpeed);
                rotating = Rotating.None;
            } else {
                rotatingSpeed -= Math.Min(Math.Abs(rotatingSpeed), shipClass.rotationDecel) * Math.Sign(rotatingSpeed);
            }
            rotationDegrees += rotatingSpeed;

            if(decelerating) {
                if(velocity.Magnitude > 0.05) {
                    velocity -= velocity.Normal * Math.Min(velocity.Magnitude, shipClass.thrust / 2);
                } else {
                    velocity = new XY();
                }
                decelerating = false;
            }

            position += velocity / 30;
        }
        public bool Active => true;
        public ColoredGlyph tile => new ColoredGlyph('y', Color.Purple, Color.Transparent);
    }
}
