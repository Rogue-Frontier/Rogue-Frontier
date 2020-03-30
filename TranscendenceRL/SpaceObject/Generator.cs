using Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TranscendenceRL {
	public interface ShipGenerator {
		List<Ship> Generate(TypeCollection tc, SpaceObject owner);
	}
	public class ShipList : ShipGenerator {
		List<ShipGenerator> generators;
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
		public List<Ship> Generate(TypeCollection tc, SpaceObject owner) {
			var result = new List<Ship>();
			generators.ForEach(g => result.AddRange(g.Generate(tc, owner)));
			return result;
		}
	}
	public class ShipEntry : ShipGenerator {
		public string codename;
		public ShipEntry(XElement e) {
			this.codename = e.ExpectAttribute("codename");
		}
		public List<Ship> Generate(TypeCollection tc, SpaceObject owner) {
			if (tc.Lookup<ShipClass>(codename, out var shipClass)) {
				return new List<Ship> { new Ship(owner.World, shipClass, owner.Sovereign, owner.Position) };
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
		List<ArmorGenerator> generators;
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
	class WeaponEntry : DeviceGenerator {
		public string codename;
		public WeaponEntry(XElement e) {
			this.codename = e.ExpectAttribute("codename");
		}
		public List<Device> Generate(TypeCollection tc) {
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
