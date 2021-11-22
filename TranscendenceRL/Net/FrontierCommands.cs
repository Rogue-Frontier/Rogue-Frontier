﻿using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public enum CommandServer {
        PLAYER_SHIP_ASSUME,
        PLAYER_SHIP_LEAVE,
        PLAYER_SHIP_INPUT,
    }
    public enum CommandClient {
        SET_WORLD,
        SET_CAMERA,
        ENTITY_ENFORCE_CERTAINTY
    }

    public class EntityLocation {
        public int id;
        public XY pos;
        public XY vel;
        public EntityLocation(int id, XY pos, XY vel) {
            this.id = id;
            this.pos = pos;
            this.vel = vel;
        }
    }
    public static class SFrontierCommon {

        public static void SetSharedState(this Entity e, Entity state) {
            switch (e) {
                case PlayerShip to when state is PlayerShip from: {
                        to.ship.rotationDeg = from.rotationDeg;
                        to.position = from.position;
                        to.velocity = from.velocity;
                        break;
                    }
                case AIShip to when state is AIShip from: {
                        to.ship.rotationDeg = from.rotationDeg;
                        to.position = from.position;
                        to.velocity = from.velocity;
                        break;
                    }
                case Projectile to when state is Projectile from: {
                        to.position = from.position;
                        to.velocity = from.velocity;
                        break;
                    }
                    
            }
        }
        public static Entity GetSharedState(this Entity e) {
            switch(e) {
                case PlayerShip ps:
                    return new PlayerShip() {
                        ship = new BaseShip() {
                            Id = e.Id,
                            rotationDeg = ps.ship.rotationDeg
                        },
                        position = ps.position,
                        velocity = ps.velocity
                    };
                case AIShip ai:
                    return new AIShip() {
                        ship = new BaseShip() {
                            Id = e.Id,
                            rotationDeg = ai.ship.rotationDeg
                        },
                        position = ai.position,
                        velocity = ai.velocity
                    };
                case Projectile p:
                    return new Projectile() {
                        Id = p.Id,
                        position = p.position,
                        velocity = p.velocity,
                    };
            }
            return null;
        }
        public static void UpdatePlayerControls(this Dictionary<PlayerShip, PlayerInput> playerControls) {
            foreach (var (player, input) in playerControls) {
                var c = new PlayerControls(player, null) { input = input };
                c.ProcessAll();
            }
        }
        public static void UpdateEntityLookup(this Dictionary<int, Entity> entityLookup, World World) {
            entityLookup.Clear();
            foreach (var e in World.entities.all) {
                entityLookup[e.Id] = e;
            }
        }
    }
}