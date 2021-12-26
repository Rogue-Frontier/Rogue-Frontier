using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RogueFrontier;
public class ListIndex<T> {
    private int _index = 0;
    public int index {
        get => _index;
        set {
            if(list.Any()) {
                _index = value;
                while(_index < 0) {
                    _index += list.Count;
                }
                _index %= list.Count;
            } else {
                _index = 0;
            }
        }
    }
    public List<T> list;
    public T item => list.Any() ? list[index] : default;
    public bool any => list.Any();

    public bool Has(out T t) { t = item; return list.Any(); }
    public ListIndex(List<T> list) {
        this.list = list;
    }
    public List<T> GetNext(int count = 1) {
        if (list.Count == 0) return list;
        var l = Enumerable.Range(index, count).Select(i => list[i%list.Count]).ToList();
        index += count;
        return l;
    }
    public static ListIndex<T> operator+(ListIndex<T> i, int n) {
        i.index += n;
        return i;
    }
    public static ListIndex<T> operator++(ListIndex<T> i) {
        i.index++;
        return i;
    }
}
