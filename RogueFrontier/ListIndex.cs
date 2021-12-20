using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueFrontier;

public class ListIndex<T> {
    int index = 0;
    public List<T> list;
    public ListIndex(List<T> list) {
        this.list = list;
    }
    public List<T> Next(int count = 1) {
        if (list.Count == 0) return list;
        var l = Enumerable.Range(index, count).Select(i => list[i%list.Count]).ToList();
        index += count;
        return l;
    }
}
