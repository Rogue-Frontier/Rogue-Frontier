using Common;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
namespace TranscendenceRL {
	public enum ShipBehaviors {
		none, arnold
    }
    public class ShipClass : DesignType {
		public static ShipClass empty => new ShipClass() { devices = new DeviceList(), damageDesc = new HPSystemDesc(), rotationDecel = 1 };

		public string codename;
		public string name;
		public double thrust;
		public double maxSpeed;
		public double rotationMaxSpeed;
		public double rotationDecel;
		public double rotationAccel;
		public ShipBehaviors behavior;
		public StaticTile tile;
		public HullSystemDesc damageDesc;
		public ItemList cargo;
		public DeviceList devices;
		public PlayerSettings playerSettings;
		
		public void Validate() {
			if(rotationDecel == 0) {
				throw new Exception("Ship must be able to decelerate rotation");
            }
        }
		public ShipClass() {}
		public void Initialize(TypeCollection collection, XElement e) {
			codename = e.ExpectAttribute("codename");
			name = e.ExpectAttribute("name");
			thrust = e.ExpectAttributeDouble("thrust");
			maxSpeed = e.ExpectAttributeDouble("maxSpeed");
			rotationMaxSpeed = e.ExpectAttributeDouble("rotationMaxSpeed");
			rotationDecel = e.ExpectAttributeDouble("rotationDecel");
			rotationAccel = e.ExpectAttributeDouble("rotationAccel");
			behavior = e.TryAttributeEnum(nameof(behavior), ShipBehaviors.none);
			tile = new StaticTile(e);
			if(e.HasElement("HPSystem", out XElement xmlHPSystem)) {
				damageDesc = new HPSystemDesc(xmlHPSystem);
			} else if(e.HasElement("LayeredArmorSystem", out XElement xmlLayeredArmor)) {
				damageDesc = new LayeredArmorDesc(xmlLayeredArmor);
			} else {
				throw new Exception("<ShipClass> requires either <HPSystem> or <LayeredArmorSystem> subelement");
			}
			if(e.HasElement("Devices", out XElement xmlDevices)) {
				devices = new DeviceList(xmlDevices);
			}
			if(e.HasElement("Cargo", out XElement xmlCargo) || e.HasElement("Items", out xmlCargo)) {
				cargo = new ItemList(xmlCargo);
            }
			if(e.HasElement("PlayerSettings", out XElement xmlPlayerSettings)) {
				playerSettings = new PlayerSettings(xmlPlayerSettings);
			}
		}
	}
	public interface HullSystemDesc {
		HullSystem Create(SpaceObject owner);
	}
	public class HPSystemDesc : HullSystemDesc {
		public int maxHP;
		public HPSystemDesc() { }
		public HPSystemDesc(XElement e) {
			maxHP = e.ExpectAttributeInt("maxHP");
		}
		public HullSystem Create(SpaceObject owner) {
			return new HPSystem(maxHP);
		}
	}
	public class LayeredArmorDesc : HullSystemDesc {
		public ArmorList armorList;
		public LayeredArmorDesc() { }
		public LayeredArmorDesc(XElement e) {
			armorList = new ArmorList(e);
		}
		public HullSystem Create(SpaceObject owner) {
			return new LayeredArmorSystem(armorList.Generate(owner.world.types));
		}
	}
	public class PlayerSettings {
		public bool startingClass;
		public string description;
		public string[] map;
		public PlayerSettings() { }
		public PlayerSettings(XElement e) {
			startingClass = e.ExpectAttributeBool("startingClass");
			description = e.ExpectAttribute("description");
			if(e.HasElement("Map", out var xmlMap)) {
				map = xmlMap.Value.Replace("\r", "").Split('\n');
			}
		}
	}
}
