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
                && (owner.sovereign.IsEnemy(target)
                || target.sovereign.IsEnemy(owner.sovereign)))
                && !(target is Wreck);
        }
    }
    public static class SShip {
        public static bool IsEnemy(this IShip owner, SpaceObject target) {
            return owner.CanTarget(target) && (owner.sovereign.IsEnemy(target) || target.sovereign.IsEnemy(owner.sovereign)) && !(target is Wreck);
        }
        public static bool IsFriendly(this IShip owner, SpaceObject target) {
            return owner.CanTarget(target) && owner.sovereign.IsFriend(target) && !(target is Wreck);
        }
        public static bool CanTarget(this IShip owner, SpaceObject target) {
            return owner != target;
        }
    }
    public interface IShip : SpaceObject {
        XY position { get; set; }
        XY velocity { get; set; }
        HashSet<Item> cargo { get; }
        DeviceSystem devices { get; }
        ShipClass shipClass { get; }
        double rotationDeg { get; }
        public double stoppingRotation { get; }
        Docking dock { get; set; }
    }
    public class BaseShip : SpaceObject {
        [JsonIgnore]
        public static BaseShip dead => new BaseShip(World.empty, ShipClass.empty, Sovereign.Gladiator, XY.Zero) { active = false };
        [JsonIgnore]
        public string name => shipClass.name;
        [JsonProperty]
        public World world { get; private set; }
        [JsonProperty]
        public ShipClass shipClass { get; private set; }
        [JsonProperty]
        public Sovereign sovereign { get; private set; }
        public XY position { get; set; }
        public XY velocity { get; set; }
        public bool active { get; set; }
        [JsonProperty]
        public HashSet<Item> cargo { get; private set; }
        [JsonProperty]
        public DeviceSystem devices { get; private set; }
        [JsonProperty]
        public HullSystem damageSystem { get; private set; }
        public Disrupt controlHijack;

        public Rand destiny;

        public delegate void Destroyed(BaseShip ship, SpaceObject destroyer, Wreck wreck);
        public FuncSet<IContainer<Destroyed>> onDestroyed = new FuncSet<IContainer<Destroyed>>();

        public double rotationDeg { get; set; }
        [JsonIgnore]
        public double stoppingRotation { get {
                var stoppingTime = Program.TICKS_PER_SECOND * Math.Abs(rotatingVel) / (shipClass.rotationDecel);
                return rotationDeg + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((shipClass.rotationDecel / Program.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
        }}
        [JsonIgnore]
        public double stoppingRotationWithCounterTurn {
            get {
                var stoppingRate = shipClass.rotationDecel + shipClass.rotationAccel;
                var stoppingTime = Math.Abs(Program.TICKS_PER_SECOND * rotatingVel / stoppingRate);
                return rotationDeg + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((stoppingRate / Program.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
            }
        }

        public bool thrusting;
        public Rotating rotating;
        public double rotatingVel;
        public bool decelerating;
        public BaseShip() { }
        public BaseShip(World world, ShipClass shipClass, Sovereign Sovereign, XY Position) {
            this.world = world;
            this.shipClass = shipClass;
            
            this.sovereign = Sovereign;
            
            this.position = Position;
            this.velocity = new XY();
            
            this.active = true;
            
            this.cargo = new HashSet<Item>();
            this.cargo.UnionWith(shipClass.cargo?.Generate(world.types) ?? new List<Item>());
            this.devices = new DeviceSystem();
            this.devices.Install(shipClass.devices?.Generate(world.types) ?? new List<Device>());

            this.damageSystem = shipClass.damageDesc.Create(this);
            this.destiny = new Rand(world.karma.NextInteger());
        }
        public BaseShip(BaseShip source) {
            this.world = source.world;
            this.shipClass = source.shipClass;
            this.sovereign = source.sovereign;
            this.position = source.position;
            this.velocity = source.velocity;
            this.active = true;
            this.cargo = source.cargo;
            this.devices = source.devices;
            this.damageSystem = source.damageSystem;
            this.destiny = source.destiny;
        }
        public void SetThrusting(bool thrusting = true) => this.thrusting = thrusting;
        public void SetRotating(Rotating rotating = Rotating.None) {
            this.rotating = rotating;
        }
        public void SetDecelerating(bool decelerating = true) => this.decelerating = decelerating;
        public void Damage(SpaceObject source, int hp) => damageSystem.Damage(this, source, hp);
        public void Destroy(SpaceObject source) {
            var items = cargo.Concat(devices.Installed.Select(d => d.source)
                .Where(i => i != null).Select(i => {
                        i.RemoveArmor();
                        return i;
                    }
                ));
            var wreck = new Wreck(this, items);
            world.AddEntity(wreck);
            active = false;

            onDestroyed.set.RemoveWhere(d => d.Value == null);
            foreach(var on in onDestroyed.set) {
                on.Value.Invoke(this, source, wreck);
            }
        }
        public void Update() {
            UpdateControls();
            UpdateMotion();
            //Devices.Update(this);
        }
        public void UpdateControls() {
            if(controlHijack != null) {
                switch(controlHijack.thrustMode) {
                    case DisruptMode.FORCE_ON:
                        thrusting = true;
                        break;
                    case DisruptMode.FORCE_OFF:
                        thrusting = false;
                        break;
                }
                switch (controlHijack.turnMode) {
                    case DisruptMode.FORCE_ON:
                        rotating = Rotating.CCW;
                        break;
                    case DisruptMode.FORCE_OFF:
                        rotating = Rotating.None;
                        break;
                }
                switch(controlHijack.brakeMode) {
                    case DisruptMode.FORCE_ON:
                        decelerating = true;
                        break;
                    case DisruptMode.FORCE_OFF:
                        decelerating = false;
                        break;
                }
                switch (controlHijack.fireMode) {
                    case DisruptMode.FORCE_ON:
                        foreach (var w in devices.Weapons) {
                            w.firing = true;
                        }
                        break;
                    case DisruptMode.FORCE_OFF:
                        foreach (var w in devices.Weapons) {
                            w.firing = false;
                        }
                        break;
                }
                controlHijack.Update();
                if(controlHijack.active == false) {
                    controlHijack = null;
                }
            }
            UpdateThrust();
            UpdateTurn();
            rotationDeg += rotatingVel;
            UpdateBrake();
            void UpdateThrust() {
                if (thrusting) {
                    var rotationRads = rotationDeg * Math.PI / 180;

                    var exhaust = new EffectParticle(position + XY.Polar(rotationRads, -1),
                        velocity + XY.Polar(rotationRads, -shipClass.thrust),
                        new ColoredGlyph(Color.Yellow, Color.Transparent, '.'),
                        4);
                    world.AddEffect(exhaust);

                    velocity += XY.Polar(rotationRads, shipClass.thrust);
                    if (velocity.magnitude > shipClass.maxSpeed) {
                        velocity = velocity.normal * shipClass.maxSpeed;
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
                        rotatingVel += shipClass.rotationAccel / Program.TICKS_PER_SECOND;
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
                        rotatingVel -= shipClass.rotationAccel / Program.TICKS_PER_SECOND;
                    }
                    rotatingVel = Math.Min(Math.Abs(rotatingVel), shipClass.rotationMaxSpeed) * Math.Sign(rotatingVel);
                    rotating = Rotating.None;
                } else {
                    Decel();
                }
            }
            void Decel() => rotatingVel -= Math.Min(Math.Abs(rotatingVel), shipClass.rotationDecel / Program.TICKS_PER_SECOND) * Math.Sign(rotatingVel); ;
            void UpdateBrake() {
                if (decelerating) {
                    if (velocity.magnitude > 0.05) {
                        velocity -= velocity.normal * Math.Min(velocity.magnitude, shipClass.thrust / 2);
                    } else {
                        velocity = new XY();
                    }
                    decelerating = false;
                }
            }
        }
        public void UpdateMotion() {
            position += velocity / Program.TICKS_PER_SECOND;
        }
        public ColoredGlyph tile => shipClass.tile.Original;
    }
    public interface ShipBehavior {
        void Update(IShip owner);
    }
    public class Sulphin : ShipBehavior {
        int ticks = 0;
        HashSet<PlayerShip> playersMet = new HashSet<PlayerShip>();
        public void Update(IShip owner) {
            ticks++;
            if (ticks % 150 == 0) {
                var players = owner.world.entities.all
                    .OfType<PlayerShip>()
                    .Where(p => (p.position - owner.position).magnitude < 80)
                    .Where(p => !playersMet.Contains(p));
                foreach(var p in players) {
                    p.AddMessage(new Transmission(owner, "Kack! What the hell are you doing here??"));
                }
                playersMet.UnionWith(players);

            }
        }
    }


    public class AIShip : IShip {
        public static int ID = 0;
        public int Id = ID++;

        [JsonIgnore]
        public string name => ship.name;
        [JsonIgnore]
        public World world => ship.world;
        [JsonIgnore] 
        public ShipClass shipClass => ship.shipClass;
        [JsonIgnore] 
        public Sovereign sovereign => ship.sovereign;
        [JsonIgnore] 
        public XY position { get => ship.position; set => ship.position = value; }
        [JsonIgnore] 
        public XY velocity { get => ship.velocity; set => ship.velocity = value; }
        [JsonIgnore] 
        public double rotationDeg => ship.rotationDeg;
        [JsonIgnore]
        public HashSet<Item> cargo => ship.cargo;
        [JsonIgnore] 
        public DeviceSystem devices => ship.devices;

        [JsonIgnore] 
        public HullSystem damageSystem => ship.damageSystem;

        public ShipBehavior behavior;
        public BaseShip ship;
        public IOrder controller;
        public Docking dock { get; set; }
        [JsonIgnore] 
        public Rand destiny => ship.destiny;
        [JsonIgnore] 
        public double stoppingRotation => ship.stoppingRotation;

        [JsonIgnore]
        public HashSet<SpaceObject> avoidHit => new HashSet<SpaceObject> {
            dock?.Target, (controller as GuardOrder)?.GuardTarget
        };

        public AIShip(BaseShip ship, IOrder controller) {
            this.ship = ship;
            this.controller = controller;
            InitBehavior(ship.shipClass.behavior);
        }
        public void InitBehavior(ShipBehaviors b) {
            switch(b) {
                case ShipBehaviors.sulphin:
                    behavior = new Sulphin();
                    break;
            }
        }
        public void SetThrusting(bool thrusting = true) => ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => ship.SetDecelerating(decelerating);
        public void Damage(SpaceObject source, int hp) => ship.damageSystem.Damage(this, source, hp);
        public void Destroy(SpaceObject source) {
            if (source is PlayerShip ps) {
                ps.shipsDestroyed.Add(this);
            }
            ship.Destroy(source);
        }
        public void Update() {
            behavior?.Update(this);
            controller.Update(this);

            dock?.Update(this);

            ship.UpdateControls();
            ship.UpdateMotion();

            //We update the ship's devices as ourselves because they need to know who the exact owner is
            //In case someone other than us needs to know who we are through our devices
            ship.devices.Update(this);
        }
        [JsonIgnore] 
        public bool active => ship.active;
        [JsonIgnore] 
        public ColoredGlyph tile => ship.tile;
    }

    public struct BaseOnDestroyed : IContainer<Destroyed> {
        public PlayerShip player;
        public BaseOnDestroyed(PlayerShip player) {
            this.player = player;
        }
        [JsonIgnore]
        public Destroyed Value { get {
                var player = this.player;
                if(player == null) {
                    return null;
                }
                return (BaseShip s, SpaceObject source, Wreck wreck) => {
                    player.onDestroyed.set.RemoveWhere(d => d.Value == null);
                    foreach (var f in player.onDestroyed.set) f.Value.Invoke(player, source, wreck);
                };
        } }
        public override bool Equals(object obj) => obj is BaseOnDestroyed b && b.player == player;
    }
    public class PlayerShip : IShip {
        public Player player;
        [JsonIgnore] 
        public string name => ship.name;
        [JsonIgnore] 
        public World world => ship.world;
        [JsonIgnore] 
        public ShipClass shipClass => ship.shipClass;
        [JsonIgnore] 
        public Sovereign sovereign => ship.sovereign;
        [JsonIgnore] 
        public XY position { get => ship.position; set => ship.position = value; }
        [JsonIgnore] 
        public XY velocity { get => ship.velocity; set => ship.velocity = value; }
        [JsonIgnore] 
        public double rotationDeg => ship.rotationDeg;
        [JsonIgnore]
        public double rotationRad => ship.rotationDeg * Math.PI / 180;
        [JsonIgnore] 
        public double stoppingRotation => ship.stoppingRotation;
        [JsonIgnore] 
        public HashSet<Item> cargo => ship.cargo;

        public int targetIndex = -1;
        public bool targetFriends = false;
        public List<SpaceObject> TargetList = new List<SpaceObject>();

        public bool firingPrimary = false;
        public int selectedPrimary = 0;

        public int mortalChances = 3;
        public double mortalTime = 0;

        [JsonIgnore] 
        public DeviceSystem devices => ship.devices;
        public HullSystem hull => ship.damageSystem;
        public BaseShip ship;
        public EnergySystem energy;
        public List<Power> powers;
        public Docking dock { get; set; }

        public bool autopilot;

        public List<IPlayerMessage> messages = new List<IPlayerMessage>();

        public HashSet<Entity> visible = new HashSet<Entity>();
        public HashSet<Station> known = new HashSet<Station>();
        int ticks = 0;

        public HashSet<IShip> shipsDestroyed = new HashSet<IShip>();

        public delegate void PlayerDestroyed(PlayerShip playerShip, SpaceObject destroyer, Wreck wreck);
        public FuncSet<IContainer<PlayerDestroyed>> onDestroyed = new FuncSet<IContainer<PlayerDestroyed>>();
        public delegate void PlayerDamaged(PlayerShip playerShip, SpaceObject damager, int hp);
        public FuncSet<IContainer<PlayerDamaged>> onDamaged = new FuncSet<IContainer<PlayerDamaged>>();

        public PlayerShip(Player player, BaseShip ship) {
            this.player = player;
            this.ship = ship;

            energy = new EnergySystem(ship.devices);
            powers = new List<Power>();

            //Remember to create the Heading when you add or replace this ship in the World


            //Hook up our own event to the ship since calling Damage can call base ship's Destroy without calling our own Destroy()
            ship.onDestroyed += new BaseOnDestroyed(this);
        }
        public void Detach() {
            ship.onDestroyed -= new BaseOnDestroyed(this);
            //Ship.OnDestroyed.set.RemoveWhere(s => s is BaseOnDestroyed b && b.player == this);
        }
        public void SetThrusting(bool thrusting = true) => ship.SetThrusting(thrusting);
        public void SetRotating(Rotating rotating = Rotating.None) => ship.SetRotating(rotating);
        public void SetDecelerating(bool decelerating = true) => ship.SetDecelerating(decelerating);
        public void SetFiringPrimary(bool firingPrimary = true) => this.firingPrimary = firingPrimary;
        public void SetRotatingToFace(double targetRads) {
            var facingRads = ship.stoppingRotationWithCounterTurn * Math.PI / 180;

            var ccw = (XY.Polar(facingRads + 3 * Math.PI / 180) - XY.Polar(targetRads)).magnitude;
            var cw = (XY.Polar(facingRads - 3 * Math.PI / 180) - XY.Polar(targetRads)).magnitude;
            if (ccw < cw) {
                SetRotating(Rotating.CCW);
            } else if (cw < ccw) {
                SetRotating(Rotating.CW);
            } else {
                if (ship.rotatingVel > 0) {
                    SetRotating(Rotating.CW);
                } else {
                    SetRotating(Rotating.CCW);
                }
            }
        }
        public void DisengageAutopilot() {
            if (autopilot) {
                autopilot = false;
                AddMessage(new InfoMessage($"Autopilot disengaged"));
            }
        }
        public Stargate CheckGate() {
            return world.entities[position]
                .Select(s => (s is Segment seg ? seg.parent : s))
                .OfType<Stargate>().FirstOrDefault();
        }
        public void NextWeapon() {
            selectedPrimary++;
            if(selectedPrimary >= ship.devices.Weapons.Count) {
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
            } else if(targetIndex >= TargetList.Count - 1) {
                Refresh();
            }

        CheckTarget:
            targetIndex++;
            if(targetIndex < TargetList.Count) {
                var target = TargetList[targetIndex];
                if(!this.IsEnemy(target)) {
                    goto CheckTarget;
                } else if (!target.active) {
                    goto CheckTarget;
                } else if((target.position - position).magnitude > 100) {
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
                TargetList = 
                    world.entities.all
                    .OfType<SpaceObject>()
                    .Where(e => this.IsEnemy(e))
                    .OrderBy(e => (e.position - position).magnitude)
                    .Select(s => s is Segment seg ? seg.parent : s)
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
            } else if (targetIndex >= TargetList.Count - 1) {
                Refresh();
            }

        CheckTarget:
            targetIndex++;
            if (targetIndex < TargetList.Count) {
                var target = TargetList[targetIndex];
                if (!this.IsFriendly(target)) {
                    goto CheckTarget;
                } else if (!target.active) {
                    goto CheckTarget;
                } else if ((target.position - position).magnitude > 100) {
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
                TargetList = world.entities.all
                    .OfType<SpaceObject>()
                    .Where(e => this.IsFriendly(e))
                    .OrderBy(e => (e.position - position).magnitude)
                    .Select(s => s is Segment seg ? seg.parent : s)
                    .Distinct()
                    .ToList();
                canRefresh = false;
            }
        }
        //Remember to call this before we set the targetIndex == -1
        public void ResetAutoAim() {
            var target = TargetList[targetIndex];
            if (selectedPrimary < ship.devices.Weapons.Count) {
                var primary = ship.devices.Weapons[selectedPrimary];
                if(primary.target == target) {
                    primary.OverrideTarget(null);
                }
            }
        }
        //Remember to call this after we set the targetIndex > -1
        public void UpdateAutoAim() {
            var target = TargetList[targetIndex];
            if (selectedPrimary < ship.devices.Weapons.Count) {
                var primary = ship.devices.Weapons[selectedPrimary];
                primary.OverrideTarget(target);
            }
        }
        //Stop targeting, but remember our remaining targets
        public void ForgetTarget() {
            ResetAutoAim();
            TargetList = TargetList.GetRange(targetIndex, TargetList.Count - targetIndex);
            targetIndex = -1;
        }
        //Stop targeting and clear our target list
        public void ClearTarget() {
            ResetAutoAim();
            TargetList.Clear();
            targetIndex = -1;
        }

        public void SetTargetList(List<SpaceObject> targetList) {
            if(targetIndex > -1) {
                ResetAutoAim();
            }
            this.TargetList = targetList;
            if(targetList.Count > 0) {
                targetIndex = 0;
                UpdateAutoAim();
            } else {
                targetIndex = -1;

            }
        }
        public bool GetTarget(out SpaceObject target) {
            if (targetIndex != -1) {
                target = TargetList[targetIndex];
                if (target.active) {
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
            if(selectedPrimary < ship.devices.Weapons.Count) {
                return ship.devices.Weapons[selectedPrimary];
            }
            return null;
        }
        public bool GetPrimary(out Weapon result) {
            if (selectedPrimary < ship.devices.Weapons.Count) {
                result = ship.devices.Weapons[selectedPrimary];
                return true;
            }
            result = null;
            return false;
        }
        public void Damage(SpaceObject source, int hp) {
            //Base ship can get destroyed without calling our own Destroy(), so we need to hook up an OnDestroyed event to this
            ship.Damage(source, hp);

            if(hp > ship.damageSystem.GetHP() / 3) {
                if(mortalTime <= 0) {
                    if(mortalChances > 0) {
                        AddMessage(new InfoMessage("Escape while you can!"));

                        mortalTime = mortalChances * 3.0 + 1;
                        mortalChances--;
                    }
                }
            }

            foreach(var f in onDamaged.set) f.Value.Invoke(this, source, hp);
        }
        public void Destroy(SpaceObject source) {
            ship.Destroy(source);
        }
        public void Update() {
            messages.ForEach(m => m.Update());
            messages.RemoveAll(m => !m.Active);

            if(GetTarget(out SpaceObject target)) {
                Heading.Crosshair(world, target.position);
            }

            powers.ForEach(p => {
                if (p.cooldownLeft > 0) {
                    p.cooldownLeft--;

                    if (p.cooldownLeft == 0) {
                        AddMessage(new InfoMessage($"[Power] {p.type.name} is ready"));
                    }
                }
            });

            if (firingPrimary && selectedPrimary < ship.devices.Weapons.Count) {
                if(!energy.disabled.Contains(ship.devices.Weapons[selectedPrimary])) {
                    ship.devices.Weapons[selectedPrimary].SetFiring(true, target);
                }
                firingPrimary = false;
            }

            ticks++;
            visible = new HashSet<Entity>(world.entities.GetAll(p => (position - p).maxCoord < 50));
            if (ticks%30 == 0) {
                foreach (var s in visible.OfType<Station>().Where(s => !known.Contains(s))) {
                    messages.Add(new Transmission(s, $"Discovered: {s.type.name}"));
                    known.Add(s);
                }
            }

            dock?.Update(this);

            ship.UpdateControls();
            ship.UpdateMotion();

            //We update the ship's devices as ourselves because they need to know who the exact owner is
            //In case someone other than us needs to know who we are through our devices
            foreach (var enabled in ship.devices.Installed.Except(energy.disabled)) {
                enabled.Update(this);
            }
            energy.Update(this);
        }
        public void AddMessage(IPlayerMessage message) {
            var existing = messages.FirstOrDefault(m => m.message.String.Equals(message.message.String));
            if (existing != null) {
                existing.Reset();
            } else {
                messages.Add(message);
            }
        }
        [JsonIgnore]
        public bool active => ship.active;
        [JsonIgnore]
        public ColoredGlyph tile => ship.tile;
    }
}
