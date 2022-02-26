using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using static RogueFrontier.BaseShip;
using Newtonsoft.Json;

namespace RogueFrontier;

public enum Rotating {
    None, CCW, CW
}

public static class SStation {
    public static bool IsEnemy(this Station owner, ActiveObject target) {
        return (owner != target
            && (owner.sovereign.IsEnemy(target)
            || target.sovereign.IsEnemy(owner.sovereign)))
            && !(target is Wreck);
    }
}
public static class SShip {
    public static bool IsEnemy(this IShip owner, ActiveObject target) {
        return owner.CanTarget(target) && (owner.sovereign.IsEnemy(target) || target.sovereign.IsEnemy(owner.sovereign)) && !(target is Wreck);
    }
    public static bool IsFriendly(this IShip owner, ActiveObject target) {
        return owner.CanTarget(target) && owner.sovereign.IsFriend(target) && !(target is Wreck);
    }
    public static bool CanTarget(this IShip owner, ActiveObject target) {
        return owner != target;
    }
}
public interface IShip : ActiveObject {
    XY position { get; set; }
    XY velocity { get; set; }
    HashSet<Item> cargo { get; }
    DeviceSystem devices { get; }
    ShipClass shipClass { get; }
    double rotationDeg { get; }
    double rotationRad => rotationDeg * Math.PI / 180;
    public double stoppingRotation { get; }
    Docking dock { get; set; }
}
public class BaseShip : StructureObject {
    [JsonIgnore]
    public static BaseShip dead => new(System.empty, ShipClass.empty, XY.Zero) { active = false };
    [JsonIgnore]
    public string name => shipClass.name;
    [JsonIgnore]
    public ColoredGlyph tile => shipClass.tile.Original;

    [JsonIgnore]
    public double stoppingRotation {
        get {
            var stoppingTime = Program.TICKS_PER_SECOND * Math.Abs(rotatingVel) / (shipClass.rotationDecel);
            return rotationDeg + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((shipClass.rotationDecel / Program.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
        }
    }
    [JsonIgnore]
    public double stoppingRotationWithCounterTurn {
        get {
            var stoppingRate = shipClass.rotationDecel + shipClass.rotationAccel;
            var stoppingTime = Math.Abs(Program.TICKS_PER_SECOND * rotatingVel / stoppingRate);
            return rotationDeg + (rotatingVel * stoppingTime) + Math.Sign(rotatingVel) * ((stoppingRate / Program.TICKS_PER_SECOND) * stoppingTime * stoppingTime) / 2;
        }
    }


    [JsonProperty]
    public System world { get; set; }
    [JsonProperty]
    public ShipClass shipClass { get; set; }
    [JsonProperty]
    public XY position { get; set; }
    [JsonProperty]
    public int id { get; set; }
    [JsonProperty]
    public XY velocity { get; set; }
    public bool active { get; set; }
    [JsonProperty]
    public HashSet<Item> cargo { get; private set; }
    [JsonProperty]
    public DeviceSystem devices { get; private set; }
    [JsonProperty]
    public HullSystem damageSystem { get; private set; }
    [JsonProperty]
    public Disrupt disruption;
    [JsonProperty]
    public Rand destiny;
    [JsonProperty]
    public double rotationDeg { get; set; }
    public bool thrusting;
    public Rotating rotating;
    public double rotatingVel;
    public bool decelerating;


    public delegate void Destroyed(BaseShip ship, ActiveObject destroyer, Wreck wreck);
    public FuncSet<IContainer<Destroyed>> onDestroyed = new();



    public BaseShip() { }
    public BaseShip(System world, ShipClass shipClass, XY Position) {
        this.world = world;
        this.id = world.nextId++;
        this.shipClass = shipClass;

        this.position = Position;
        this.velocity = new();

        this.active = true;

        this.cargo = new();
        this.cargo.UnionWith(shipClass.cargo?.Generate(world.types) ?? new List<Item>());
        this.devices = new();
        this.devices.Install(shipClass.devices?.Generate(world.types) ?? new List<Device>());
        this.damageSystem = shipClass.damageDesc.Create(world.types);
        this.destiny = new Rand(world.karma.NextInteger());
    }
    public BaseShip(BaseShip source) {
        this.world = source.world;
        this.shipClass = source.shipClass;
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
    
    public void ReduceDamage(Projectile p) {
        int dmgFull = p.damageHP;
        ref int dmgLeft = ref p.damageHP;
        foreach (var s in devices.Shield) {
            if (dmgLeft == 0) {
                break;
            }
            var d = Math.Min(s.maxAbsorb, (int)(dmgLeft * s.absorbFactor));
            if (d > 0) {
                s.Absorb(d);
                dmgLeft -= d;
            }
        }
        if (dmgLeft > 0) {
            int knockback = p.fragment.knockback * dmgLeft / dmgFull;
            velocity += (p.velocity - velocity).WithMagnitude(knockback);
            disruption = p.fragment.disruptor?.GetHijack() ?? disruption;
        }
    }
    public void Damage(Projectile p) {
        ReduceDamage(p);
        damageSystem.Damage(world.tick, p, Destroy);
    }
    public void Destroy(ActiveObject source) {
        var items = cargo.Concat(
            devices.Installed
            .Select(d => d.source)
            .Where(i => i != null)
        );
        var wreck = new Wreck(this, items);
        world.AddEntity(wreck);
        foreach(var angle in Enumerable.Range(0, 16).Select(i => i * 2 * Math.PI / 16)) {
            var blast = new EffectParticle(position + XY.Polar(angle, 1),
                velocity + XY.Polar(angle, 4),
                new ColoredGlyph(Color.Orange, Color.Orange.SetAlpha(128), 'x'),
                60);
            world.AddEffect(blast);

        }

        active = false;

        onDestroyed.set.RemoveWhere(d => d.Value == null);
        foreach (var on in onDestroyed) {
            on.Value.Invoke(this, source, wreck);
        }
    }
    public void Update() {
        UpdateControls();
        UpdateMotion();
        //Devices.Update(this);
    }
    public void UpdateControls() {
        if (disruption != null) {
            thrusting = disruption.thrustMode switch {
                DisruptMode.FORCE_ON => true,
                DisruptMode.FORCE_OFF => false,
                _ => thrusting
            };
            rotating = disruption.turnMode switch {
                DisruptMode.FORCE_ON => Rotating.CCW,
                DisruptMode.FORCE_OFF => Rotating.None,
                _ => rotating
            };

            decelerating = disruption.brakeMode switch {
                DisruptMode.FORCE_ON => true,
                DisruptMode.FORCE_OFF => false,
                _ => decelerating
            };
            devices.Weapon.ForEach(a => a.firing = disruption.fireMode switch {
                DisruptMode.FORCE_ON => true,
                DisruptMode.FORCE_OFF => false,
                _ => a.firing
            });
            disruption.Update();
            if (disruption.active == false) {
                disruption = null;
            }
        }
        UpdateThrust();
        UpdateTurn();
        UpdateRotation();
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
            void Decel() => rotatingVel -= Math.Min(Math.Abs(rotatingVel), shipClass.rotationDecel / Program.TICKS_PER_SECOND) * Math.Sign(rotatingVel); ;
        }
        void UpdateRotation() => rotationDeg += rotatingVel;
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
}
public static class SShipBehavior {
    public static bool CanTarget(this IShipBehavior behavior, ActiveObject other) {
        switch (behavior) {
            case Wingmate w:
                return w.order.CanTarget(other);
            case IShipOrder o:
                return o.CanTarget(other);
        }
        return false;
    }
    public static IShipOrder GetOrder(this IShipBehavior behavior) {
        switch (behavior) {
            case Wingmate w:
                return w.order;
            case IShipOrder o:
                return o;
            case Sulphin s:
                return s.order;
            default:
                throw new Exception("Unknown behavior type");
        }
    }
    public static string GetOrderName(this IShipBehavior behavior) =>
        behavior.GetOrder()?.GetType().Name ?? "Unknown";
}
public class AIShip : IShip {
    [JsonIgnore]
    public int id => ship.id;
    [JsonIgnore]
    public string name => ship.name;
    [JsonIgnore]
    public System world => ship.world;
    [JsonIgnore]
    public ShipClass shipClass => ship.shipClass;
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
    [JsonIgnore]
    public Rand destiny => ship.destiny;
    [JsonIgnore]
    public double stoppingRotation => ship.stoppingRotation;
    [JsonIgnore]
    public HashSet<Entity> avoidHit => new HashSet<Entity> {
        dock?.Target,
        (behavior as GuardOrder)?.home
    };
    public bool dockable => false;
    public Docking dock { get; set; }

    public delegate void Destroyed(AIShip ship, ActiveObject destroyer, Wreck wreck);
    public FuncSet<IContainer<Destroyed>> onDestroyed = new();

    public BaseShip ship;
    public Sovereign sovereign { get; set; }
    //IShipBehavior and IShipOrder have the same interface but different purpose
    //The behavior sets the current order and does not affect ship controls
    public IShipBehavior behavior;
    public AIShip() { }
    public AIShip(BaseShip ship, Sovereign sovereign, IShipBehavior behavior = null, IShipOrder order = null) {
        this.ship = ship;
        this.sovereign = sovereign;
        this.behavior = behavior ?? ship.shipClass.behavior switch {
            EShipBehavior.sulphin => new Sulphin(order),
            EShipBehavior.none => order,
            _ => order
        };
    }
    public override string ToString() => $"{id}, {position.roundDown}, {velocity.roundDown}, {shipClass.codename}, {behavior}";
    public void SetThrusting(bool thrusting = true) => ship.SetThrusting(thrusting);
    public void SetRotating(Rotating rotating = Rotating.None) => ship.SetRotating(rotating);
    public void SetDecelerating(bool decelerating = true) => ship.SetDecelerating(decelerating);
    public void Damage(Projectile p) => ship.damageSystem.Damage(world.tick, p, Destroy);
    public void Destroy(ActiveObject source) {
        if (source is PlayerShip ps) {
            ps.shipsDestroyed.Add(this);
            if (shipClass.crimeOnDestroy) {
                ps.crimeRecord.Add(new DestructionCrime(this));
            }
        }
        ship.Destroy(source);
    }
    public void Update() {
        behavior?.Update(this);

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

public class PlayerShip : IShip {
    [JsonIgnore]
    public string name => ship.name;
    [JsonIgnore]
    public int id => ship.id;
    [JsonIgnore]
    public System world => ship.world;
    [JsonIgnore]
    public ShipClass shipClass => ship.shipClass;
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
    [JsonIgnore]
    public DeviceSystem devices => ship.devices;
    [JsonIgnore]
    public HullSystem hull => ship.damageSystem;

    public Player player;
    public BaseShip ship;
    public Sovereign sovereign { get; set; }
    public EnergySystem energy;
    public List<Power> powers = new();

    [JsonIgnore]
    public HashSet<Entity> avoidHit => new() {
        dock?.Target
    };
    public Docking dock { get; set; }

    public int targetIndex = -1;
    public bool targetFriends = false;
    public List<ActiveObject> targetList = new();

    public bool firingPrimary = false;
    public bool firingSecondary = false;

    public ListIndex<Weapon> primary;
    public ListIndex<Weapon> secondary;

    public int mortalChances = 3;
    public double mortalTime = 0;

    public bool autopilot = false;

    public List<IPlayerMessage> logs = new();
    public List<IPlayerMessage> messages = new();
    public HashSet<Entity> visible = new();
    public HashSet<Station> known = new();
    public HashSet<ActiveObject> missionTargets = new();
    private int ticks = 0;
    public HashSet<IShip> shipsDestroyed = new();
    public HashSet<Station> stationsDestroyed = new();

    public List<ICrime> crimeRecord=new();

    public delegate void Destroyed(PlayerShip playerShip, ActiveObject destroyer, Wreck wreck);
    public FuncSet<IContainer<Destroyed>> onDestroyed = new();
    public delegate void Damaged(PlayerShip playerShip, Projectile p);
    public FuncSet<IContainer<Damaged>> onDamaged = new();

    public delegate void WeaponFired(PlayerShip playerShip, Weapon w, List<Projectile> p);
    public FuncSet<IContainer<WeaponFired>> onWeaponFire = new();


    public List<AIShip> wingmates = new();
    public PlayerShip() { }
    public PlayerShip(Player player, BaseShip ship, Sovereign sovereign) {
        this.player = player;
        this.ship = ship;
        this.sovereign = sovereign;

        energy = new EnergySystem(ship.devices);
        primary = new(ship.devices.Weapon);
        secondary = new(ship.devices.Weapon);
    }
    public bool CanSee(Entity e) {
        return e switch {

            _ => true
        };
    }

    public void FireOnDestroyed(BaseShip s, ActiveObject source, Wreck wreck) {
        onDestroyed.set.RemoveWhere(d => d.Value == null);
        foreach (var f in onDestroyed.set) f.Value.Invoke(this, source, wreck);
    }
    public void SetThrusting(bool thrusting = true) => ship.SetThrusting(thrusting);
    public void SetRotating(Rotating rotating = Rotating.None) => ship.SetRotating(rotating);
    public void SetDecelerating(bool decelerating = true) => ship.SetDecelerating(decelerating);
    public void SetFiringPrimary(bool firingPrimary = true) => this.firingPrimary = firingPrimary;
    public void SetFiringSecondary(bool firingSecondary = true) => this.firingSecondary = firingSecondary;
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
            AddMessage(new Message($"Autopilot disengaged"));
        }
    }
    public bool CheckGate(out Stargate gate) {
        foreach (var s in world.effects[position]) {
            if ((s is ISegment seg ? seg.parent : s) is Stargate g) {
                gate = g;
                return true;
            }
        }
        gate = null;
        return false;
    }
    public void NextPrimary() => primary.index++;
    public void PrevPrimary() => primary.index--;
    public void NextSecondary() => secondary.index++;
    public void PrevSecondary() => secondary.index--;
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

        if (targetFriends) {
            Refresh();
            targetFriends = false;
        } else if (targetIndex >= targetList.Count - 1) {
            Refresh();
        }

    CheckTarget:
        targetIndex++;
        if (targetIndex < targetList.Count) {
            var target = targetList[targetIndex];
            if (target is ActiveObject a && !this.IsEnemy(a)) {
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
            targetList =
                world.entities.all
                .OfType<ActiveObject>()
                .Where(e => this.IsEnemy(e))
                .OrderBy(e => (e.position - position).magnitude)
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
            targetList = world.entities.all
                .OfType<ActiveObject>()
                .Where(e => this.IsFriendly(e))
                .OrderBy(e => (e.position - position).magnitude)
                .Distinct()
                .ToList();
            canRefresh = false;
        }
    }
    //Remember to call this before we set the targetIndex == -1
    public void ResetAutoAim() {
        var primary = GetPrimary();
        if(primary?.target == targetList[targetIndex]) {
            primary.OverrideTarget(null);
        }
    }
    //Remember to call this after we set the targetIndex > -1
    public void UpdateAutoAim() {
        GetPrimary()?.OverrideTarget(targetList[targetIndex]);
    }
    //Stop targeting, but remember our remaining targets
    public void ForgetTarget() {
        if (targetIndex == -1) {
            return;
        }
        ResetAutoAim();
        targetList = targetList.GetRange(targetIndex, targetList.Count - targetIndex);
        targetIndex = -1;
    }
    //Stop targeting and clear our target list
    public void ClearTarget() {
        if (targetIndex == -1) {
            return;
        }
        ResetAutoAim();
        targetList.Clear();
        targetIndex = -1;
    }

    public void SetTargetList(List<ActiveObject> targetList) {
        if (targetIndex > -1) {
            ResetAutoAim();
        }
        this.targetList = targetList;
        if (targetList.Count > 0) {
            targetIndex = 0;
            UpdateAutoAim();
        } else {
            targetIndex = -1;

        }
    }
    public bool GetTarget(out ActiveObject target) => (target = GetTarget()) != null;
    public ActiveObject GetTarget() {
        if (targetIndex != -1) {
            var target = targetList[targetIndex];
            if (target.active) {
                return target;
            }
            ForgetTarget();
        }
        return null;
    }
    public Weapon GetPrimary() => primary.item;
    public bool GetPrimary(out Weapon result) => (result = GetPrimary()) != null;
    public Weapon GetSecondary() => secondary.item;
    public bool GetSecondary(out Weapon result) => (result = GetSecondary()) != null;
    public void Damage(Projectile p) {
        int originalHP = ship.damageSystem.GetHP();
        
        //We handle our own damage system
        ship.ReduceDamage(p);
        ship.damageSystem.Damage(world.tick, p, DestroyCheck);

        //Check for saving throws
        void DestroyCheck(ActiveObject o) {
            powers.ForEach(power => power.OnDestroyCheck(this, p));
            ship.ReduceDamage(p);
            ship.damageSystem.Damage(world.tick, p, Destroy);
        }

        if (!active) {
            return;
        }
        int delta = originalHP - ship.damageSystem.GetHP();
        if (delta > ship.damageSystem.GetHP() / 3) {
            if (mortalTime <= 0) {
                if (mortalChances > 0) {
                    AddMessage(new Message("Escape while you can!"));

                    mortalTime = mortalChances * 3.0 + 1;
                    mortalChances--;
                }
            }
        }
        foreach (var f in onDamaged.set) f.Value.Invoke(this, p);
    }
    public void Destroy(ActiveObject source) {
        ship.Destroy(source);
    }
    public void Update() {
        messages.ForEach(m => m.Update());
        messages.RemoveAll(m => !m.Active);

        if (GetTarget(out ActiveObject target)) {
            Heading.Crosshair(world, target.position);
        }

        powers.ForEach(p => {
            if (p.cooldownLeft > 0) {
                p.cooldownLeft--;

                if (p.cooldownLeft == 0) {
                    AddMessage(new Message($"[Power] {p.type.name} is ready"));
                }
            }
        });

        if (firingPrimary && primary.Has(out var w)) {
            if (!energy.off.Contains(w))
                w.SetFiring(true, target);
            firingPrimary = false;
        }

        if (firingSecondary && secondary.Has(out w)) {
            if (!energy.off.Contains(w))
                w.SetFiring(true, target);
            firingSecondary = false;
        }

        ticks++;
        if (ticks % 60 == 0) {
            visible = new HashSet<Entity>(world.entities.GetAll(p => (position - p).maxCoord < 50));
            foreach (var s in visible.OfType<Station>().Where(s => !known.Contains(s))) {
                AddMessage(new Transmission(s, $"Discovered: {s.type.name}"));
                known.Add(s);
            }
        }

        dock?.Update(this);

        ship.UpdateControls();
        ship.UpdateMotion();

        //We update the ship's devices as ourselves because they need to know who the exact owner is
        //In case someone other than us needs to know who we are through our devices
        foreach (var enabled in ship.devices.Installed.Except(energy.off)) {
            enabled.Update(this);
        }
        energy.Update(this);
    }
    public void AddMessage(IPlayerMessage message) {
        var existing = messages.FirstOrDefault(m => m.Equals(message));
        if (existing != null) {
            existing.Reset();
        } else {
            messages.Add(message);
            logs.Add(message);
        }
    }
    [JsonIgnore]
    public bool active => ship.active;
    [JsonIgnore]
    public ColoredGlyph tile => ship.tile;

    public string GetMemorial(string epitaph) =>
@$"
{player.name} ({player.Genome.subjective}/{player.Genome.objective})
{player.Genome.name}

{epitaph}

Ship: {shipClass.name}

Armor
{string.Join('\n', (hull as LayeredArmorSystem).layers.Select(l => $"    {l.source.type.name}"))}

Devices
{string.Join('\n', devices.Installed.Select(device => $"    {device.source.type.name}"))}

Cargo
{string.Join('\n', cargo.GroupBy(i => i.type.name).Select(group => $"{group.Count(),4}x {group.Key}"))}

Ships Destroyed
{string.Join('\n', shipsDestroyed.GroupBy(sc => sc.shipClass).Select(pair => $"{pair.Count(),4}x {pair.Key.name,-16}"))}
";
}
