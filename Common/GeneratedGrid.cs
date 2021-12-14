using Newtonsoft.Json;
using System.Collections.Generic;

namespace Common;

public interface IGridGenerator<T> {
    public T Generate((int, int) pos);
}
public class GeneratedGrid<T> : GridTree<T> {
    [JsonIgnore]
    public QTree<T> tree;
    public IGridGenerator<T> generator;
    public GeneratedGrid(IGridGenerator<T> generator) {
        tree = new QTree<T>();
        this.generator = generator;
    }
    public ref T this[int x, int y] => ref Initialize(x, y);
    public T Get(int x, int y) {
        return Initialize(x, y);
    }

    public ref T At(int x, int y) {
        return ref Initialize(x, y);
    }
    public ref T Initialize(int x, int y) {
        ref var t = ref tree.At(x, y);
        if (EqualityComparer<T>.Default.Equals(t, default(T))) {
            t = generator.Generate((x, y));
        }
        return ref t;
    }
    public void Set(int x, int y, T t) => tree.Set(x, y, t);
    public bool IsInit(int x, int y) {
        return !EqualityComparer<T>.Default.Equals(tree.Get(x, y), default(T));
    }
}
