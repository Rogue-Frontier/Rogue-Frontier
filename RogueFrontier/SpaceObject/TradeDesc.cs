using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace RogueFrontier;
public record ItemFilter(HashSet<string> hasAttributes, HashSet<string> lacksAttributes) {
    public ItemFilter(XElement e) : this(
        e.TryAtt("hasAttributes").Split(";").ToHashSet(),
        e.TryAtt("lacksAttributes").Split(";").ToHashSet()
        ) { }
    public bool Matches(Item i) {
        var f = (string att) => i.type.attributes.Contains(att);
        return hasAttributes.All(f) && !lacksAttributes.Any(f);
    }
}
public record TradeEntry(ItemFilter filter, double priceFactor, int priceInc) {
    public TradeEntry(XElement e) : this(
        new ItemFilter(e),
        e.TryAttDouble("priceAdj"),
        e.TryAttInt("priceInc")
        ) {
        if(e.TryAtt("price", out var strPrice)) {
            (priceFactor, priceInc) = (0, int.Parse(strPrice));
        }
    }
}
public record TradeDesc() : IDesignType {
    Dictionary<ItemType, int> priceTable;
    List<TradeEntry> buyAdj, sellAdj;
    public void Initialize(TypeCollection tc, XElement e) {
        priceTable = e.Element("Prices")?.Value.Trim().Split("\n")
            .Select(line => line.Split(":")).ToDictionary(
            parts => tc.Lookup<ItemType>(parts[0]),
            parts => int.Parse(parts[1])) ?? new();
        sellAdj = e.Element("Buy")?.Elements("Item").Select(e => new TradeEntry(e)).ToList();
        buyAdj = e.Element("Sell")?.Elements("Item").Select(e => new TradeEntry(e)).ToList();
    }
    public int GetBuyPrice(Item i) {
        int price = priceTable[i.type];
        foreach(var e in buyAdj) {
            if (e.filter.Matches(i)) {
                price = (int)(price * e.priceFactor) + e.priceInc;
            }
        }
        return -1;
    }
    public int GetSellPrice(Item i) {
        int price = priceTable[i.type];
        foreach (var e in sellAdj) {
            if (e.filter.Matches(i)) {
                price = (int)(price * e.priceFactor) + e.priceInc;
            }
        }
        return -1;
    }
}