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
    public T item => index < list.Count ? list[index] : default;
    public bool any => list.Any();
    public void Reset() => _index = 0;
    public void Skip(IEnumerable<T> obj) {
        if(list.Count == 0) {
            return;
        }
        CycleWhile(obj.Contains);
    }
    public bool CycleWhile(Func<T, bool> filter) {
        for (int i = 0; i < list.Count; i++) {
            if (filter(item)) {
                index++;
            } else {
                return true;
            }
        }
        return false;
    }

    public bool Has(out T t) { t = item; return list.Any(); }
    public ListIndex(List<T> list) {
        this.list = list;
    }

    public void Adjust(T item) {
        var i = list.IndexOf(item);
        if (i > -1) {
            _index = i;
        } else {
            _index = list.Count > 0 ? Math.Clamp(_index, 0, list.Count - 1) : 0;
        }
    }
    public List<T> GetNext(int count = 1) {
        if (list.Count == 0) return list;
        var l = Enumerable.Range(index, count).Select(i => list[i%list.Count]).ToList();
        index += count;
        return l;
    }
    public T GetNext() {
        var f = list[index];
        index++;
        return f;
    }
    public T GetFirstOrNext(Func<T, bool> f) => list.FirstOrDefault(f) ?? GetNext();
    public static ListIndex<T> operator+(ListIndex<T> i, int n) {
        i.index += n;
        return i;
    }
    public static ListIndex<T> operator++(ListIndex<T> i) {
        i.index++;
        return i;
    }
    public IEnumerable<T> GetEnumerable() {
        if(list.Count == 0) {
            yield break;
        }
        var start = Math.Min(index, list.Count);
        for(int i = start; i < list.Count; i++) {
            yield return list[i];
        }
        for(int i = 0; i < start; i++) {
            yield return list[i];
        }
    }
}
