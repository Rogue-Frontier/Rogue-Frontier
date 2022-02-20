using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static RogueFrontier.Weapon;

namespace RogueFrontier;

public interface ShipGenerator {
    IEnumerable<AIShip> Generate(TypeCollection tc, SpaceObject owner);
    public IEnumerable<AIShip> GenerateAndPlace(TypeCollection tc, SpaceObject owner) {
        var w = owner.world;
        var result = Generate(tc, owner);
        foreach(var s in result) {
            w.AddEntity(s);
            w.AddEffect(new Heading(s));
        }
        return result;
    }
}
public class ShipList : ShipGenerator {
    public List<ShipGenerator> generators;
    public ShipList() { generators = new List<ShipGenerator>(); }
    public ShipList(XElement e) {
        generators = new();
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
    public IEnumerable<AIShip> Generate(TypeCollection tc, SpaceObject owner) =>
        generators.SelectMany(g => g.Generate(tc, owner));
}
public enum ShipOrder {
    attack, guard, patrol, patrolCircuit, 
}
public class ShipEntry : ShipGenerator {
    [Opt] public int count = 1;
    [Req] public string codename;
    [Opt] public string sovereign;
    public IShipOrderDesc orderDesc;
    public EShipBehavior behavior;
    public ShipEntry() { }
    public ShipEntry(XElement e) {
        e.Initialize(this);
        orderDesc = e.TryAttEnum("order", ShipOrder.guard) switch {
            ShipOrder.attack => new AttackDesc(),
            ShipOrder.guard => new GuardDesc(),
            ShipOrder.patrol => new PatrolOrbitDesc(e),
            ShipOrder.patrolCircuit => new PatrolCircuitDesc(e),
            _ => new GuardDesc()
        };
    }
    public IEnumerable<AIShip> Generate(TypeCollection tc, SpaceObject owner) {
        var shipClass = tc.Lookup<ShipClass>(codename);
        Sovereign s = sovereign?.Any() == true ? tc.Lookup<Sovereign>(sovereign) : owner.sovereign;
        Func<int, XY> GetPos = orderDesc switch {
            PatrolOrbitDesc pod => i => owner.position + XY.Polar(
                                        Math.PI * 2 * i / count,
                                        pod.patrolRadius),
            _ => i => owner.position
        };
        return Enumerable.Range(0, count)
            .Select(i => new AIShip(new BaseShip(
                    owner.world,
                    shipClass,
                    s,
                    GetPos(i)
                ),
                orderDesc.Value(owner)
                ));
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

    public interface IShipOrderDesc : IContainer<IShipOrder.Create> {}
    public record AttackDesc : IShipOrderDesc {
        [JsonIgnore]
        public IShipOrder.Create Value => target => new AttackOrder(target);
    }
    public record GuardDesc : IShipOrderDesc {
        [JsonIgnore]
        public IShipOrder.Create Value => target => new GuardOrder(target);
    }
    public record PatrolOrbitDesc() : IShipOrderDesc {
        [Req] public int patrolRadius;
        public PatrolOrbitDesc(XElement e) : this() {
            e.Initialize(this);
        }
        [JsonIgnore]
        public IShipOrder.Create Value => target => new PatrolOrbitOrder(target, patrolRadius);
    }
    //Patrol an entire cluster of stations (moving out to 50 ls + radius of nearest station)
    public record PatrolCircuitDesc() : IShipOrderDesc {
        [Req] public int patrolRadius;
        public PatrolCircuitDesc(XElement e) : this() {
            e.Initialize(this);
        }
        [JsonIgnore]
        public IShipOrder.Create Value => target => new PatrolCircuitOrder(target, patrolRadius);
    }
}

public record ModRoll() {
    public double modifierChance;
    public Modifier modifier;
    public ModRoll(XElement e) : this() {
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
        if(new Rand().NextDouble() <= modifierChance) {
            return modifier;
        }
        return null;
    }
}
public interface IGenerator<T> {
    List<T> Generate(TypeCollection t);
}
public record None<T>() : IGenerator<T> {
    public None(XElement e) : this() { }
    public List<T> Generate(TypeCollection tc) => new();
}
public static class SItemGenerator {
    public static IGenerator<Item> From(XElement element) {
        return element.Name.LocalName switch {
            "Item" => new ItemEntry(element),
            "Items" => new ItemList(element),
            "ItemList" => new ItemList(element),
            "ItemTable" => new ItemTable(element),
            "None" => new None<Item>(),
            _ => throw new Exception($"Unknown <Items> subelement {element.Name}")
        };
    }
}
public record ItemList() : IGenerator<Item> {
    public List<IGenerator<Item>> generators;
    public static List<Item> From(TypeCollection tc, string str) => new ItemList(XElement.Parse(str)).Generate(tc);
    public ItemList(XElement e) : this() {
        generators = new List<IGenerator<Item>>();
        foreach (var element in e.Elements()) {
            generators.Add(SItemGenerator.From(element));
        }
    }
    public List<Item> Generate(TypeCollection tc) =>
        new(generators.SelectMany(g => g.Generate(tc)));
}

public record ItemTable() : IGenerator<Item> {
    [Opt] public IDice count = new Constant(1);
    [Opt] public bool replacement = true;
    public List<(double chance, IGenerator<Item>)> generators;
    private double totalChance;
    public static List<Item> From(TypeCollection tc, string str) => new ItemTable(XElement.Parse(str)).Generate(tc);
    public ItemTable(XElement e) : this() {
        e.Initialize(this);
        generators = new();
        foreach (var element in e.Elements()) {
            var chance = element.ExpectAttDouble("chance");
            generators.Add((chance, SItemGenerator.From(element)));
            totalChance += chance;
        }
    }
    public List<Item> Generate(TypeCollection tc) {
        if (replacement) {
            return new(Enumerable.Range(0, count.Roll()).SelectMany(i => {
                var c = new Random().NextDouble() * totalChance;
                foreach ((var chance, var g) in generators) {
                    if (c < chance) {
                        return g.Generate(tc);
                    } else {
                        c -= chance;
                    }
                }
                throw new Exception("Unexpected roll");
            }));
        } else {
            List<(double chance, IGenerator<Item>)> choicesLeft;
            double totalChanceLeft;
            ResetTable();
            return new(Enumerable.Range(0, count.Roll()).SelectMany(i => {
                if(totalChanceLeft > 0) {
                    ResetTable();
                }
                var c = new Random().NextDouble() * totalChanceLeft;
                for(int j = 0; j < choicesLeft.Count; j++) {
                    (var chance, var g) = generators[j];
                    if (c < chance) {
                        generators.RemoveAt(j);
                        totalChanceLeft -= chance;
                        return g.Generate(tc);
                    } else {
                        c -= chance;
                    }
                }
                throw new Exception("Unexpected roll");
            }));

            void ResetTable() {
                choicesLeft = new(generators);
                totalChanceLeft = totalChance;
            }

        }
    }
}

public record ItemEntry() : IGenerator<Item> {
    [Req] public string codename;
    [Opt] public int count = 1;
    public ModRoll mod;
    public ItemEntry(XElement e) : this() {
        e.Initialize(this);
        mod = new(e);
    }
    public List<Item> Generate(TypeCollection tc) {
        var type = tc.Lookup<ItemType>(codename);
        return new List<Item>(Enumerable.Range(0, count).Select(_ => new Item(type, mod.Generate())));
    }
    //In case we want to make sure immediately that the type is valid
    public void ValidateEager(TypeCollection tc) =>
        tc.Lookup<ItemType>(codename);
}
public interface ArmorGenerator {
    List<Armor> Generate(TypeCollection tc);
}
public record ArmorList() : ArmorGenerator {
    public List<ArmorGenerator> generators;
    public ArmorList(XElement e) : this() {
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
    public List<Armor> Generate(TypeCollection tc) =>
        new(generators.SelectMany(g => g.Generate(tc)));
}
public record ArmorEntry() : ArmorGenerator {
    [Req] public string codename;
    public ModRoll mod;
    public ArmorEntry(XElement e) : this() {
        e.Initialize(this);
        mod = new(e);
    }
    List<Armor> ArmorGenerator.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    public Armor Generate(TypeCollection tc) =>
        SDevice.Generate<Armor>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) =>
        Generate(tc);
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

}
public static class SDevice {
    private static T Install<T>(TypeCollection tc, string codename, ModRoll mod) where T : class, Device =>
        new Item(tc.Lookup<ItemType>(codename), mod.Generate()).Install<T>();
    public static T Generate<T>(TypeCollection tc, string codename, ModRoll mod) where T : class, Device =>
        Install<T>(tc, codename, mod) ??
            throw new Exception($"Expected <ItemType> type with <{typeof(T).Name}> desc: {codename}");
}
public record DeviceList() : IGenerator<Device> {
    public List<IGenerator<Device>> generators;
    public DeviceList(XElement e) : this() {
        generators = new List<IGenerator<Device>>();
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
    public List<Device> Generate(TypeCollection tc) =>
        new(generators.SelectMany(g => g.Generate(tc)));
}

public record ReactorEntry() : IGenerator<Device> {
    [Req] public string codename;
    public ModRoll mod;
    public ReactorEntry(XElement e) : this() {
        e.Initialize(this);
        mod = new(e);
    }
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    Reactor Generate(TypeCollection tc) =>
        SDevice.Generate<Reactor>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}

public record SolarEntry() : IGenerator<Device> {
    [Req] public string codename;
    public ModRoll mod;
    public SolarEntry(XElement e) : this() {
        e.Initialize(this);
        mod = new(e);
    }
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    Solar Generate(TypeCollection tc) =>
        SDevice.Generate<Solar>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}

public record ServiceEntry() : IGenerator<Device> {
    [Req] public string codename;

    public ModRoll mod;
    public ServiceEntry(XElement e) : this() {
        e.Initialize(this);
        mod = new(e);
    }
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    Service Generate(TypeCollection tc) => SDevice.Generate<Service>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}

public record ShieldEntry() : IGenerator<Device> {
    public string codename;

    public ModRoll mod;
    public ShieldEntry(XElement e) : this() {
        codename = e.ExpectAtt("codename");
        mod = new(e);
    }
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    Shield Generate(TypeCollection tc) =>
        SDevice.Generate<Shield>(tc, codename, mod);
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}
public record WeaponList() : IGenerator<Weapon> {
    public List<IGenerator<Weapon>> generators;
    public WeaponList(XElement e) : this() {
        generators = new List<IGenerator<Weapon>>();
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
    public List<Weapon> Generate(TypeCollection tc) =>
        new(generators.SelectMany(g => g.Generate(tc)));
}
public record WeaponEntry() : IGenerator<Device>, IGenerator<Weapon> {
    public string codename;
    public bool omnidirectional;
    public ModRoll mod;
    public WeaponEntry(XElement e) : this() {
        codename = e.ExpectAtt("codename");
        omnidirectional = e.TryAttBool("omnidirectional", false);
        mod = new(e);
    }

    List<Weapon> IGenerator<Weapon>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };
    List<Device> IGenerator<Device>.Generate(TypeCollection tc) =>
        new() { Generate(tc) };

    Weapon Generate(TypeCollection tc) {
        var w = SDevice.Generate<Weapon>(tc, codename, mod);
        if (omnidirectional) {
            w.aiming = new Omnidirectional();
        }
        return w;
    }
    public void ValidateEager(TypeCollection tc) => Generate(tc);
}