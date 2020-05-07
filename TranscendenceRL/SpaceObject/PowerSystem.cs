using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class PowerSystem {
        private DeviceSystem devices;
        public int totalMaxOutput;
        public int powerUsed;
        public PowerSystem(DeviceSystem devices) {
            this.devices = devices;
        }
        public void Update() {
            if (!devices.Reactors.Any()) {
                return;
            }

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

            totalMaxOutput = sources.Sum(r => r.maxOutput);
            int maxOutputLeft = totalMaxOutput;
            int sourceIndex = 0;

            int sourceOutput = sources[sourceIndex].maxOutput;

            int outputUsed = 0;
            foreach(var powered in devices.Powered.Where(p => p.enabled)) {
                if(outputUsed + powered.powerUse > maxOutputLeft) {
                    powered.SetEnabled(false);
                }
                outputUsed += powered.powerUse;
                maxOutputLeft -= outputUsed;

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

            int maxReactorOutputLeft = maxOutputLeft - batteries.Sum(b => b.maxOutput);
            foreach(var battery in batteries.Where(b => b.energy < b.desc.capacity)) {
                if(maxReactorOutputLeft > 0) {
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
            }

            outputUsed = totalMaxOutput - maxOutputLeft;
        }
    }
}
