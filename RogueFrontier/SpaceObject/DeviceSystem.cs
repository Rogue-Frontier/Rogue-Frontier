using System.Collections.Generic;
using System.Linq;

namespace RogueFrontier {
    public class DeviceSystem {
        public List<Device> Installed;

        public List<Powered> Powered;
        public List<Reactor> Reactors;
        public List<Weapon> Weapons;
        public DeviceSystem() {
            Installed = new List<Device>();
            Powered = new List<Powered>();
            Weapons = new List<Weapon>();
            Reactors = new List<Reactor>();
        }
        public void Install(IEnumerable<Device> Devices) {
            Installed.AddRange(Devices);
            UpdateDevices();
        }
        public void Install(Device Device) {
            Installed.Add(Device);
            UpdateDevices();
        }
        public void Remove(Device device) {
            Installed.Remove(device);
            UpdateDevices();
        }
        public void Clear() {
            Installed.Clear();
            UpdateDevices();
        }
        public void UpdateDevices() {
            Powered = Installed.OfType<Powered>().ToList();
            Reactors = Installed.OfType<Reactor>().ToList();
            Weapons = Installed.OfType<Weapon>().ToList();
        }
        public void Update(IShip owner) {
            Installed.ForEach(d => d.Update(owner));
        }
    }
}
