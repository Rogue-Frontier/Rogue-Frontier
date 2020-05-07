using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    class PowerSystem {
        private DeviceSystem devices;
        public PowerSystem(DeviceSystem devices) {
            this.devices = devices;
        }
        public void Update() {
            var reactors = new List<Reactor>();
            var batteries = new List<Reactor>();
            foreach(var reactor in devices.Reactors) {
                if(reactor.battery) {
                    batteries.Add(reactor);
                } else {
                    reactors.Add(reactor);
                }
            }
            var sources = reactors.Concat(batteries).ToList();

            int maxOutputLeft = sources.Sum(r => r.maxOutput);
            int sourceIndex = 0;
            int sourceOutput = sources[sourceIndex].maxOutput;

            int powerUse = 0;
            foreach(var powered in devices.Powered.Where(p => p.enabled)) {
                if(powerUse + powered.powerUse > maxOutputLeft) {
                    powered.SetEnabled(false);
                }
                powerUse += powered.powerUse;
                maxOutputLeft -= powerUse;

            CheckReactor:
                if (powerUse > sourceOutput) {
                    powerUse -= sourceOutput;
                    sources[sourceIndex].energyDelta = -sourceOutput;

                    //Go to the next reactor
                    sourceIndex++;
                    sourceOutput = sources[sourceIndex].maxOutput;
                    goto CheckReactor;
                } else {
                    sources[sourceIndex].energyDelta = -powerUse;
                }
            }

            int maxSourceOutputLeft = maxOutputLeft - batteries.Sum(b => b.maxOutput);
            foreach(var battery in batteries.Where(b => b.energy < b.desc.capacity)) {
                if(maxSourceOutputLeft > 0) {
                    int delta = Math.Min(battery.maxOutput, maxSourceOutputLeft);
                    battery.energyDelta = delta;

                    powerUse += delta;
                    maxSourceOutputLeft -= delta;

                CheckReactor:
                    if (powerUse > sourceOutput) {
                        powerUse -= sourceOutput;
                        sources[sourceIndex].energyDelta = -sourceOutput;

                        //Go to the next reactor
                        sourceIndex++;
                        sourceOutput = sources[sourceIndex].maxOutput;
                        goto CheckReactor;
                    } else {
                        sources[sourceIndex].energyDelta = -powerUse;
                    }
                }
            }
        }
    }
}
