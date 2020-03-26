using Common;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
namespace TranscendenceRL {
    public class ShipClass : DesignType {
		public string codename;
		public string name;
		public double thrust;
		public double maxSpeed;
		public double rotationMaxSpeed;
		public double rotationDecel;
		public double rotationAccel;
		public StaticTile tile;
		public DeviceList weapons;
		public PlayerSettings playerSettings;
		

		public void Initialize(TypeCollection collection, XElement e) {
			codename = e.ExpectAttribute("codename");
			name = e.ExpectAttribute("name");
			thrust = e.ExpectAttributeDouble("thrust");
			maxSpeed = e.ExpectAttributeDouble("maxSpeed");
			rotationMaxSpeed = e.ExpectAttributeDouble("rotationMaxSpeed");
			rotationDecel = e.ExpectAttributeDouble("rotationDecel");
			rotationAccel = e.ExpectAttributeDouble("rotationAccel");
			tile = new StaticTile(e);

			if(e.HasElement("Devices", out XElement xmlDevices)) {
				weapons = new DeviceList(xmlDevices);
			} else {

			}
			if(e.HasElement("PlayerSettings", out XElement xmlPlayerSettings)) {
				playerSettings = new PlayerSettings(xmlPlayerSettings);
			}
		}
		public interface DeviceGenerator {
			List<Device> Generate(TypeCollection c);
		}
		public class DeviceList : DeviceGenerator {
			List<DeviceGenerator> generators;
			public DeviceList(XElement e) {
				generators = new List<DeviceGenerator>();
				foreach(var element in e.Elements()) {
					switch(element.Name.LocalName) {
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
				this.codename = e.ExpectAttribute(codename);
			}
			public List<Device> Generate(TypeCollection tc) {
				if(tc.Lookup<ItemType>(codename, out var result) && result.weapon != null) {
					return new List<Device> { new Item(result).weapon };
				} else {
					throw new Exception($"Expected <ItemType> type with <Weapon> desc: {codename}");
				}
			}
			//In case we want to make sure immediately that the type is valid
			public void ValidateEager(TypeCollection tc) {

			}
		}
	}
	public class PlayerSettings {
		public bool startingClass;
		public string description;
		public string[] map;
		public PlayerSettings(XElement e) {
			startingClass = e.ExpectAttributeBool("startingClass");
			description = e.ExpectAttribute("description");
			var xmlMap = e.ExpectElement("Map");
			map = xmlMap.Value.Replace("\r\n", "\n").Split('\n');
		}
	}
}
