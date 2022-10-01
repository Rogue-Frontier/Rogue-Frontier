using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using Helper = Common.Main;
using SadRogue.Primitives;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace RogueFrontier;
public class Item {
    public string name => type.name;
    public ItemType type;

    //These fields are to remain null while the item is not installed and to be populated upon installation
    
    public Armor armor;
    public Engine engine;
    public Reactor reactor;
    public Service service;
    public Shield shield;
    public Solar solar;
    public Weapon weapon;

    public Modifier mod;

    public bool HasAtt(string att) => type.attributes.Contains(att);
    public Item() { }
    public Item(Item copy) {
        type = copy.type;
        weapon = copy.weapon?.Copy(this);
        armor = copy.armor?.Copy(this);
        shield = copy.shield?.Copy(this);
        reactor = copy.reactor?.Copy(this);
        solar = copy.solar?.Copy(this);
        service = copy.service?.Copy(this);
        mod = copy.mod is Modifier m ? (m with { }) : null;
    }
    public Item(ItemType type, Modifier mod = null) {
        this.type = type;
        this.mod = mod;

        weapon = type.weapon?.GetWeapon(this);
        armor = type.armor?.GetArmor(this);
        shield = type.shield?.GetShield(this);
        reactor = type.reactor?.GetReactor(this);
        solar = type.solar?.GetSolar(this);
        service = type.service?.GetService(this);
    }
    public T Get<T>() where T:class, Device{
        return (T)new Dictionary<Type, Device>() {
                [typeof(Armor)] = armor,
                [typeof(Engine)] = engine,
                [typeof(Reactor)] = reactor,
                [typeof(Service)]= service,
                [typeof(Shield)] = shield,
                [typeof(Solar)] = solar,
                [typeof(Weapon)] = weapon,
        }[typeof(T)];
    }
    public bool Get<T>(out T result) where T : class, Device => (result = Get<T>()) != null;
    public bool Has<T>() where T : class, Device => Get<T>() != null;
    public bool HasDevice() => (armor ?? engine ?? reactor??service??shield?? (Device)solar ?? weapon) != null;
    public IEnumerable<Device> GetDevices() => new List<Device> { armor, engine, reactor, shield, solar, weapon }.Where(d => d != null);
}
public interface Device {
    Item source { get; }
    void Update(double delta, IShip owner);
    int? powerUse => null;
    public bool IsEnabled(IShip owner) =>
        (owner as PlayerShip)?.energy.off.Contains(this) != false;
    public void OnOverload(PlayerShip owner) { }
    public void OnDisable() { }
}
/*
public class MultiItemAmmo : IAmmo {
    public int index;
    public List<IAmmo> missiles;
    public IAmmo current => missiles[index];
    public bool AllowFire => current.AllowFire;
    public MultiItemAmmo(List<IAmmo> missiles) {
        this.missiles = missiles;
    }
    public void Update(IShip source) => current.Update(source);
    public void Update(Station source) => current.Update(source);

    public void OnFire() => current.OnFire();
}
*/
public interface PowerSource {
    double energyDelta { get; set; }
    int maxOutput { get; }
}
public class Decay {
    [Req] public double lifetime;
    [Req] public double rate;
    [Opt] public bool lethal;
    [Opt] public double degradeFactor;
    public Decay() { }
    public Decay(XElement e) => e.Initialize(this);
    public Decay(Decay source) {
        this.lifetime = source.lifetime;
        this.rate = source.rate;
        this.lethal = source.lethal;
        this.degradeFactor = source.degradeFactor;
    }
}
public class Armor : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ArmorDesc desc;
    public int hp;
    //Temporarily increase max HP upon absorbing damage. Titan HP decays over time.
    public double titanHP;
    public double hpToRecover;
    public double recoveryHP;
    public double regenHP;
    public double decayHP;
    public double degradeHP;
    public int killHP;
    public double damageDelay;
    public double stealth => desc.stealth == 0 ? 0 : desc.stealth * hp / desc.maxHP;
    public int lifetimeDamageAbsorbed;
    public int lastDamageTick;
    HashSet<Decay> decay=new();
    public int maxHP => Math.Max(desc.maxHP/4, desc.maxHP - (int)(desc.lifetimeDegrade * lifetimeDamageAbsorbed));
    
    public Armor() { }
    public Armor(Item source, ArmorDesc desc) {
        this.source = source;
        this.desc = desc;
        this.hp = desc.initialHP != -1 ? desc.initialHP : desc.maxHP;
    }
    public Armor Copy(Item source) => desc.GetArmor(source);
    public void Repair(RepairArmor ra) {
        hp = Math.Min(maxHP, hp + ra.repairHP);
    }
    public void Update(double delta, IShip owner) {
        if (decay.Any()) {
            var expired = new HashSet<Decay>();

            foreach (var d in decay) {
                decayHP += d.rate * delta * 60;
                degradeHP += d.rate * d.degradeFactor * delta * 60;

                d.lifetime -= delta;
                if(d.lifetime <= 0) {
                    expired.Add(d);
                }
            }
            decay.ExceptWith(expired);
            if (decayHP >= 1) {
                var deltaHP = Math.Min(hp, (int)decayHP);
                hp -= deltaHP;
                lastDamageTick = owner.world.tick;

                //lifetimeDamageAbsorbed += (int)decayHP;
                //lifetimeDamageAbsorbed += deltaHP;
                lifetimeDamageAbsorbed += (int)degradeHP;
                
                hpToRecover += (deltaHP * desc.recoveryFactor);
                damageDelay = 30;

                decayHP = 0;
                degradeHP = 0;
            }
        }
        if (damageDelay > 0) {
            damageDelay -= delta * Program.TICKS_PER_SECOND;
            return;
        }
        if (hpToRecover >= 1) {
            recoveryHP += desc.recoveryRate * delta * Program.TICKS_PER_SECOND;
            while (recoveryHP >= 1) {
                if (hp < maxHP) {
                    hp++;
                    recoveryHP--;
                    hpToRecover--;
                } else {
                    recoveryHP = 0;
                    hpToRecover = 0;
                }
            }
        }
        regenHP += desc.regenRate * delta * Program.TICKS_PER_SECOND;
        while (regenHP >= 1) {
            if(hp < maxHP) {
                hp++;
                regenHP--;
            } else {
                regenHP = 0;
            }
        }
        if (hp > 0 && killHP < desc.killHP) {
            killHP = desc.killHP;
        }
    }
    private void OnAbsorb(int absorbed) {
        lifetimeDamageAbsorbed += absorbed;

        titanHP += absorbed * desc.titanFactor;
        hpToRecover += (absorbed * desc.recoveryFactor);
        damageDelay = 30;
    }
    public void Absorb(int amount) {
        if(hp == 0 || amount < 1) {
            return;
        }
        //Check if we have a kill threshold
        if (hp <= killHP) {
            if(amount < killHP) {
                //Remember this but take no damage
                OnAbsorb(amount);
            } else {
                //Otherwise, we fall
                hp = 0;
                OnAbsorb(killHP);
            }
            return;
        }
        var absorbed = Math.Min(hp, amount);
        hp -= absorbed;
        OnAbsorb(absorbed);
        if (killHP > 0 && absorbed > killHP) {
            killHP = 0;
        }
    }
    public int Absorb(Projectile p) {
        if (p.damageHP < 1)
            return 0;

        bool down = hp == 0;
        //If we have a minAbsorb, then we're considered active even at 0 hp

        var damageWall = desc.minAbsorb.Roll();
        if(down && damageWall == 0) {
            return 0;
        }
        //If we're below the drill threshold, then skip this armor
        if((float)hp / desc.maxHP < p.desc.armorDrill) {
            return 0;
        }
        //If we're active and the projectile has armor skip, then skip us now.
        if(p.armorSkip > 0) {
            p.armorSkip--;
            return 0;
        }
        if(down && damageWall > 0) {
            p.damageHP = Math.Max(0, p.damageHP - damageWall);
            return 0;
        }


        //Check if we have a kill threshold
        if (hp <= killHP) {
            if (p.damageHP < killHP) {
                p.damageHP = 0;
                var amount = p.damageHP;
                p.hitBlocked = true;
                //Remember this but take no damage
                OnAbsorb(amount);
                return amount;
            } else {
                p.damageHP -= killHP;
                lastDamageTick = p.world.tick;
                //Otherwise, we fall
                hp = 0;
                OnAbsorb(killHP);
                return killHP;
            }
        }

        var multiplier = p.desc.armorFactor;// + lifetimeDamageAbsorbed * desc.lifetimeDegrade;
        var absorbed = (int)Math.Clamp(p.damageHP * multiplier, 0, hp);
        hp -= absorbed;
        OnAbsorb(absorbed);
        if (killHP > 0 && absorbed > killHP) {
            killHP = 0;
        }
        lastDamageTick = p.world.tick;

        if (p.desc.decay is Decay d) {
            decay.Add(new(d));
        }

        double reflectChance = desc.reflectFactor * hp / desc.maxHP - p.desc.antiReflect;
        if (p.world.karma.NextDouble() < reflectChance) {
            p.hitReflected = true;
            return absorbed;
        }

        p.damageHP = Math.Max(0, p.damageHP - (int)Math.Ceiling(Math.Max(absorbed, damageWall) / multiplier));
        
        return absorbed;
    }
}
public class Engine : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public EngineDesc desc;
    public bool thrusting;
    public Engine() { }
    public Engine(Item source, EngineDesc desc) {
        this.source = source;
        this.desc = desc;
    }
    public Engine Copy(Item source) => desc.GetEngine(source);
    public void Update(double delta, IShip owner) {
        var rotationDeg = owner.rotationDeg;
        var ship = (owner is PlayerShip ps ? ps.ship : owner is AIShip a ? a.ship : null);
        var sc = ship.shipClass;
        UpdateThrust();
        UpdateTurn();
        UpdateRotation();
        UpdateBrake();
        void UpdateThrust() {
            if (thrusting) {
                var rotationRads = rotationDeg * Math.PI / 180;

                var exhaust = new EffectParticle(ship.position + XY.Polar(rotationRads, -1),
                    ship.velocity + XY.Polar(rotationRads, -sc.thrust),
                    new ColoredGlyph(Color.Yellow, Color.Transparent, '.'),
                    4);
                ship.world.AddEffect(exhaust);

                ship.velocity += XY.Polar(rotationRads, sc.thrust * delta * Program.TICKS_PER_SECOND);
                if (ship.velocity.magnitude > ship.shipClass.maxSpeed) {
                    ship.velocity = ship.velocity.normal * sc.maxSpeed;
                }

                thrusting = false;
            }
        }
        void UpdateTurn() {
            if (ship.rotating != Rotating.None) {
                ref var rv = ref ship.rotatingVel;
                if (ship.rotating == Rotating.CCW) {
                    /*
                    if (rotatingSpeed < 0) {
                        rotatingSpeed += Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if (rv < 0) {
                        Decel();
                    }
                    rv += sc.rotationAccel * delta;
                } else if (ship.rotating == Rotating.CW) {
                    /*
                    if(rotatingSpeed > 0) {
                        rotatingSpeed -= Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if (rv > 0) {
                        Decel();
                    }
                    rv -= sc.rotationAccel * delta;
                }
                rv = Math.Min(Math.Abs(rv), sc.rotationMaxSpeed) * Math.Sign(rv);
                ship.rotating = Rotating.None;
            } else {
                Decel();
            }
            void Decel() => ship.rotatingVel -= Math.Min(Math.Abs(ship.rotatingVel), sc.rotationDecel * delta) * Math.Sign(ship.rotatingVel); ;
        }
        void UpdateRotation() => ship.rotationDeg += ship.rotatingVel;
        void UpdateBrake() {
            if (ship.decelerating) {
                if (ship.velocity.magnitude > 0.05) {
                    ship.velocity -= ship.velocity.normal * Math.Min(ship.velocity.magnitude, sc.thrust * delta * Program.TICKS_PER_SECOND / 2);
                } else {
                    ship.velocity = new XY();
                }
                ship.decelerating = false;
            }
        }
    }
}
public class Enhancer : Device {
    [JsonProperty]
    public Item source { get; set; }
    public EnhancerDesc desc;
    public int powerUse => desc.powerUse;
    public Enhancer() { }
    public Enhancer(Item source, EnhancerDesc desc) {
        this.source = source;
        this.desc = desc;
    }
    public Enhancer Copy(Item source) => desc.GetEnhancer(source);
    public void Update(double delta, IShip owner) {
    }
}

public class Launcher : Device {
    public LauncherDesc desc;
    public Weapon weapon;
    public int index;
    [JsonIgnore]
    public Item source => weapon.source;
    [JsonIgnore]
    public LaunchDesc fragmentDesc => desc.missiles[index];
    [JsonIgnore]
    int? Device.powerUse => ((Device)weapon).powerUse;
    [JsonIgnore]
    public Capacitor capacitor => weapon.capacitor;
    [JsonIgnore]
    public IAiming aiming => weapon.aiming;
    [JsonIgnore]
    public IAmmo ammo => weapon.ammo;
    [JsonIgnore]
    public double delay => weapon.delay;
    [JsonIgnore]
    public bool firing => weapon.firing;
    [JsonIgnore]
    public int repeatsLeft => weapon.repeatsLeft;
    public Launcher() { }
    public Launcher(Item source, LauncherDesc desc) {
        this.weapon = desc.GetWeapon(source);
        this.desc = desc;
    }
    public Launcher Copy(Item source) => desc.GetLauncher(source);
    public void SetMissile(int index) {
        this.index = index;
        var l = desc.missiles[index];
        weapon.ammo = new ItemAmmo(l.ammoType);
        weapon.desc.projectile = l.shot;
    }
    public string GetReadoutName() => weapon.GetReadoutName();
    public ColoredString GetBar(int BAR) => weapon.GetBar(BAR);
    public void Update(double delta, Station owner) => weapon.Update(delta, owner);
    public void Update(double delta, IShip owner) => weapon.Update(delta, owner);
    public void OnDisable() => weapon.OnDisable();
    public bool RangeCheck(ActiveObject user, ActiveObject target) => weapon.RangeCheck(user, target);
    public bool AllowFire => weapon.AllowFire;
    public bool ReadyToFire => weapon.ReadyToFire;
    public void Fire(ActiveObject owner, double direction) => weapon.Fire(owner, direction);
    public ActiveObject target => weapon.target;
    public void OverrideTarget(ActiveObject target) => weapon.SetTarget(target);
    public void SetFiring(bool firing = true) => weapon.SetFiring(firing);
    public void SetFiring(bool firing = true, ActiveObject target = null) => weapon.SetFiring(firing, target);
}
public class Reactor : Device, PowerSource {
    [JsonProperty]
    public Item source { get; set; }
    public ReactorDesc desc;
    public double energy;
    [JsonProperty]
    public double energyDelta { get; set; }
    public int rechargeDelay;
    public int maxOutput => energy > 0 ? desc.maxOutput : 0;
    public double lifetimeEnergyUsed;
    public Reactor() { }
    public Reactor(Item source, ReactorDesc desc) {
        this.source = source;
        this.desc = desc;
        energy = desc.capacity;
        energyDelta = 0;
    }
    public Reactor Copy(Item source) => desc.GetReactor(source);
    public void Update(double delta, IShip owner) {
        var e = energy;
        energy = Math.Clamp(energy + (energyDelta < 0 ? energyDelta / desc.efficiency : energyDelta) * delta, 0, desc.capacity);
        lifetimeEnergyUsed += Math.Max(0, e - energy);
    }
}
public class Service : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ServiceDesc desc;
    public int ticks;
    [JsonProperty]
    public int powerUse { get; private set; }
    int? Device.powerUse => powerUse;
    public Service() { }
    public Service(Item source, ServiceDesc desc) {
        this.source = source;
        this.desc = desc;
        powerUse = 0;
    }
    public Service Copy(Item source) => desc.GetService(source);
    public void Update(double delta, IShip owner) {
        ticks++;
        if (ticks % desc.interval == 0) {
            var powerUse = 0;
            switch (desc.type) {
                case ServiceType.missileJack: {
                        //May not work in Arena mode if we assume control
                        //bc weapon locks are focused on the old AI ship
                        var missile = owner.world.entities.all
                            .OfType<Projectile>()
                            .FirstOrDefault(
                                p => (owner.position - p.position).magnitude < 24
                                  && p.maneuver != null
                                  && p.maneuver.maneuver > 0
                                  && Equals(p.maneuver.target, owner)
                                );
                        if (missile != null) {
                            missile.maneuver.target = missile.source;
                            missile.source = owner;
                            var offset = (missile.position - owner.position);
                            var dist = offset.magnitude;
                            var inc = offset.normal;
                            for (var i = 0; i < dist; i++) {
                                var p = owner.position + inc * i;
                                owner.world.AddEffect(new EffectParticle(p, new ColoredGlyph(Color.Orange, Color.Transparent, '-'), 10));
                            }
                            powerUse = desc.powerUse;
                        }
                        break;
                    }
                case ServiceType.armorRepair: {
                        break;
                    }
                case ServiceType.grind:
                    if (owner is PlayerShip player) {
                        powerUse = this.powerUse + (player.energy.totalOutputMax - player.energy.totalOutputUsed);
                    }
                    break;
            }
            this.powerUse = powerUse;
        }
    }
    void Device.OnOverload(PlayerShip owner) {
        powerUse = owner.energy.totalOutputLeft;
    }
}
public class Shield : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ShieldDesc desc;
    public int hp;
    public double regenHP;
    public double delay;
    public double absorbFactor => desc.absorbFactor;
    public int maxAbsorb => hp;
    public int lifetimeDamageAbsorbed;
    public int stealth => desc.stealth == 0 ? 0 :
        delay > 0 ? 0 :
        desc.stealth * hp / desc.maxHP;
    /*
    public int maxAbsorb => desc.absorbMaxHP == -1 ?
        hp : Math.Min(hp, absorbHP);
    public int absorbHP;
    public double absorbRegenHP;
    */
    int? Device.powerUse => hp < desc.maxHP ? desc.powerUse : desc.idlePowerUse;
    public Shield() { }
    public Shield(Item source, ShieldDesc desc) {
        this.source = source;
        this.desc = desc;
    }
    public Shield Copy(Item source) => desc.GetShield(source);
    public void OnDisable() => Deplete();
    public void Deplete() {

        hp = 0;
        regenHP = 0;
        delay = desc.depletionDelay;
    }
    public void Update(double delta, IShip owner) {
        if (delay > 0) {
            delay -= delta * Program.TICKS_PER_SECOND;
        } else {
            regenHP += desc.regen * delta * Program.TICKS_PER_SECOND;
            while (regenHP >= 1) {
                if (hp < desc.maxHP) {
                    hp++;
                    regenHP--;
                } else {
                    regenHP = 0;
                }
            }
            /*
            absorbRegenHP += desc.absorbRegen;
            while(absorbRegenHP >= 1) {
                if(absorbHP < desc.absorbMaxHP) {
                    absorbHP++;
                    absorbRegenHP--;
                } else {
                    absorbRegenHP = 0;
                }
            }
            */
        }
    }
    public void Absorb(Projectile p) {
        var multiplier = p.desc.shieldFactor;
        var absorbed = (int)Math.Clamp(p.damageHP * (1 - p.desc.shieldDrill) * absorbFactor * multiplier, 0, maxAbsorb);
        if (absorbed > 0) {
            hp -= absorbed;
            lifetimeDamageAbsorbed += absorbed;
            delay = (hp == 0 ? desc.depletionDelay : desc.damageDelay);

            double reflectChance = desc.reflectFactor * hp / desc.maxHP - p.desc.antiReflect;
            if (p.world.karma.NextDouble() < reflectChance) {
                p.hitReflected = true;
                return;
            }
            p.damageHP -= (int)Math.Ceiling(absorbed / multiplier);
        }
    }
}
public class Solar : Device, PowerSource {
    [JsonProperty]
    public Item source { get; private set; }
    public SolarDesc desc;
    public int durability;
    [JsonProperty]
    public int maxOutput { get; private set; }
    [JsonProperty]
    public double energyDelta { get; set; }
    public Solar() { }
    public Solar(Item source, SolarDesc desc) {
        this.source = source;
        this.desc = desc;
        durability = desc.durability;
    }
    public Solar Copy(Item source) => desc.GetSolar(source);
    public void Update(double delta, IShip owner) {
        void Update() {
            var t = owner.world.backdrop.starlight.GetBackgroundFixed(owner.position);
            var b = t.GetBrightness();
            maxOutput = (int)(b * desc.maxOutput);
        }
        switch (durability) {
            case -1:
                Update();
                break;
            case 0:
                break;
            case 1:
                durability = 0;
                maxOutput = 0;
                if (owner is PlayerShip ps) {
                    ps.AddMessage(new Message($"{source.name} has stopped functioning"));
                }
                break;
            case > 1:
                durability = (int)Math.Max(1, durability + energyDelta * delta);
                Update();
                break;
            default: throw new Exception($"Invalid durability value {durability}");
        }
    }
}
public class Weapon : Device, Ob<Projectile.OnHitActive> {
    [JsonProperty]
    public Item source { get; private set; }
    public WeaponDesc desc;
    [JsonIgnore]
    int? Device.powerUse => (firing || delay > 0 || capacitor?.full == false) ? desc.powerUse : 0;
    public IAiming aiming;
    public Targeting targeting;
    public Capacitor capacitor;
    public IAmmo ammo;
    public Modifier mod;
    public FragmentDesc projectileDesc;
    public bool structural;
    public double delay;
    public bool firing;
    public int repeatsLeft;
    public double angle;
    public bool blind;
    public double criticalFactor = 0.0;
    public XY offset=new(0,0);
    public double timeSinceLastFire;
    public int totalTimesFired;
    public record OnFire(Weapon w, List<Projectile> p);
    public Vi<OnFire> onFire=new();
    public Weapon() { }
    public Weapon(Item source, WeaponDesc desc, IAiming aiming = null) {
        this.source = source;
        this.aiming = aiming;
        SetWeaponDesc(desc);
    }
    public bool IsInRange(XY offset) => offset.magnitude2 < projectileDesc.range2;
    public void SetWeaponDesc(WeaponDesc desc) {
        this.desc = desc;

        capacitor =
            desc.capacitor != null ?
                new(desc.capacitor) :
            null;
        ammo =
            desc.initialCharges > -1 ?
                new ChargeAmmo(desc.initialCharges) :
            desc.ammoType != null ?
                new ItemAmmo(desc.ammoType) :
            null;
        aiming ??=
            desc.projectile.omnidirectional || desc.omnidirectional ?
                new Omnidirectional() :
            desc.sweep > 0 ?
                new Swivel(desc.sweep) :
            desc.leftRange + desc.rightRange > 0 ?
                new Swivel(desc.leftRange, desc.rightRange) :
            null;
        targeting =
            desc.projectile.multiTarget ?
                new(true) :
            desc.projectile.acquireTarget ?
                new(false) :
            aiming != null ?
                new(false) :
            null;
        UpdateProjectileDesc();
        structural = desc.structural;
    }
    public Weapon Copy(Item source) => desc.GetWeapon(source);
    public string GetReadoutName() {
        string name = source.type.name;
        return ammo switch {
            ChargeAmmo c => $"[{c.charges, 4}] {name}",
            ItemAmmo i => $"[{i.count, 4}] {name}",
            _ => name
        };
    }
    public ColoredString GetBar(int BAR) {
        if (ammo?.AllowFire == false) {
            return new(new(' ', BAR), Color.Transparent, Color.Black);
        }
        var fireBar = (int)(BAR * (double)(desc.fireCooldown - delay) / desc.fireCooldown);
        ColoredString bar;
        if (capacitor != null && capacitor.desc.minChargeToFire > 0) {
            var chargeBar = (int)(BAR * Math.Min(1, capacitor.charge / capacitor.desc.minChargeToFire));
            bar = new ColoredString(new('>', chargeBar), Color.Gray, Color.Black)
                + new ColoredString(new(' ', BAR - chargeBar), Color.Transparent, Color.Black);
        } else {
            bar = new(new('>', BAR), Color.Gray, Color.Black);
        }
        foreach (var cg in bar.Take(fireBar)) {
            cg.Foreground = Color.White;
        }
        if (capacitor != null) {
            var n = BAR * capacitor.charge / capacitor.desc.maxCharge;
            foreach (var cg in bar.Take((int)n + 1)) {
                cg.Foreground = cg.Foreground.Blend(Color.Cyan.SetAlpha(128));
            }
        }
        return bar;
    }
    public void PeriodicUpdateProjectileDesc(ActiveObject owner, int interval) {
        if(owner.world.tick % interval == 0) {
            UpdateProjectileDesc();
        }
    }
    public void UpdateProjectileDesc() {
        projectileDesc = Modifier.Sum(capacitor?.mod, source.mod, mod) * desc.projectile;
    }
    public void Update(double delta, Station owner) {
        if (!blind) {
            targeting?.Update(owner, this);
            aiming?.Update(owner, this);
        }
        capacitor?.Update();
        timeSinceLastFire += delta;
        if (delay > 0) {
            delay -= delta * Program.TICKS_PER_SECOND;
            PeriodicUpdateProjectileDesc(owner, 30);

            goto Done;
        }

        double? direction = aiming?.GetFireAngle();
        var hasAimAngle = direction != null;

        firing = direction.HasValue;

        bool beginRepeat = true;
        if (repeatsLeft > 0) {
            repeatsLeft--;
            firing = true;
            beginRepeat = false;
        } else if (desc.autoFire) {
            bool CheckProjectile() {
                if (desc.targetProjectile && !blind && Targeting.AcquireMissile(owner, this, s => SStation.IsEnemy(owner, s)) is Projectile target) {
                    direction = Omnidirectional.GetFireAngle(owner, target, this);
                    return true;
                }
                return false;
            }
            bool CheckSpray() {
                direction = desc.spray ? aiming switch {
                    Omnidirectional => new Random().NextDouble() * 2 * Math.PI,
                    Swivel s => angle + new Random().NextDouble() * (s.leftRange + s.rightRange) - s.leftRange,
                    _ => angle
                } : direction;
                return desc.spray;
            }
            if (!(firing = CheckProjectile() || CheckSpray() || hasAimAngle)) {
                goto Cancel;
            }
        } else if (!firing) {
            goto Cancel;
        }
        if (direction == null) {
            goto Cancel;
        }
        var d = XY.Polar(direction.Value);
        var p = owner.position;
        for (int i = 0; i < projectileDesc.range; i++) {
            p += d;
            foreach (var other in owner.world.entities[p].Select(s => s is ISegment seg ? seg.parent : s).Distinct()) {
                switch (other) {
                    case ActiveObject a when (targeting?.HasTarget(a) == true) || owner.CanTarget(a):
                        goto LineCheckDone;
                    case Station s when s == owner:
                    case AIShip ai when owner.guards.Contains(ai):
                    case Wreck:
                    case Projectile:
                        continue;
                    default:
                        firing = false;
                        goto LineCheckDone;
                }
            }
        }
    LineCheckDone:
        ammo?.Update(owner);
        if (!firing || capacitor?.AllowFire == false || ammo?.AllowFire == false) {
            goto Cancel;
        }
        UpdateProjectileDesc();
        delay = repeatsLeft == 0 ? desc.fireCooldown : desc.repeatDelay;
        if (beginRepeat) {
            repeatsLeft = desc.repeat;
        }
        Fire(owner, direction.Value);
        goto Done;

    Cancel:
        PeriodicUpdateProjectileDesc(owner, 15);
        repeatsLeft = 0;

    Done:
        firing = false;
        blind = false;
    }
    public void Update(double delta, IShip owner) {
        if (!blind) {
            targeting?.Update(owner, this);
            aiming?.Update(owner, this);
        }
        capacitor?.Update();
        timeSinceLastFire += delta;
        if (delay > 0) {
            delay -= delta * Program.TICKS_PER_SECOND;
            PeriodicUpdateProjectileDesc(owner, 30);
            goto Done;
        }
        double direction = owner.rotationRad + angle;
        var hasAimAngle = false;
        if (aiming?.GetFireAngle() is double aimAngle) {
            hasAimAngle = true;
            direction = aimAngle;
        }
        bool beginRepeat = true;
        if (repeatsLeft > 0) {
            repeatsLeft--;
            firing = true;
            beginRepeat = false;
        } else if (desc.autoFire) {
            bool CheckProjectile() {
                if (desc.targetProjectile && !blind && Targeting.AcquireMissile(owner, this, s => s != null && SShip.IsEnemy(owner, s)) is Projectile target) {
                    direction = Omnidirectional.GetFireAngle(owner, target, this);
                    return true;
                }
                return false;
            }
            bool CheckSpray() {
                direction = desc.spray ? new Random().NextDouble() * 2 * Math.PI : direction;
                return desc.spray;
            }
            if (!(firing = CheckProjectile() || CheckSpray() || hasAimAngle)) {
                goto Cancel;
            }
        }
        ammo?.Update(owner);
        if (!firing || capacitor?.AllowFire == false || ammo?.AllowFire == false) {
            goto Cancel;
        }
        UpdateProjectileDesc();
        delay = repeatsLeft == 0 ? desc.fireCooldown : desc.repeatDelay;
        if (beginRepeat) {
            repeatsLeft = desc.repeat;
        }
        if(desc.failureRate > 0 && owner.world.karma.NextDouble() < desc.failureRate) {
            repeatsLeft = 0;
            delay = desc.fireCooldown * 3;
            if(owner is PlayerShip pl) {
                pl.AddMessage(new Message("Weapon failed!"));
            }
        }
        Fire(owner, direction);
        //Apply on next tick (create a delta-momentum variable)
        if (desc.recoil > 0) {
            owner.velocity += XY.Polar(direction + Math.PI, desc.recoil);
        }
        goto Done;
    Cancel:
        PeriodicUpdateProjectileDesc(owner, 15);
        repeatsLeft = 0;
    Done:
        firing = false;
        blind = false;
    }
    public void OnDisable() {
        delay = desc.fireCooldown;
        capacitor?.Clear();
        targeting?.ClearTarget();
    }
    public bool RangeCheck(ActiveObject user, ActiveObject target) =>
        (user.position - target.position).magnitude < projectileDesc.range;
    public bool AllowFire => ammo?.AllowFire ?? true;
    public bool ReadyToFire => delay == 0 && (capacitor?.AllowFire ?? true) && (ammo?.AllowFire ?? true);
    public List<Projectile> CreateProjectiles(ActiveObject owner, List<ActiveObject> targets, double direction) {
        HashSet<Entity> exclude = new() { null };
        if (!projectileDesc.hitSource) {
            exclude.Add(owner);
        }
        var projectiles = projectileDesc.CreateProjectiles(owner, targets, direction, offset, exclude);
        var criticalChance = 1.0 / (1 + criticalFactor);
        foreach (var p in projectiles) {
            if (owner.world.karma.NextDouble() > criticalChance) {
                p.damageHP *= 3;
            }
        }
        projectiles.ForEach(p => p.onHitActive += this);
        exclude.UnionWith(projectiles);
        exclude.UnionWith(owner.world.entities.all.OfType<ActiveObject>().Where(a => !owner.CanTarget(a)));
        switch (owner) {
            case PlayerShip p:
                exclude.UnionWith(p.avoidHit);
                AllowWeaponTargets(p.devices.Weapon);
                p.onWeaponFire.Observe(new(p, this, projectiles));
                break;
            case AIShip ai:
                exclude.UnionWith(ai.avoidHit);
                AllowWeaponTargets(ai.devices.Weapon);
                ai.onWeaponFire.Observe(new(ai, this, projectiles));
                break;
            case Station st:
                exclude.UnionWith(st.guards);
                AllowWeaponTargets(st.weapons);
                st.onWeaponFire.Observe(new(st, this, projectiles));
                break;
            case null:
                return projectiles;
        }
        if (targets != null) {
            exclude.ExceptWith(targets);
        }
        return projectiles;
        void AllowWeaponTargets(IEnumerable<Weapon> weapons) =>
            exclude.ExceptWith(weapons.Select(w => w.targeting).Where(a => a != null).SelectMany(w => w.GetMultiTarget()));
    }
    public void Fire(ActiveObject owner, double direction, List<Projectile> result = null) {
        var targets = targeting?.GetMultiTarget().ToList();
        var projectiles = CreateProjectiles(owner, targets, direction);
        projectiles.ForEach(owner.world.AddEntity);
        result?.AddRange(projectiles);


        targeting?.OnFire();
        ammo?.OnFire();
        capacitor?.OnFire();
        onFire.Observe(new(this, projectiles));
        timeSinceLastFire = 0;
        totalTimesFired++;
    }
    public record OnHitActive(Weapon w, Projectile p, ActiveObject hit);
    public Vi<OnHitActive> onHitActive = new();
    public void Observe(Projectile.OnHitActive ev) {
        (var projectile, var hit) = ev;
        projectile.onHitActive -= this;
        onHitActive.Observe(new(this, projectile, hit));
        if (projectile.hitHull) {
            if (projectileDesc.lightning) {
                //delay = 5;
                hit.world.AddEntity(new LightningRod(hit, this, projectile));
            }
            if (projectileDesc.hook) {
                hit.world.AddEntity(new Hook(hit, projectile.source));
            }
            if (projectileDesc.tracker != 0) {
                switch (projectile.source) {
                    case PlayerShip pl:
                        HandlePlayer(pl);
                        break;
                    case AIShip ai when ai.behavior is Wingmate w:
                        HandlePlayer(w.player);
                        break;
                }
                void HandlePlayer(PlayerShip pl) {
                    var time = projectileDesc.tracker;
                    if (pl.tracking.TryGetValue(hit, out var t)) {
                        time = Math.Max(t, time);
                    }
                    pl.tracking[hit] = projectileDesc.tracker;
                }
            }
        }
    }
    public ActiveObject target => targeting?.target;
    public void SetTarget(ActiveObject target) =>
        targeting?.SetTarget(target);
    public void SetFiring(bool firing = true) => this.firing = firing;
    //Use this if you want to override auto-aim
    public void SetFiring(bool firing = true, ActiveObject forceTarget = null) {
        this.firing = firing;
        if (forceTarget != null) {
            targeting?.UpdateTarget(forceTarget);
        }
    }
}
public class Capacitor {
    public CapacitorDesc desc;
    public double charge;
    public bool full => charge == desc.maxCharge;
    public Capacitor(CapacitorDesc desc) {
        this.desc = desc;
    }
    public void CheckFire(ref bool firing) => firing = firing && AllowFire;
    public bool AllowFire => desc.minChargeToFire <= charge;
    public void Update() =>
        charge = Math.Min(desc.maxCharge, charge + desc.rechargePerTick);
    public Modifier mod => new() {
        damageHP =      new(inc: (int)(charge * desc.bonusDamagePerCharge)),
        missileSpeed =  new(inc: (int)(charge * desc.bonusSpeedPerCharge)),
        lifetime =      new(inc: (int)(charge * desc.bonusLifetimePerCharge))
    };
    /*
    public FragmentDesc Modify(FragmentDesc fd) =>
        fd with {
            damageHP = new DiceInc(fd.damageHP, (int)(desc.bonusDamagePerCharge * charge)),
            missileSpeed = fd.missileSpeed + (int)(desc.bonusSpeedPerCharge * charge),
            lifetime = fd.lifetime + (int)(desc.bonusLifetimePerCharge * charge)
        };
    public void Modify(ref FragmentDesc fd) =>
        fd = fd with {
            damageHP = new DiceInc(fd.damageHP, (int)(desc.bonusDamagePerCharge * charge)),
            missileSpeed = fd.missileSpeed + (int)(desc.bonusSpeedPerCharge * charge),
            lifetime = fd.lifetime + (int)(desc.bonusLifetimePerCharge * charge)
        };
    */
    public void OnFire() =>
        charge = Math.Max(0, charge - desc.dischargeOnFire);
    public void Clear() => charge = 0;
}
public class Targeting : IDestroyedListener {
    public bool cycleTargets;
    private int ticks;
    public void Observe(IDestroyedListener.Destroyed ev) {
        var (s, d) = ev;
        targets.Remove(s);
        index.Adjust(s);
        ticks = 0;
    }
    public IEnumerable<ActiveObject> GetMultiTarget() {
        if (cycleTargets) {
            foreach(var t in index.list) {
                yield return t;
            }
        } else {
            yield return target;
        }
    }
    public List<ActiveObject> targets => index.list;
    public ListIndex<ActiveObject> index;
    public ActiveObject target {
        get {
            return index.item;
        }
        set {
            index.list.Clear();
            index.list.Add(value);
            index.Reset();
        }
    }
    public Targeting(bool cycleTargets) {
        this.cycleTargets = cycleTargets;
        index = new(new());
    }
    public void Update(ActiveObject owner, Weapon weapon, Func<ActiveObject, bool> filter) {
        if (ticks++ % 30 != 0) {
            return;
        }
        targets.RemoveAll(t => !(t?.active == true
            && owner.CanSee(t)
            && weapon.IsInRange(owner.position - t.position)
            ));
        index.Adjust(target);
        if (targets.Any()) {
            return;
        }
        var current = SSpaceObject.GetTargets(owner);
        targets.AddRange(AcquireTargets(owner, weapon, filter));
        index.Reset();
        index.Skip(current);
        foreach (var t in targets) {
            ((IDestroyedListener)this).Register(t);
        }
    }
    public void OnFire() {
        if (cycleTargets) {
            index++;
        }
    }
    public void Update(Station owner, Weapon weapon) =>
        Update(owner, weapon, other => owner.CanSee(other) && SStation.IsEnemy(owner, other));
    public void Update(IShip owner, Weapon weapon) =>
        Update(owner, weapon, other => owner.CanSee(other) && SShip.IsEnemy(owner, other));
    public void ClearTarget() => targets.Clear();
    public bool HasTarget(ActiveObject o) => targets.Contains(o);
    public void SetTarget(ActiveObject target) {
        ClearTarget();
        targets.Add(target);
    }
    public void UpdateTarget(ActiveObject target) {
        targets.Insert(0, target);
    }
    public static ActiveObject AcquireTarget(ActiveObject owner, Weapon weapon, Func<ActiveObject, bool> filter) {

        return owner.world.entities.FilterKey(p =>
            weapon.IsInRange(owner.position - p))
            .OfType<ActiveObject>()
            .FirstOrDefault(filter);
    }
    public static IEnumerable<ActiveObject> AcquireTargets(ActiveObject owner, Weapon weapon, Func<ActiveObject, bool> filter) =>
        owner.world.entities.FilterKey(p =>
            weapon.IsInRange(owner.position - p))
            .OfType<ActiveObject>()
            .Where(filter);
    public static Projectile AcquireMissile(ActiveObject owner, Weapon weapon, Func<ActiveObject, bool> filter) =>
        owner.world.entities.all
            .OfType<Projectile>()
            .Where(p => (owner.position - p.position).magnitude2 < weapon.projectileDesc.range2)
            .Where(p => filter(p.source))
            .OrderBy(p => (owner.position - p.position).Dot(p.velocity))
            //.OrderBy(p => (owner.Position - p.Position).Magnitude2)
            .FirstOrDefault();
}
public interface IAiming {
    void Update(Station owner, Weapon weapon);
    void Update(IShip owner, Weapon weapon);
    double? GetFireAngle() => null;
}
public class Omnidirectional : IAiming {
    double? direction;
    public Omnidirectional() {
    }
    public static double GetFireAngle(MovingObject owner, MovingObject target, Weapon w) =>
        Helper.CalcFireAngle(target.position - (owner.position + w.offset),
            target.velocity - owner.velocity,
            w.projectileDesc.missileSpeed, out var _);
    public void UpdateDirection(ActiveObject owner, Weapon weapon) {
        var t = weapon.targeting.target;
        if (t != null) {
            direction = GetFireAngle(owner, t, weapon);
            Heading.AimLine(owner.world, owner.position + weapon.offset, direction.Value);
            //Heading.Crosshair(owner.world, target.position);
        } else {
            direction = null;
        }
    }
    public void Update(Station owner, Weapon weapon) {
        UpdateDirection(owner, weapon);
        
    }
    public void Update(IShip owner, Weapon weapon) {
        UpdateDirection(owner, weapon);
    }
    public double? GetFireAngle() => direction;
}
public class Swivel : IAiming {
    private double facing;
    public readonly double leftRange, rightRange;
    double? direction;
    public Swivel(double range) {
        leftRange = rightRange = range / 2;
    }
    public Swivel(double left, double right) {
        leftRange = left;
        rightRange = right;
    }
    public void Update(ActiveObject owner, Weapon weapon) {
        if (weapon.targeting.cycleTargets) {
            weapon.targeting.index.CycleWhile(t => {
                direction = Omnidirectional.GetFireAngle(owner, t, weapon);
                var deltaRad = Main.AngleDiffRad(facing, direction.Value);
                if (deltaRad > 0 ? deltaRad > leftRange : -deltaRad > rightRange) {
                    direction = null;
                    return true;
                }
                Heading.AimLine(owner.world, owner.position + weapon.offset, direction.Value);
                return false;
            });
            return;
        }
        var targeting = weapon.targeting;
        if (targeting.target == null) {
            direction = null;
            return;
        }
        direction = Omnidirectional.GetFireAngle(owner, targeting.target, weapon);
        var deltaRad = Main.AngleDiffRad(facing, direction.Value);
        if (deltaRad > 0 ? deltaRad > leftRange : -deltaRad > rightRange) {
            direction = null;
            targeting.ClearTarget();
            return;
        }
        Heading.AimLine(owner.world, owner.position + weapon.offset, direction.Value);
    }
    public void Update(Station owner, Weapon weapon) {
        facing = owner.rotation + weapon.angle;
        Update((ActiveObject)owner, weapon);
    }
    public void Update(IShip owner, Weapon weapon) {
        facing = owner.rotationRad + weapon.angle;
        Update((ActiveObject)owner, weapon);
    }
    public double? GetFireAngle() => direction;
}

public interface IAmmo {
    bool AllowFire { get; }
    public void Update(IShip source) { }
    public void Update(Station source) { }
    void CheckFire(ref bool firing) => firing &= AllowFire;
    void OnFire();
}
public class ChargeAmmo : IAmmo {
    public int charges;
    public bool AllowFire => charges > 0;
    public ChargeAmmo(int charges) {
        this.charges = charges;
    }
    public void OnFire() => charges--;
}
public class ItemAmmo : IAmmo {
    public ItemType itemType;
    public HashSet<Item> inventory;
    public Item unit;
    public bool AllowFire => unit != null;
    public int count;
    public ItemAmmo(ItemType itemType) =>
        this.itemType = itemType;
    public void Update(IShip source) {
        if (source.world.tick % 120 == 0) {
            Update(source.cargo);
        }
    }
    public void Update(Station source) {
        if(source.world.tick%120 == 0) {
            Update(source.cargo);
        }
    }
    public void Update(HashSet<Item> inventory) {
        this.inventory = inventory;
        if (unit == null || !inventory.Contains(unit)) {
            UpdateUnit();
        }
    }
    public void UpdateUnit() {
        var units = inventory.Where(i => i.type == itemType).ToList();
        unit = units.FirstOrDefault();
        count = units.Count();
    }
    public void OnFire() {
        inventory.Remove(unit);
        UpdateUnit();
    }
}