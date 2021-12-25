using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static RogueFrontier.Weapon;

namespace RogueFrontier;

public interface ShipGenerator {
    List<AIShip> Generate(TypeCollection tc, SpaceObject owner);
    public void GenerateAndPlace(TypeCollection tc, SpaceObject owner) {
        var w = owner.world;
        Generate(tc, owner)?.ForEach(s => {
            w.AddEntity(s);
            w.AddEffect(new Heading(s));
        });
    }
}
public class ShipList : ShipGenerator {
    public List<ShipGenerator> generators;
    public ShipList() { generators = new List<ShipGenerator>(); }
    public ShipList(XElement e) {
        generators = new List<ShipGenerator>();
        foreach (var element in e.Elements()) {
            switch (element.Name.LocalName) {
                case "Ship":
                    generators.Add(new ShipEntry(element));
                    break;
                default:
                    throw new Exception($"Unknown <Ships> subelement {element.Name}");
            }
        }
    }
    public List<AIShip> Generate(TypeCollection tc, SpaceObject owner) {
        var result = new List<AIShip>();
        generators.ForEach(g => result.AddRange(g.Generate(tc, owner)));
        return result;
    }
}
public enum ShipOrder {
    attack, guard, patrol, patrolCircuit, 

}
public class ShipEntry : ShipGenerator {
    [Opt<int>(1)] public int count;
    [Req]         public string codename;
    [Opt]         public string sovereign;
    public IOrderDesc orderDesc;
    public ShipEntry() { }
    public ShipEntry(XElement e) {
        e.Initialize(this);
        orderDesc = e.TryAttEnum("order", ShipOrder.guard) switch {
            ShipOrder.attack => new AttackDesc(),
            ShipOrder.guard => new GuardDesc(),
            ShipOrder.patrol => new PatrolOrbitDesc(e),
            ShipOrder.patrolCircuit => new PatrolCircuitDesc(e)
        };
    }
    public List<AIShip> Generate(TypeCollection tc, SpaceObject owner) {
        var shipClass = tc.Lookup<ShipClass>(codename);
        Sovereign s = sovereign.Any() ? tc.Lookup<Sovereign>(sovereign) : owner.sovereign;
        Func<int, XY> GetPos = orderDesc switch {
            PatrolOrbitDesc pod => i => owner.position + XY.Polar(
                                        Math.PI * 2 * i / count,
                                        pod.patrolRadius),
            _ => i => owner.position
        };
        return new List<AIShip>(
            Enumerable.Range(0, count)
            .Select(i => new AIShip(new BaseShip(
                    owner.world,
                    shipClass,
                    s,
                    GetPos(i)
                ),
                orderDesc.CreateOrder(owner)
                ))
            );
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        if (!tc.Lookup<ShipClass>(codename, out var shipClass)) {
            throw new Exception($"Invalid ShipClass type {codename}");
        }
        if (sovereign.Any() && !tc.Lookup<Sovereign>(sovereign, out var sov)) {
            throw new Exception($"Invalid Sovereign type {sovereign}");
        }
    }

    public interface IOrderDesc {
        IShipOrder CreateOrder(SpaceObject owner);
    }
    public record AttackDesc : IOrderDesc {
        public IShipOrder CreateOrder(SpaceObject owner) => new AttackOrder(owner);
    }
    public record GuardDesc : IOrderDesc {
        public IShipOrder CreateOrder(SpaceObject owner) => new GuardOrder(owner);
    }
    public record PatrolOrbitDesc : IOrderDesc {
        [Req] public int patrolRadius;
        public PatrolOrbitDesc() { }
        public PatrolOrbitDesc(XElement e) {
            e.Initialize(this);
        }
        public IShipOrder CreateOrder(SpaceObject owner) => new PatrolOrbitOrder(owner, patrolRadius);
    }
    //Patrol an entire cluster of stations (moving out to 50 ls + radius of nearest station)
    public record PatrolCircuitDesc : IOrderDesc {
        public int patrolRadius;
        public PatrolCircuitDesc() { }
        public PatrolCircuitDesc(XElement e) {
            e.Initialize(this);
        }
        public IShipOrder CreateOrder(SpaceObject owner) => new PatrolCircuitOrder(owner, patrolRadius);
    }
}

public class ModRoll {
    public double modifierChance;
    public Modifier modifier;
    public ModRoll() { }
    public ModRoll(XElement e) {
        modifierChance = e.TryAttDouble(nameof(modifierChance), 1);
        modifier = new Modifier(e);
        if (modifier.empty) {
            modifier = null;
        }
    }
    public Modifier Generate() {
        if (modifier == null) {
            return null;
        }
        return new Rand().NextDouble() <= modifierChance ? modifier : null;
    }
}
public interface ItemGenerator {
    List<Item> Generate(TypeCollection tc);
}
public class ItemList : ItemGenerator {
    public List<ItemGenerator> generators;
    public ItemList() { }
    public ItemList(XElement e) {
        generators = new List<ItemGenerator>();
        foreach (var element in e.Elements()) {
            switch (element.Name.LocalName) {
                case "Item":
                    generators.Add(new ItemEntry(element));
                    break;
                default:
                    throw new Exception($"Unknown <Items> subelement {element.Name}");
            }
        }
    }
    public List<Item> Generate(TypeCollection tc) {
        var result = new List<Item>();
        if (generators != null) {
            generators.ForEach(g => result.AddRange(g.Generate(tc)));
        }
        return result;
    }
}
public class ItemEntry : ItemGenerator {
    [Req]         public string codename;
    [Opt<int>(1)] public int count;
    public ModRoll mod;
    public ItemEntry() { }
    public ItemEntry(XElement e) {
        e.Initialize(this);
        mod = new(e);

    }
    public List<Item> Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var result = new List<Item>(Enumerable.Range(0, count).Select(_ => new Item(type, mod.Generate())));
        return result;
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        tc.Lookup<ItemType>(codename);
    }
}
public interface ArmorGenerator {
    List<Armor> Generate(TypeCollection tc);
}
public class ArmorList : ArmorGenerator {
    public List<ArmorGenerator> generators;
    public ArmorList() { }
    public ArmorList(XElement e) {
        generators = new List<ArmorGenerator>();
        foreach (var element in e.Elements()) {
            switch (element.Name.LocalName) {
                case "Armor":
                    generators.Add(new ArmorEntry(element));
                    break;
                default:
                    throw new Exception($"Unknown <Armor> subelement {element.Name}");
            }
        }
    }
    public List<Armor> Generate(TypeCollection tc) {
        var result = new List<Armor>();
        generators.ForEach(g => result.AddRange(g.Generate(tc)));
        return result;
    }
}
public class ArmorEntry : ArmorGenerator {
    [Req] public string codename;
    public ModRoll mod;
    public ArmorEntry() { }
    public ArmorEntry(XElement e) {
        e.Initialize(this);
        mod = new(e);
    }
    List<Armor> ArmorGenerator.Generate(TypeCollection tc) {
        return new List<Armor> { Generate(tc) };
    }
    public Armor Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());

        return item.InstallArmor()
            ?? throw new Exception($"Expected <ItemType> type with <Armor> desc: {codename}");
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        var a = item.InstallArmor()
            ?? throw new Exception($"Expected <ItemType> type with <Armor> desc: {codename}");

        }
    }
/*
public interface Generator<T> where T: Device {
    List<T> Generate(TypeCollection tc);
}
public class GeneratorList<T> : Generator<T> where T: Device {
    public List<Generator<T>> generators;
    public GeneratorList(XElement e) {

    }
    public List<T> Generate(TypeCollection tc) {
        var result = new List<T>();
        generators.ForEach(g => result.AddRange(g.Generate(tc)));
        return result;
    }
}
*/

public interface DeviceGenerator {
    List<Device> Generate(TypeCollection tc);
}
public class DeviceList : DeviceGenerator {
    public List<DeviceGenerator> generators;
    public DeviceList() {
        generators = new List<DeviceGenerator>();
    }
    public DeviceList(XElement e) {
        generators = new List<DeviceGenerator>();
        foreach (var element in e.Elements()) {
            switch (element.Name.LocalName) {
                case "Weapon":
                    generators.Add(new WeaponEntry(element));
                    break;
                case "Shield":
                    generators.Add(new ShieldEntry(element));
                    break;
                case "Reactor":
                    generators.Add(new ReactorEntry(element));
                    break;
                case "Solar":
                    generators.Add(new SolarEntry(element));
                    break;
                case "Service":
                    generators.Add(new ServiceEntry(element));
                    break;
                default:
                    throw new Exception($"Unknown <Devices> subelement <{element.Name}>");
            }
        }
    }
    public List<Device> Generate(TypeCollection tc) {
        var result = new List<Device>();
        if (generators.Count == 2) {
            //int i = 0;
        }
        foreach (var g in generators) {
            result.AddRange(g.Generate(tc));
        }

        return result;
    }
}

class ReactorEntry : DeviceGenerator {
    [Req] public string codename;
    public ModRoll mod;
    public ReactorEntry() { }
    public ReactorEntry(XElement e) {
        e.Initialize(this);
        mod = new(e);
    }
    List<Device> DeviceGenerator.Generate(TypeCollection tc) {
        return new List<Device> { Generate(tc) };
    }
    Reactor Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        return item.InstallReactor()
            ?? throw new Exception($"Expected <ItemType> type with <Reactor> desc: {codename}");
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        var r = item.InstallReactor() 
            ?? throw new Exception($"Expected <ItemType> type with <Reactor> desc: {codename}");
    }
}

class SolarEntry : DeviceGenerator {
    [Req] public string codename;
    public ModRoll mod;
    public SolarEntry() { }
    public SolarEntry(XElement e) {
        e.Initialize(this);
        mod = new(e);
    }
    List<Device> DeviceGenerator.Generate(TypeCollection tc) {
        return new List<Device> { Generate(tc) };
    }
    Solar Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        return item.InstallSolar()
            ?? throw new Exception($"Expected <ItemType> type with <Solar> desc: {codename}");
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        var r = item.InstallSolar()
            ?? throw new Exception($"Expected <ItemType> type with <Solar> desc: {codename}");
    }
}

class ServiceEntry : DeviceGenerator {
    [Req] public string codename;

    public ModRoll mod;
    public ServiceEntry() { }
    public ServiceEntry(XElement e) {
        e.Initialize(this);
        mod = new(e);
    }
    List<Device> DeviceGenerator.Generate(TypeCollection tc) {
        return new List<Device> { Generate(tc) };
    }
    ServiceDevice Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        return item.InstallMisc() ?? throw new Exception($"Expected <ItemType> type with <Service> desc: {codename}");
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        if (item.InstallMisc() == null) {
            throw new Exception($"Expected <ItemType> type with <Service> desc: {codename}");
        }
    }
}

class ShieldEntry : DeviceGenerator {
    public string codename;

    public ModRoll mod;
    public ShieldEntry() { }
    public ShieldEntry(XElement e) {
        this.codename = e.ExpectAtt("codename");
        this.mod = new(e);
    }
    List<Device> DeviceGenerator.Generate(TypeCollection tc) {
        return new List<Device> { Generate(tc) };
    }

    Shield Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        if (item.InstallShields() != null) {
            return item.shield;
        } else {
            throw new Exception($"Expected <ItemType> type with <Shields> desc: {codename}");
        }
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        if (item.InstallShields() == null) {
            throw new Exception($"Expected <ItemType> type with <Shields> desc: {codename}");
        }
    }
}

public interface WeaponGenerator {
    List<Weapon> Generate(TypeCollection tc);
}
public class WeaponList : WeaponGenerator {
    public List<WeaponGenerator> generators;
    public WeaponList() {
        generators = new List<WeaponGenerator>();
    }
    public WeaponList(XElement e) {
        generators = new List<WeaponGenerator>();
        foreach (var element in e.Elements()) {
            switch (element.Name.LocalName) {
                case "Weapon":
                    generators.Add(new WeaponEntry(element));
                    break;
                default:
                    throw new Exception($"Unknown <Weapons> subelement {element.Name}");
            }
        }
    }
    public List<Weapon> Generate(TypeCollection tc) {
        var result = new List<Weapon>();
        generators.ForEach(g => result.AddRange(g.Generate(tc)));
        return result;
    }
}
class WeaponEntry : DeviceGenerator, WeaponGenerator {
    public string codename;
    public bool omnidirectional;
    public ModRoll mod;
    public WeaponEntry() { }
    public WeaponEntry(XElement e) {
        codename = e.ExpectAtt("codename");
        omnidirectional = e.TryAttBool("omnidirectional", false);
        mod = new(e);
    }

    List<Weapon> WeaponGenerator.Generate(TypeCollection tc) {
        return new List<Weapon> { Generate(tc) };
    }
    List<Device> DeviceGenerator.Generate(TypeCollection tc) {
        return new List<Device> { Generate(tc) };
    }

    Weapon Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        if (item.InstallWeapon() != null) {
            if (omnidirectional) {
                item.weapon.aiming = new Omnidirectional();
            }
            return item.weapon;
        } else {
            throw new Exception($"Expected <ItemType> type with <Weapon> desc: {codename}");
        }
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type, mod.Generate());
        if (item.InstallWeapon() == null) {
            throw new Exception($"Expected <ItemType> type with <Weapon> desc: {codename}");
        }
    }
}
