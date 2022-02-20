using Common;
using System;
using System.Xml.Linq;
namespace RogueFrontier;

public enum EShipBehavior {
    none, sulphin
}
public class ShipClass : DesignType {
    public static ShipClass empty => new ShipClass() { devices = new DeviceList(), damageDesc = new HPSystemDesc(), rotationDecel = 1 };

    [Req] public string codename;
    [Req] public string name;
    [Req] public double thrust;
    [Req] public double maxSpeed;
    [Req] public double rotationMaxSpeed;
    [Req] public double rotationDecel;
    [Req] public double rotationAccel;
    [Opt] public bool crimeOnDestroy;
    public EShipBehavior behavior;
    public StaticTile tile;
    public HullSystemDesc damageDesc;
    public ItemList cargo;
    public DeviceList devices;
    public PlayerSettings playerSettings;

    public void Validate() {
        if (rotationDecel == 0) {
            throw new Exception("Ship must be able to decelerate rotation");
        }
    }
    public ShipClass() { }
    public void Initialize(TypeCollection collection, XElement e) {
        if(e.TryAtt("inherit", out string inherit)) {
            var parent = collection.Lookup<ShipClass>(inherit);
            codename = e.Att(nameof(codename));
            name = e.TryAtt(nameof(name), parent.name);
            thrust = e.TryAttDouble(nameof(thrust), parent.thrust);
            maxSpeed = e.TryAttDouble(nameof(maxSpeed), parent.maxSpeed);
            rotationMaxSpeed = e.TryAttDouble(nameof(rotationMaxSpeed), parent.rotationMaxSpeed);
            rotationDecel = e.TryAttDouble(nameof(rotationDecel), parent.rotationDecel);
            rotationAccel = e.TryAttDouble(nameof(rotationAccel), parent.rotationAccel);
            crimeOnDestroy = e.TryAttBool(nameof(crimeOnDestroy), parent.crimeOnDestroy);

            tile = parent.tile;

            behavior = parent.behavior;
            damageDesc = parent.damageDesc;
            devices = parent.devices;
            cargo = parent.cargo;
            playerSettings = parent.playerSettings;


            if(e.HasElement("Tile", out XElement xmlTile)){
                tile = new(xmlTile);
            }
        } else {
            e.Initialize(this);

            tile = new(e);
        }
        behavior = e.TryAttEnum(nameof(behavior), behavior);
        
        if (e.HasElement("HPSystem", out XElement xmlHPSystem)) {
            damageDesc = new HPSystemDesc(xmlHPSystem);
        } else if (e.HasElement("LayeredArmorSystem", out XElement xmlLayeredArmor)) {
            damageDesc = new LayeredArmorDesc(xmlLayeredArmor);
        }
        
        if(damageDesc == null) {
            throw new Exception("<ShipClass> requires either <HPSystem> or <LayeredArmorSystem> subelement");
        }

        if (e.HasElement("Devices", out XElement xmlDevices)) {
            devices = new(xmlDevices);
        }
        if (e.HasElement("Cargo", out XElement xmlCargo) || e.HasElement("Items", out xmlCargo)) {
            cargo = new(xmlCargo);
        }
        if (e.HasElement("PlayerSettings", out XElement xmlPlayerSettings)) {
            playerSettings = new(xmlPlayerSettings);
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
        maxHP = e.ExpectAttInt("maxHP");
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
        startingClass = e.ExpectAttBool("startingClass");
        description = e.ExpectAtt("description");
        if (e.HasElement("Map", out var xmlMap)) {
            map = xmlMap.Value.Replace("\r", "").Split('\n');
        }
    }
}
