using System.Collections.Generic;
using System.Linq;

namespace RogueFrontier;

public class DeviceSystem {
    public List<Device> Installed;
    public List<Device> Powered;
    public List<Reactor> Reactors;
    public List<Solar> Solars;
    public List<Weapon> Weapons;
    public List<Shield> Shields;
    public DeviceSystem() {
        Installed = new();
        Powered = new();
        Reactors = new();
        Solars = new();
        Weapons = new();
        Shields = new();
    }
    public void Install(IEnumerable<Device> Devices) {
        Installed.AddRange(Devices);
        Powered.AddRange(Devices.Where(d=>d.powerUse>0));
        Reactors.AddRange(Devices.OfType<Reactor>());
        Solars.AddRange(Devices.OfType<Solar>());
        Weapons.AddRange(Devices.OfType<Weapon>());
        Shields.AddRange(Devices.OfType<Shield>());
    }
    public void Install(params Device[] Devices) {
        Installed.AddRange(Devices);
        Powered.AddRange(Devices.Where(d=>d.powerUse>0));
        Reactors.AddRange(Devices.OfType<Reactor>());
        Solars.AddRange(Devices.OfType<Solar>());
        Weapons.AddRange(Devices.OfType<Weapon>());
        Shields.AddRange(Devices.OfType<Shield>());
    }
    public void Remove(params Device[] Devices) {
        Installed.RemoveAll(Devices.Contains);
        Powered.RemoveAll(Devices.Where(d=>d.powerUse>0).Contains);
        Reactors.RemoveAll(Devices.OfType<Reactor>().Contains);
        Solars.RemoveAll(Devices.OfType<Solar>().Contains);
        Weapons.RemoveAll(Devices.OfType<Weapon>().Contains);
        Shields.RemoveAll(Devices.OfType<Shield>().Contains);
    }
    public void Clear() {
        Installed.Clear();
        Powered.Clear();
        Reactors.Clear();
        Solars.Clear();
        Weapons.Clear();
        Shields.Clear();
    }
    public void UpdateDevices() {
        Powered = Installed.Where(d=>d.powerUse>0).ToList();
        Reactors = Installed.OfType<Reactor>().ToList();
        Solars = Installed.OfType<Solar>().ToList();
        Weapons = Installed.OfType<Weapon>().ToList();
        Shields=Installed.OfType<Shield>().ToList();
    }
    public void Update(IShip owner) {
        Installed.ForEach(d => d.Update(owner));
    }
}
