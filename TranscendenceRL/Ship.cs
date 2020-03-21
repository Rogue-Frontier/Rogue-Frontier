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
        public ShipClass shipClass = new ShipClass() { thrust = 5, maxSpeed = 20, rotationAccel = 4, rotationDecel = 2, rotationMaxSpeed = 3};
        public XY position { get; private set; }
        public XY velocity;
        public double rotationDegrees;

        public bool thrusting;
        public Rotating rotating;
        public double rotatingSpeed;
        public Ship() {
            position = new XY();
            velocity = new XY();
        }
        public void SetThrusting(bool thrusting = true) => this.thrusting = thrusting;
        public void SetRotating(Rotating rotating = Rotating.None) {
            this.rotating = rotating;
        }
        public void Update() {
            
            if(thrusting) {
                velocity += XY.Polar(rotationDegrees * Math.PI / 180, shipClass.thrust);
                if (velocity.Magnitude > shipClass.maxSpeed) {
                    velocity = velocity.Normal * shipClass.maxSpeed;
                }
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
        }
        public bool Active => true;
        public ColoredGlyph Tile => new ColoredGlyph('Y', Color.Purple, Color.Transparent);
    }
}
