using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Common;

public interface IDice {
    public static IDice From(string s) {
        Match m;
        if((m = Regex.Match(s, "^(?<value>[0-9]+$")).Success) {
            return new Constant(int.Parse(m.Groups["value"].Value));
        }
        if((m = Regex.Match(s, "^(?<min>[0-9]+)-(?<max>[0-9]+)$")).Success) {
            return new IntRange(int.Parse(m.Groups["min"].Value), int.Parse(m.Groups["max"].Value));
        }
        if((m = Regex.Match(s, "^(?<n>[0-9]+)d(?<m>[0-9]+)((\\+(?<bonus>[0-9]+))|(?<bonus>\\-[0-9]+))$")).Success) {
            return new DiceRange(int.Parse(m.Groups["n"].Value), int.Parse(m.Groups["m"].Value), int.Parse(m.Groups["bonus"].Value));
        }
        return null;
    }
}
public class Constant : IDice {
    public Constant(int Value) { this.Value = Value; }
    public int Value { get; private set; }
}
public class IntRange :IDice{
    public int min, max;
    public Rand r;
    public int range => max - min;
    public IntRange(int min, int max) {
        this.min = min;
        this.max = max;
        r = new();
    }
    public int Value => r.NextInteger(min, max);
}
public class DiceRange : IDice {
    public int n, m, bonus;
    public Rand r;
    public DiceRange(int n, int m, int bonus) {
        this.n = n;
        this.m = m;
        this.bonus = bonus;
        r = new();
    }
    public int Value => Enumerable.Range(0, n).Select(i => r.NextInteger(m)).Sum() + bonus;
}
