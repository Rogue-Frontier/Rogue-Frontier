using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class EnergySystem {
        public DeviceSystem devices;
        public HashSet<Powered> disabled;
        public int totalMaxOutput;
        public int totalUsedOutput;
        public EnergySystem(DeviceSystem devices) {
            this.devices = devices;
            this.disabled = new HashSet<Powered>();
        }
        public void Update(PlayerShip player) {
            if (!devices.Reactors.Any()) {
                return;
            }

            var generators = new List<Reactor>();
            var batteries = new List<Reactor>();
            foreach(var reactor in devices.Reactors) {
                reactor.energyDelta = 0;
                if(reactor.desc.battery) {
                    batteries.Add(reactor);
                } else {
                    generators.Add(reactor);
                }
            }
            var sources = generators.Concat(batteries).ToList();

            totalMaxOutput = sources.Sum(r => r.maxOutput);
            int maxOutputLeft = totalMaxOutput;
            int sourceIndex = 0;
            int sourceOutput = sources[sourceIndex].maxOutput;
            HashSet<Powered> deactivated = new HashSet<Powered>();
            //Devices consume power
            int outputUsed = 0;
            foreach(var powered in devices.Powered.Where(p => !disabled.Contains(p))) {
                var powerUse = powered.powerUse;
                if (powerUse <= 0) { continue; }
                if (powerUse > maxOutputLeft) {
                    deactivated.Add(powered);
                    continue;
                }
                outputUsed += powerUse;
                maxOutputLeft -= powerUse;

            CheckReactor:
                var source = sources[sourceIndex];

                if(source.desc.battery) {
                    source.chargeDelay = 60;
                }
                if (outputUsed > sourceOutput) {
                    outputUsed -= sourceOutput;
                    source.energyDelta = -sourceOutput;
                    //Go to the next reactor
                    sourceIndex++;
                    sourceOutput = sources[sourceIndex].maxOutput;
                    goto CheckReactor;
                } else {
                    source.energyDelta = -outputUsed;
                }
            }

            
            if(deactivated.Any()) {
                disabled.UnionWith(deactivated);
                player.AddMessage(new Message("Reactor output overload!"));
                foreach(var d in deactivated) {
                    player.AddMessage(new Message($"{d.source.type.name} deactivated!"));
                }
            }

            //Batteries recharge from reactor
            int maxReactorOutputLeft = maxOutputLeft - batteries.Sum(b => b.maxOutput);
            foreach(var battery in batteries.Where(b => b.energy < b.desc.capacity)) {
                if(maxReactorOutputLeft == 0) {
                    continue;
                }
                if(battery.chargeDelay > 0) {
                    battery.chargeDelay--;
                    continue;
                }

                int delta = Math.Min(battery.maxOutput, maxReactorOutputLeft);
                battery.energyDelta = delta;

                outputUsed += delta;
                maxReactorOutputLeft -= delta;

            CheckReactor:
                if (outputUsed > sourceOutput) {
                    outputUsed -= sourceOutput;
                    sources[sourceIndex].energyDelta = -sourceOutput;

                    //Go to the next reactor
                    sourceIndex++;
                    sourceOutput = sources[sourceIndex].maxOutput;
                    goto CheckReactor;
                } else {
                    sources[sourceIndex].energyDelta = -outputUsed;
                }
            }

            totalUsedOutput = totalMaxOutput - maxOutputLeft;
        }
    }
}
