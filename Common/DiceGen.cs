using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;
namespace Common;

public interface IDice {
    public static IDice Parse(string s) {
        Match m;
        IDice result = null;
        if ((m = Regex.Match(s, "^(?<value>\\-?[0-9]+$)")).Success) {
            result = new Constant(int.Parse(m.Groups["value"].Value));
        }
        if ((m = Regex.Match(s, "^(?<min>[0-9]+)-(?<max>[0-9]+)$")).Success) {
            result = new IntRange(int.Parse(m.Groups["min"].Value), int.Parse(m.Groups["max"].Value));
        }
        if ((m = Regex.Match(s, "^(?<n>[0-9]+)d(?<m>[0-9]+)((\\+(?<bonus>[0-9]+))|(?<bonus>\\-[0-9]+))$")).Success) {
            result = new DiceRange(int.Parse(m.Groups["n"].Value), int.Parse(m.Groups["m"].Value), int.Parse(m.Groups["bonus"].Value));
        }
        return result;
    }
    public static bool TryParse(string s, out IDice result) => (result = Parse(s)) != null;
    public static string strBonus(int bonus) => bonus > 0 ? $"+{bonus}" : bonus < 0 ? $"-{bonus}" : "";
    int Roll();
    string str { get; }
}

public record DiceInc(IDice sub, int bonus) : IDice {
    public int Roll() => sub.Roll() + bonus;
    public string str => $"({sub.str}){IDice.strBonus(bonus)}";
}
public record DiceFactor(IDice sub, double factor) : IDice {

    public int Roll() => (int)(sub.Roll() * factor);
    public string str => $"({sub.str})*{factor}";
}
public record Constant(int Value) : IDice {
    public int Roll() => Value;
    public string str => $"{Value}";
}
public record IntRange(int min, int max) :IDice{
    public Rand r = new();
    public int range => max - min;
    public int Value => r.NextInteger(min, max);
    public int Roll() => Value;
    public string str => $"{min}-{max}";
}
public record DiceRange(int n, int m, int bonus) : IDice {
    public Rand r=new();
    public int Value => Enumerable.Range(0, n).Select(i => r.NextInteger(m)).Sum() + bonus;
    public int Roll() => Value;
    public string str => $"{n}d{m}{IDice.strBonus(bonus)}";
}
