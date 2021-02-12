using Common;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TranscendenceRL {
	public interface ShipGenerator {
		List<BaseShip> Generate(TypeCollection tc, SpaceObject owner);
	}
	public class ShipList : ShipGenerator {
		List<ShipGenerator> generators;
		public ShipList() {}
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
		public List<BaseShip> Generate(TypeCollection tc, SpaceObject owner) {
			var result = new List<BaseShip>();
			generators.ForEach(g => result.AddRange(g.Generate(tc, owner)));
			return result;
		}
	}
	public class ShipEntry : ShipGenerator {
		public string codename;
		public ShipEntry() { }
		public ShipEntry(XElement e) {
			this.codename = e.ExpectAttribute("codename");
		}
		public List<BaseShip> Generate(TypeCollection tc, SpaceObject owner) {
			if (tc.Lookup<ShipClass>(codename, out var shipClass)) {
				return new List<BaseShip> { new BaseShip(owner.World, shipClass, owner.Sovereign, owner.Position) };
			} else {
				throw new Exception($"Invalid ShipClass type {codename}");
			}
		}
		//In case we want to make sure immediately that the type is valid
		public void ValidateEager(TypeCollection tc) {
			if(!tc.Lookup<ShipClass>(codename, out var shipClass)) {
				throw new Exception($"Invalid ShipClass type {codename}");
			}
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
						throw new Exception($"Unknown <Devices> subelement {element.Name}");
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
		List<DeviceGenerator> generators;
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
					default:
						throw new Exception($"Unknown <Devices> subelement {element.Name}");
				}
			}
		}
		public List<Device> Generate(TypeCollection tc) {
			var result = new List<Device>();
			generators.ForEach(g => result.AddRange(g.Generate(tc)));
			return result;
		}
	}

	class ReactorEntry : DeviceGenerator {
		public string codename;
		public ReactorEntry(XElement e) {
			this.codename = e.ExpectAttribute("codename");
		}
		List<Device> DeviceGenerator.Generate(TypeCollection tc) {
			var type = tc.Lookup<ItemType>(codename);
			var item = new Item(type);
			if (item.InstallReactor() != null) {
				return new List<Device> { item.reactor };
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

	class ShieldsEntry : DeviceGenerator {
		public string codename;
		public ShieldsEntry(XElement e) {
			this.codename = e.ExpectAttribute("codename");
		}
		List<Device> DeviceGenerator.Generate(TypeCollection tc) {
			var type = tc.Lookup<ItemType>(codename);
			var item = new Item(type);
			if (item.InstallShields() != null) {
				return new List<Device> { item.shields };
			} else {
				throw new Exception($"Expected <ItemType> type with <Shields> desc: {codename}");
			}
		}
		//In case we want to make sure immediately that the type is valid
		public void ValidateEager(TypeCollection tc) {
			var type = tc.Lookup<ItemType>(codename);
			var item = new Item(type);
			if (item.InstallReactor() == null) {
				throw new Exception($"Expected <ItemType> type with <Shields> desc: {codename}");
			}
		}
	}

	public interface WeaponGenerator {
		List<Weapon> Generate(TypeCollection tc);
	}
	public class WeaponList : WeaponGenerator {
		List<WeaponGenerator> generators;
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
		public WeaponEntry(XElement e) {
			this.codename = e.ExpectAttribute("codename");
		}
		List<Weapon> WeaponGenerator.Generate(TypeCollection tc) {
			var type = tc.Lookup<ItemType>(codename);
			var item = new Item(type);
			if (item.InstallWeapon() != null) {
				return new List<Weapon> { item.weapon };
			} else {
				throw new Exception($"Expected <ItemType> type with <Weapon> desc: {codename}");
			}
		}
		List<Device> DeviceGenerator.Generate(TypeCollection tc) {
			var type = tc.Lookup<ItemType>(codename);
			var item = new Item(type);
			if (item.InstallWeapon() != null) {
				return new List<Device> { item.weapon };
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
}
