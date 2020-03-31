using System.Collections.Generic;
using System.Linq;

namespace TranscendenceRL {
    public class DeviceSystem {
        public List<Device> Installed;
        public List<Weapon> Weapons;
        public DeviceSystem() {
            Installed = new List<Device>();
            Weapons = new List<Weapon>();
        }
        public void Add(List<Device> Devices) {
            this.Installed.AddRange(Devices);
            UpdateDevices();
        }
        public void UpdateDevices() {
            Weapons = Installed.OfType<Weapon>().ToList();
        }
        public void Update(IShip owner) {
            Installed.ForEach(d => d.Update(owner));
        }
    }
}
