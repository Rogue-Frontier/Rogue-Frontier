using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static RogueFrontier.Weapon;

namespace RogueFrontier;

public interface ShipGenerator {
    List<AIShip> Generate(TypeCollection tc, SpaceObject owner);
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
public class ShipEntry : ShipGenerator {
    public int count;
    public string codename;
    public IOrderDesc orderDesc;
    public string sovereign;
    public ShipEntry() { }
    public ShipEntry(XElement e) {
        this.count = e.TryAttributeInt(nameof(count), 1);
        this.codename = e.ExpectAttribute(nameof(codename));
        switch (e.TryAttribute("order", "guard")) {
            case "guard":
                orderDesc = new GuardDesc();
                break;
            case "patrol":
                orderDesc = new PatrolOrbitDesc(e);
                break;
            case "patrolCircuit":
                orderDesc = new PatrolCircuitDesc(e);
                break;
        }
        this.sovereign = e.TryAttribute(nameof(sovereign), "");
    }
    public List<AIShip> Generate(TypeCollection tc, SpaceObject owner) {
        if (tc.Lookup<ShipClass>(codename, out var shipClass)) {
            Sovereign sov = owner.sovereign;
            if (sovereign.Any()) {
                tc.Lookup(sovereign, out sov);
            }

            Func<int, XY> GetPos;
            switch (orderDesc) {
                case PatrolOrbitDesc pod:
                    GetPos = i => owner.position + XY.Polar(
                                Math.PI * 2 * i / count,
                                pod.patrolRadius);
                    break;
                default:
                    GetPos = i => owner.position;
                    break;
            }
            return new List<AIShip>(
                Enumerable.Range(0, count)
                .Select(i => new AIShip(new BaseShip(
                        owner.world,
                        shipClass,
                        sov,
                        GetPos(i)
                    ),
                    orderDesc.CreateOrder(owner)
                    ))
                );
        } else {
            throw new Exception($"Invalid ShipClass type {codename}");
        }
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
    public class GuardDesc : IOrderDesc {
        public IShipOrder CreateOrder(SpaceObject owner) => new GuardOrder(owner);
    }
    public class PatrolOrbitDesc : IOrderDesc {
        public int patrolRadius;
        public PatrolOrbitDesc() { }
        public PatrolOrbitDesc(XElement e) {
            patrolRadius = e.ExpectAttributeInt("patrolRadius");
        }
        public IShipOrder CreateOrder(SpaceObject owner) => new PatrolOrbitOrder(owner, patrolRadius);
    }
    //Patrol an entire cluster of stations (moving out to 50 ls + radius of nearest station)
    public class PatrolCircuitDesc : IOrderDesc {
        public int patrolRadius;
        public PatrolCircuitDesc() { }
        public PatrolCircuitDesc(XElement e) {
            patrolRadius = e.ExpectAttributeInt("patrolRadius");
        }
        public IShipOrder CreateOrder(SpaceObject owner) => new PatrolCircuitOrder(owner, patrolRadius);
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
    public string codename;
    public int count;
    public ItemEntry() { }
    public ItemEntry(XElement e) {
        this.codename = e.ExpectAttribute("codename");
        this.count = e.TryAttributeInt("count", 1);
    }
    public List<Item> Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var result = new List<Item>(Enumerable.Range(0, count).Select(_ => new Item(type)));
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
    public string codename;
    public ArmorEntry() { }
    public ArmorEntry(XElement e) {
        this.codename = e.ExpectAttribute("codename");
    }
    public List<Armor> Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type);
        if (item.InstallArmor() != null) {
            return new List<Armor> { item.armor };
        } else {
            throw new Exception($"Expected <ItemType> type with <Armor> desc: {codename}");
        }
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type);
        if (item.InstallArmor() == null) {
            throw new Exception($"Expected <ItemType> type with <Armor> desc: {codename}");
        }
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
                case "Shields":
                    generators.Add(new ShieldsEntry(element));
                    break;
                case "Reactor":
                    generators.Add(new ReactorEntry(element));
                    break;
                case "Misc":
                    generators.Add(new MiscEntry(element));
                    break;
                default:
                    throw new Exception($"Unknown <Devices> subelement {element.Name}");
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
    public string codename;
    public ReactorEntry() { }
    public ReactorEntry(XElement e) {
        this.codename = e.ExpectAttribute("codename");
    }
    List<Device> DeviceGenerator.Generate(TypeCollection tc) {
        return new List<Device> { Generate(tc) };
    }
    Reactor Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type);
        if (item.InstallReactor() != null) {
            return item.reactor;
        } else {
            throw new Exception($"Expected <ItemType> type with <Reactor> desc: {codename}");
        }
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type);
        if (item.InstallReactor() == null) {
            throw new Exception($"Expected <ItemType> type with <Reactor> desc: {codename}");
        }
    }
}

class MiscEntry : DeviceGenerator {
    public string codename;
    public MiscEntry() { }
    public MiscEntry(XElement e) {
        this.codename = e.ExpectAttribute("codename");
    }
    List<Device> DeviceGenerator.Generate(TypeCollection tc) {
        return new List<Device> { Generate(tc) };
    }
    MiscDevice Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type);
        return item.InstallMisc() ?? throw new Exception($"Expected <ItemType> type with <Misc> desc: {codename}");
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type);
        if (item.InstallReactor() == null) {
            throw new Exception($"Expected <ItemType> type with <Misc> desc: {codename}");
        }
    }
}

class ShieldsEntry : DeviceGenerator {
    public string codename;
    public ShieldsEntry() { }
    public ShieldsEntry(XElement e) {
        this.codename = e.ExpectAttribute("codename");
    }
    List<Device> DeviceGenerator.Generate(TypeCollection tc) {
        return new List<Device> { Generate(tc) };
    }

    Shields Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type);
        if (item.InstallShields() != null) {
            return item.shields;
        } else {
            throw new Exception($"Expected <ItemType> type with <Shields> desc: {codename}");
        }
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type);
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
    public WeaponEntry() { }
    public WeaponEntry(XElement e) {
        codename = e.ExpectAttribute("codename");
        omnidirectional = e.TryAttributeBool("omnidirectional", false);
    }

    List<Weapon> WeaponGenerator.Generate(TypeCollection tc) {
        return new List<Weapon> { Generate(tc) };
    }
    List<Device> DeviceGenerator.Generate(TypeCollection tc) {
        return new List<Device> { Generate(tc) };
    }

    Weapon Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        var item = new Item(type);
        if (item.InstallWeapon() != null) {
            if (item.type.name == "Iron laser") {
                int i = 0;
            }
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
        var item = new Item(type);
        if (item.InstallWeapon() == null) {
            throw new Exception($"Expected <ItemType> type with <Weapon> desc: {codename}");
        }
    }
}
