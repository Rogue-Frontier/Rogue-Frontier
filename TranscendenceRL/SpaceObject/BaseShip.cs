using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranscendenceRL.Types;
using IslandHopper;

namespace TranscendenceRL {
    public enum Rotating {
        None, CCW, CW
    }
    public static class SShip {
        public static bool IsEnemy(this IShip owner, SpaceObject target) {
            return owner.CanTarget(target) && owner.Sovereign.IsEnemy(target) && !(target is Wreck);
        }
        public static bool IsFriendly(this IShip owner, SpaceObject target) {
            return owner.CanTarget(target) && owner.Sovereign.IsFriend(target) && !(target is Wreck);
        }
        public static bool CanTarget(this IShip owner, SpaceObject target) {
            return owner != target;
        }
    }
    public interface IShip : SpaceObject {
        XY Position { get; set; }
        XY Velocity { get; set; }
        DeviceSystem Devices { get; }
        ShipClass ShipClass { get; }
        double rotationDegrees { get; }
        public double stoppingRotation { get; }
        Docking Dock { get; set; }
    }
    public class BaseShip : SpaceObject {
        public static BaseShip dead => new BaseShip(World.empty, ShipClass.empty, Sovereign.Gladiator, XY.Zero) { Active = false };

        public string Name => ShipClass.name;
        public World World { get; private set; }
        public ShipClass ShipClass { get; private set; }
        public Sovereign Sovereign { get; private set; }
        public XY Position { get; set; }
        public XY Velocity { get; set; }
        public bool Active { get; set; }
        public HashSet<Item> Items;
        public DeviceSystem Devices { get; private set; }
        public DamageSystem DamageSystem;

        public Random destiny;

        public delegate void Destroyed(BaseShip ship, SpaceObject destroyer, Wreck wreck);
        public event Destroyed OnDestroyed;

        public double rotationDegrees { get; set; }
        public double stoppingRotation { get {
                var stoppingTime = TranscendenceRL.TICKS_PER_SECOND * Math.Abs(rotatingVel) / (ShipClass.rotationDecel);
                return rotationDegrees + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((ShipClass.rotationDecel / TranscendenceRL.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
        }}

        public double stoppingRotationWithCounterTurn {
            get {
                var stoppingRate = ShipClass.rotationDecel + ShipClass.rotationAccel;
                var stoppingTime = Math.Abs(TranscendenceRL.TICKS_PER_SECOND * rotatingVel / stoppingRate);
                return rotationDegrees + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((stoppingRate / TranscendenceRL.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
            }
        }

        public bool thrusting;
        public Rotating rotating;
        public double rotatingVel;
        public bool decelerating;

        public BaseShip(World world, ShipClass shipClass, Sovereign Sovereign, XY Position) {
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
        public void Damage(SpaceObject source, int hp) => DamageSystem.Damage(this, source, hp);
        public void Destroy(SpaceObject source) {
            var wreck = new Wreck(this);
            wreck.Items.UnionWith(Items);
            World.AddEntity(wreck);

            OnDestroyed?.Invoke(this, source, wreck);
            Active = false;
        }
        public void Update() {
            UpdateControls();
            UpdateMotion();
            //Devices.Update(this);
        }
        public void UpdateControls() {
            if (thrusting) {
                var rotationRads = rotationDegrees * Math.PI / 180;

                var exhaust = new EffectParticle(Position + XY.Polar(rotationRads, -1),
                    Velocity + XY.Polar(rotationRads, -ShipClass.thrust),
                    new ColoredGlyph(Color.Yellow, Color.Transparent, '.'),
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
        public XY Position { get => Ship.Position; set => Ship.Position = value; }
        public XY Velocity { get => Ship.Velocity; set => Ship.Velocity = value; }
        public double rotationDegrees => Ship.rotationDegrees;
        public DeviceSystem Devices => Ship.Devices;

        public DamageSystem DamageSystem => Ship.DamageSystem;

        public BaseShip Ship;
        public Order controller;
        public Docking Dock { get; set; }
        public Random destiny => Ship.destiny;
        public double stoppingRotation => Ship.stoppingRotation;

        public AIShip(BaseShip ship, Order controller) {
            this.Ship = ship;
            this.controller = controller;
        }
        public void SetThrusting(bool thrusting = true) => Ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => Ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => Ship.SetDecelerating(decelerating);
        public void Damage(SpaceObject source, int hp) => Ship.Damage(source, hp);
        public void Destroy(SpaceObject source) {
            if (source is PlayerShip ps) {
                ps.ShipsDestroyed.Add(ShipClass);
            }
            Ship.Destroy(source);
        }
        public void Update() {

            controller.Update(this);

            Dock?.Update(this);

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
        public Player player;
        public string Name => Ship.Name;
        public World World => Ship.World;
        public ShipClass ShipClass => Ship.ShipClass;
        public Sovereign Sovereign => Ship.Sovereign;
        public XY Position { get => Ship.Position; set => Ship.Position = value; }
        public XY Velocity { get => Ship.Velocity; set => Ship.Velocity = value; }
        public double rotationDegrees => Ship.rotationDegrees;
        public double stoppingRotation => Ship.stoppingRotation;
        public HashSet<Item> Items => Ship.Items;

        public int targetIndex = -1;
        public bool targetFriends = false;
        public List<SpaceObject> targetList = new List<SpaceObject>();

        public bool firingPrimary = false;
        public int selectedPrimary = 0;

        public int mortalChances = 3;
        public double mortalTime = 0;
        public DeviceSystem Devices => Ship.Devices;
        public BaseShip Ship;
        public EnergySystem Energy;
        public List<Power> Powers;
        public Docking Dock { get; set; }

        public List<IPlayerMessage> Messages = new List<IPlayerMessage>();

        public HashSet<Entity> Visible = new HashSet<Entity>();
        public HashSet<Station> Known = new HashSet<Station>();
        int ticks = 0;

        public DictCounter<ShipClass> ShipsDestroyed = new DictCounter<ShipClass>();

        public delegate void PlayerDestroyed(PlayerShip playerShip, SpaceObject destroyer, Wreck wreck);

        public event PlayerDestroyed OnDestroyed;
        public delegate void PlayerDamaged(PlayerShip playerShip, SpaceObject damager, int hp);
        public event PlayerDamaged OnDamaged;

        public PlayerShip(Player player, BaseShip ship) {
            this.player = player;
            this.Ship = ship;

            Energy = new EnergySystem(ship.Devices);
            Powers = new List<Power>();

            //Remember to create the Heading when you add or replace this ship in the World


            //Hook up our own event to the ship since calling Damage can call base ship's Destroy without calling our own Destroy()
            ship.OnDestroyed += (s, source, wreck) => OnDestroyed?.Invoke(this, source, wreck);
        }
        public void SetThrusting(bool thrusting = true) => Ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => Ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => Ship.SetDecelerating(decelerating);
        public void SetFiringPrimary(bool firingPrimary = true) => this.firingPrimary = firingPrimary;
        public void SetRotatingToFace(double targetRads) {
            var facingRads = Ship.stoppingRotationWithCounterTurn * Math.PI / 180;

            var ccw = (XY.Polar(facingRads + 3 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            var cw = (XY.Polar(facingRads - 3 * Math.PI / 180) - XY.Polar(targetRads)).Magnitude;
            if (ccw < cw) {
                SetRotating(Rotating.CCW);
            } else if (cw < ccw) {
                SetRotating(Rotating.CW);
            } else {
                if (Ship.rotatingVel > 0) {
                    SetRotating(Rotating.CW);
                } else {
                    SetRotating(Rotating.CCW);
                }
            }
        }
        public void NextWeapon() {
            selectedPrimary++;
            if(selectedPrimary >= Ship.Devices.Weapons.Count) {
                selectedPrimary = 0;
            }
        }
        public void PrevWeapon() {
            selectedPrimary--;
            if (selectedPrimary < 0) {
                selectedPrimary = 0;
            }
        }
        /*
        //No, let the player always choose the closest target to cursor. No iteration.
        public void NextTargetSet(SpaceObject next) {
            var index = targetList.IndexOf(next);
            if(index != -1) {
                if(index <= targetIndex) {
                    bool canRefresh = true;

                CheckTarget:
                    targetIndex++;
                    if (targetIndex < targetList.Count) {
                        var target = targetList[targetIndex];
                        if (!target.Active) {
                            goto CheckTarget;
                        } else if ((target.Position - Position).Magnitude > 100) {
                            goto CheckTarget;
                        } else {
                            //Found target
                            UpdateAutoAim();
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
                        targetList = World.entities.all.OfType<SpaceObject>().OrderBy(e => (e.Position - Position).Magnitude).ToList();
                        canRefresh = false;
                    }
                }
            } else {

            }
        }
        */
        public void NextTargetEnemy() {
            bool canRefresh = true;

            if(targetFriends) {
                Refresh();
                targetFriends = false;
            } else if(targetIndex >= targetList.Count - 1) {
                Refresh();
            }

        CheckTarget:
            targetIndex++;
            if(targetIndex < targetList.Count) {
                var target = targetList[targetIndex];
                if(!this.IsEnemy(target)) {
                    goto CheckTarget;
                } else if (!target.Active) {
                    goto CheckTarget;
                } else if((target.Position - Position).Magnitude > 100) {
                    goto CheckTarget;
                } else {
                    //Found target
                    UpdateAutoAim();
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
                targetList = World.entities.all.OfType<SpaceObject>().Where(e => this.IsEnemy(e)).OrderBy(e => (e.Position - Position).Magnitude).ToList();
                canRefresh = false;
            }
        }
        public void NextTargetFriendly() {
            bool canRefresh = true;

            if (!targetFriends) {
                Refresh();
                targetFriends = true;
            } else if (targetIndex >= targetList.Count - 1) {
                Refresh();
            }

        CheckTarget:
            targetIndex++;
            if (targetIndex < targetList.Count) {
                var target = targetList[targetIndex];
                if (!this.IsFriendly(target)) {
                    goto CheckTarget;
                } else if (!target.Active) {
                    goto CheckTarget;
                } else if ((target.Position - Position).Magnitude > 100) {
                    goto CheckTarget;
                } else {
                    //Found target
                    UpdateAutoAim();
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
                targetList = World.entities.all.OfType<SpaceObject>().Where(e => this.IsFriendly(e)).OrderBy(e => (e.Position - Position).Magnitude).ToList();
                canRefresh = false;
            }
        }
        //Remember to call this before we set the targetIndex == -1
        public void ResetAutoAim() {
            var target = targetList[targetIndex];
            if (selectedPrimary < Ship.Devices.Weapons.Count) {
                var primary = Ship.Devices.Weapons[selectedPrimary];
                if(primary.target == target) {
                    primary.target = null;
                }
            }
        }
        //Remember to call this after we set the targetIndex > -1
        public void UpdateAutoAim() {
            var target = targetList[targetIndex];
            if (selectedPrimary < Ship.Devices.Weapons.Count) {
                var primary = Ship.Devices.Weapons[selectedPrimary];
                primary.target = target;
            }
        }
        //Stop targeting, but remember our remaining targets
        public void ForgetTarget() {
            ResetAutoAim();
            targetList = targetList.GetRange(targetIndex, targetList.Count - targetIndex);
            targetIndex = -1;
        }
        //Stop targeting and clear our target list
        public void ClearTarget() {
            ResetAutoAim();
            targetList.Clear();
            targetIndex = -1;
        }

        public void SetTargetList(List<SpaceObject> targetList) {
            if(targetIndex > -1) {
                ResetAutoAim();
            }
            this.targetList = targetList;
            if(targetList.Count > 0) {
                targetIndex = 0;
                UpdateAutoAim();
            } else {
                targetIndex = -1;

            }
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
        public bool GetPrimary(out Weapon result) {
            if (selectedPrimary < Ship.Devices.Weapons.Count) {
                result = Ship.Devices.Weapons[selectedPrimary];
                return true;
            }
            result = null;
            return false;
        }
        public void Damage(SpaceObject source, int hp) {
            //Base ship can get destroyed without calling our own Destroy(), so we need to hook up an OnDestroyed event to this
            Ship.Damage(source, hp);

            if(hp > Ship.DamageSystem.GetHP() / 3) {
                if(mortalTime <= 0) {
                    if(mortalChances > 0) {
                        AddMessage(new InfoMessage(new ColoredString("Escape while you can!", Color.Red, Color.Black)));

                        mortalTime = mortalChances * 3.0 + 1;
                        mortalChances--;
                    }
                }
            }

            OnDamaged?.Invoke(this, source, hp);
        }
        public void Destroy(SpaceObject source) {
            Ship.Destroy(source);
        }
        public void Update() {
            Messages.ForEach(m => m.Update());
            Messages.RemoveAll(m => !m.Active);

            if(GetTarget(out SpaceObject target)) {
                Heading.Crosshair(World, target.Position);
            }

            if (firingPrimary && selectedPrimary < Ship.Devices.Weapons.Count) {
                if(!Energy.disabled.Contains(Ship.Devices.Weapons[selectedPrimary])) {
                    Ship.Devices.Weapons[selectedPrimary].SetFiring(true, target);
                }
                firingPrimary = false;
            }

            ticks++;
            Visible = new HashSet<Entity>(World.entities.GetAll(p => (Position - p).MaxCoord < 50));
            if (ticks%30 == 0) {
                foreach (var s in Visible.OfType<Station>().Where(s => !Known.Contains(s))) {
                    Messages.Add(new Transmission(s, $"Discovered: {s.StationType.name}"));
                    Known.Add(s);
                }
            }

            Dock?.Update(this);

            Ship.UpdateControls();
            Ship.UpdateMotion();

            //We update the ship's devices as ourselves because they need to know who the exact owner is
            //In case someone other than us needs to know who we are through our devices
            foreach (var enabled in Ship.Devices.Installed.Where(i => !Energy.disabled.Contains(i))) {
                enabled.Update(this);
            }
            Energy.Update();
        }
        public void AddMessage(IPlayerMessage message) {
            var existing = Messages.FirstOrDefault(m => m.message.String.Equals(message.message.String));
            if (existing != null) {
                existing.Reset();
            } else {
                Messages.Add(message);
            }
        }
        public bool Active => Ship.Active;
        public ColoredGlyph Tile => Ship.Tile;
    }
}
