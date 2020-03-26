using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TranscendenceRL {
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
			var result = tc.Lookup<ItemType>(codename);
			if (result.weapon != null) {
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
