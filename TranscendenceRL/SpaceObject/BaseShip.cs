using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using TranscendenceRL.Types;
using static TranscendenceRL.BaseShip;
using Newtonsoft.Json;

namespace TranscendenceRL {
    public enum Rotating {
        None, CCW, CW
    }

    public static class SStation {
        public static bool IsEnemy(this Station owner, SpaceObject target) {
            return (owner != target
                && (owner.Sovereign.IsEnemy(target)
                || target.Sovereign.IsEnemy(owner.Sovereign)))
                && !(target is Wreck);
        }
    }
    public static class SShip {
        public static bool IsEnemy(this IShip owner, SpaceObject target) {
            return owner.CanTarget(target) && (owner.Sovereign.IsEnemy(target) || target.Sovereign.IsEnemy(owner.Sovereign)) && !(target is Wreck);
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
        HashSet<Item> Items { get; }
        DeviceSystem Devices { get; }
        ShipClass ShipClass { get; }
        double rotationDegrees { get; }
        public double stoppingRotation { get; }
        Docking Dock { get; set; }
    }
    public class BaseShip : SpaceObject {
        [JsonIgnore]
        public static BaseShip dead => new BaseShip(World.empty, ShipClass.empty, Sovereign.Gladiator, XY.Zero) { Active = false };
        [JsonIgnore]
        public string Name => ShipClass.name;
        [JsonProperty]
        public World World { get; private set; }
        [JsonProperty]
        public ShipClass ShipClass { get; private set; }
        [JsonProperty]
        public Sovereign Sovereign { get; private set; }
        public XY Position { get; set; }
        public XY Velocity { get; set; }
        public bool Active { get; set; }
        [JsonProperty]
        public HashSet<Item> Items { get; private set; }
        [JsonProperty]
        public DeviceSystem Devices { get; private set; }
        [JsonProperty]
        public DamageSystem DamageSystem { get; private set; }
        public ControlHijack ControlHijack;

        public Rand destiny;

        public delegate void Destroyed(BaseShip ship, SpaceObject destroyer, Wreck wreck);
        public FuncSet<IContainer<Destroyed>> OnDestroyed = new FuncSet<IContainer<Destroyed>>();

        public double rotationDegrees { get; set; }
        [JsonIgnore]
        public double stoppingRotation { get {
                var stoppingTime = TranscendenceRL.TICKS_PER_SECOND * Math.Abs(rotatingVel) / (ShipClass.rotationDecel);
                return rotationDegrees + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((ShipClass.rotationDecel / TranscendenceRL.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
        }}
        [JsonIgnore]
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
        public BaseShip() { }
        public BaseShip(World world, ShipClass shipClass, Sovereign Sovereign, XY Position) {
            this.World = world;
            this.ShipClass = shipClass;
            
            this.Sovereign = Sovereign;
            
            this.Position = Position;
            this.Velocity = new XY();
            
            this.Active = true;
            
            this.Items = new HashSet<Item>();
            this.Items.UnionWith(shipClass.items.Generate(world.types));

            this.Devices = new DeviceSystem();
            this.Devices.Install(shipClass.devices.Generate(world.types));

            this.DamageSystem = shipClass.damageDesc.Create(this);
            this.destiny = new Rand(world.karma.NextInteger());
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
            wreck.Items.UnionWith(
                Devices.Installed.Select(
                    d => d.source).Where(
                    i => i != null).Select(
                    i => {
                        i.RemoveArmor();
                        return i;
                    }
                )
            );
            World.AddEntity(wreck);

            foreach(var on in OnDestroyed.set) {
                on.Value.Invoke(this, source, wreck);
            }
            Active = false;
        }
        public void Update() {
            UpdateControls();
            UpdateMotion();
            //Devices.Update(this);
        }
        public void UpdateControls() {
            if(ControlHijack != null) {
                switch(ControlHijack.thrustMode) {
                    case HijackMode.FORCE_ON:
                        thrusting = true;
                        break;
                    case HijackMode.FORCE_OFF:
                        thrusting = false;
                        break;
                }
                switch (ControlHijack.turnMode) {
                    case HijackMode.FORCE_ON:
                        rotating = Rotating.CCW;
                        break;
                    case HijackMode.FORCE_OFF:
                        rotating = Rotating.None;
                        break;
                }
                switch(ControlHijack.brakeMode) {
                    case HijackMode.FORCE_ON:
                        decelerating = true;
                        break;
                    case HijackMode.FORCE_OFF:
                        decelerating = false;
                        break;
                }
                switch (ControlHijack.fireMode) {
                    case HijackMode.FORCE_ON:
                        foreach (var w in Devices.Weapons) {
                            w.firing = true;
                        }
                        break;
                    case HijackMode.FORCE_OFF:
                        foreach (var w in Devices.Weapons) {
                            w.firing = false;
                        }
                        break;
                }
                ControlHijack.Update();
                if(ControlHijack.active == false) {
                    ControlHijack = null;
                }
            }
            UpdateThrust();
            UpdateTurn();
            rotationDegrees += rotatingVel;
            UpdateBrake();
            void UpdateThrust() {
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
            }
            void UpdateTurn() {
                if (rotating != Rotating.None) {
                    if (rotating == Rotating.CCW) {
                        /*
                        if (rotatingSpeed < 0) {
                            rotatingSpeed += Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                        }
                        */
                        //Add decel if we're turning the other way
                        if (rotatingVel < 0) {
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
            }
            void Decel() => rotatingVel -= Math.Min(Math.Abs(rotatingVel), ShipClass.rotationDecel / TranscendenceRL.TICKS_PER_SECOND) * Math.Sign(rotatingVel); ;
            void UpdateBrake() {
                if (decelerating) {
                    if (Velocity.Magnitude > 0.05) {
                        Velocity -= Velocity.Normal * Math.Min(Velocity.Magnitude, ShipClass.thrust / 2);
                    } else {
                        Velocity = new XY();
                    }
                    decelerating = false;
                }
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

        [JsonIgnore]
        public string Name => Ship.Name;
        [JsonIgnore]
        public World World => Ship.World;
        [JsonIgnore] 
        public ShipClass ShipClass => Ship.ShipClass;
        [JsonIgnore] 
        public Sovereign Sovereign => Ship.Sovereign;
        [JsonIgnore] 
        public XY Position { get => Ship.Position; set => Ship.Position = value; }
        [JsonIgnore] 
        public XY Velocity { get => Ship.Velocity; set => Ship.Velocity = value; }
        [JsonIgnore] 
        public double rotationDegrees => Ship.rotationDegrees;
        [JsonIgnore]
        public HashSet<Item> Items => Ship.Items;
        [JsonIgnore] 
        public DeviceSystem Devices => Ship.Devices;

        [JsonIgnore] 
        public DamageSystem DamageSystem => Ship.DamageSystem;

        public BaseShip Ship;
        public IOrder controller;
        public Docking Dock { get; set; }
        [JsonIgnore] 
        public Rand destiny => Ship.destiny;
        [JsonIgnore] 
        public double stoppingRotation => Ship.stoppingRotation;

        [JsonIgnore]
        public HashSet<SpaceObject> avoidHit => new HashSet<SpaceObject> {
            Dock?.target, (controller as GuardOrder)?.guardTarget
        };

        public AIShip(BaseShip ship, IOrder controller) {
            this.Ship = ship;
            this.controller = controller;
        }
        public void SetThrusting(bool thrusting = true) => Ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => Ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => Ship.SetDecelerating(decelerating);
        public void Damage(SpaceObject source, int hp) => Ship.DamageSystem.Damage(this, source, hp);
        public void Destroy(SpaceObject source) {
            if (source is PlayerShip ps) {
                ps.ShipsDestroyed.Add(this);
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
        [JsonIgnore] 
        public bool Active => Ship.Active;
        [JsonIgnore] 
        public ColoredGlyph Tile => Ship.Tile;
    }

    public struct BaseOnDestroyed : IContainer<Destroyed> {
        public PlayerShip player;
        public BaseOnDestroyed(PlayerShip player) {
            this.player = player;
        }
        [JsonIgnore]
        public Destroyed Value { get {
                var self = this;
                return (BaseShip s, SpaceObject source, Wreck wreck) => {
                    foreach (var f in self.player.OnDestroyed.set) f.Value.Invoke(self.player, source, wreck);
                };
        } }
        public override bool Equals(object obj) => obj is BaseOnDestroyed b && b.player == player;
    }
    public class PlayerShip : IShip {
        public Player player;
        [JsonIgnore] 
        public string Name => Ship.Name;
        [JsonIgnore] 
        public World World => Ship.World;
        [JsonIgnore] 
        public ShipClass ShipClass => Ship.ShipClass;
        [JsonIgnore] 
        public Sovereign Sovereign => Ship.Sovereign;
        [JsonIgnore] 
        public XY Position { get => Ship.Position; set => Ship.Position = value; }
        [JsonIgnore] 
        public XY Velocity { get => Ship.Velocity; set => Ship.Velocity = value; }
        [JsonIgnore] 
        public double rotationDegrees => Ship.rotationDegrees;
        [JsonIgnore] 
        public double stoppingRotation => Ship.stoppingRotation;
        [JsonIgnore] 
        public HashSet<Item> Items => Ship.Items;

        public int targetIndex = -1;
        public bool targetFriends = false;
        public List<SpaceObject> targetList = new List<SpaceObject>();

        public bool firingPrimary = false;
        public int selectedPrimary = 0;

        public int mortalChances = 3;
        public double mortalTime = 0;

        [JsonIgnore] 
        public DeviceSystem Devices => Ship.Devices;
        public BaseShip Ship;
        public EnergySystem Energy;
        public List<Power> Powers;
        public Docking Dock { get; set; }

        public bool autopilot;

        public List<IPlayerMessage> Messages = new List<IPlayerMessage>();

        public HashSet<Entity> Visible = new HashSet<Entity>();
        public HashSet<Station> Known = new HashSet<Station>();
        int ticks = 0;

        public HashSet<IShip> ShipsDestroyed = new HashSet<IShip>();

        public delegate void PlayerDestroyed(PlayerShip playerShip, SpaceObject destroyer, Wreck wreck);
        public FuncSet<IContainer<PlayerDestroyed>> OnDestroyed = new FuncSet<IContainer<PlayerDestroyed>>();
        public delegate void PlayerDamaged(PlayerShip playerShip, SpaceObject damager, int hp);
        public FuncSet<IContainer<PlayerDamaged>> OnDamaged = new FuncSet<IContainer<PlayerDamaged>>();

        public PlayerShip(Player player, BaseShip ship) {
            this.player = player;
            this.Ship = ship;

            Energy = new EnergySystem(ship.Devices);
            Powers = new List<Power>();

            //Remember to create the Heading when you add or replace this ship in the World


            //Hook up our own event to the ship since calling Damage can call base ship's Destroy without calling our own Destroy()
            ship.OnDestroyed += new BaseOnDestroyed(this);
        }
        public void Detach() {
            Ship.OnDestroyed -= new BaseOnDestroyed(this);
            //Ship.OnDestroyed.set.RemoveWhere(s => s is BaseOnDestroyed b && b.player == this);
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
                targetList = 
                    World.entities.all
                    .OfType<SpaceObject>()
                    .Where(e => this.IsEnemy(e))
                    .OrderBy(e => (e.Position - Position).Magnitude)
                    .Select(s => s is Segment seg ? seg.Parent : s)
                    .Distinct()
                    .ToList();
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
                targetList = World.entities.all
                    .OfType<SpaceObject>()
                    .Where(e => this.IsFriendly(e))
                    .OrderBy(e => (e.Position - Position).Magnitude)
                    .Select(s => s is Segment seg ? seg.Parent : s)
                    .Distinct()
                    .ToList();
                canRefresh = false;
            }
        }
        //Remember to call this before we set the targetIndex == -1
        public void ResetAutoAim() {
            var target = targetList[targetIndex];
            if (selectedPrimary < Ship.Devices.Weapons.Count) {
                var primary = Ship.Devices.Weapons[selectedPrimary];
                if(primary.target == target) {
                    primary.OverrideTarget(null);
                }
            }
        }
        //Remember to call this after we set the targetIndex > -1
        public void UpdateAutoAim() {
            var target = targetList[targetIndex];
            if (selectedPrimary < Ship.Devices.Weapons.Count) {
                var primary = Ship.Devices.Weapons[selectedPrimary];
                primary.OverrideTarget(target);
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
                        AddMessage(new InfoMessage("Escape while you can!"));

                        mortalTime = mortalChances * 3.0 + 1;
                        mortalChances--;
                    }
                }
            }

            foreach(var f in OnDamaged.set) f.Value.Invoke(this, source, hp);
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
            foreach (var enabled in Ship.Devices.Installed.Except(Energy.disabled)) {
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
        [JsonIgnore]
        public bool Active => Ship.Active;
        [JsonIgnore]
        public ColoredGlyph Tile => Ship.Tile;
    }
}
