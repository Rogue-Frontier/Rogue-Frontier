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
		public DeviceList devices;
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
				devices = new DeviceList(xmlDevices);
			} else {
				devices = new DeviceList();
			}
			if(e.HasElement("PlayerSettings", out XElement xmlPlayerSettings)) {
				playerSettings = new PlayerSettings(xmlPlayerSettings);
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
