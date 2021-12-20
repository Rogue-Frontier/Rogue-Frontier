using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RogueFrontier;
public interface ICrime {
    string name { get; }
    bool resolved { get; set; }
}
public class Crime : ICrime {
    public string name { get; }
    public bool resolved { get; set; }
    public Crime(string name, bool resolved = false) {
        this.name = name;
        this.resolved = resolved;
    }
}
public class Destruction : ICrime {
    public string name => $"destruction of {station.name}";
    public bool resolved { get; set; } = false;
    public Station station;
    public Destruction(Station station) {
        this.station = station;
    }
}