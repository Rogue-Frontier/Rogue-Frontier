using System;
using System.Linq;

namespace RogueFrontier;

//Destiny-Nature Axis
class DNA {
    //First 180 are left side
    //Second 180 are right side
    //Must add up to 1000
    public int[] destiny;
    public DNA() {
        Random r = new Random();
        int rnd() => r.Next(0, 3);
        destiny = new int[360];
        int sum = 0;
        for (int i = 0; i < 180; i++) {
            int n = rnd();
            destiny[i] = n;
            sum += n;

            n = rnd();
            destiny[i + 180] = n;
            sum += n;
        }
        while (sum < 1000) {
            int index = r.Next(0, 12) + r.Next(0, 2) * 180;
            destiny[index]++;
            sum++;
        }
    }
    public int trait1 => destiny[0..12].Sum() - destiny[180..(180 + 12)].Sum();
    public int trait2 => destiny[0..12].Zip(destiny[180..(180 + 12)], (d1, d2) => d1 * d2).Sum();

}
