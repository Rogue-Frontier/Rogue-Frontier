using System.Collections.Generic;

namespace Common;

public interface IContainer<T> {
    T Value { get; }
}
public class Dict<T, U> : IContainer<Dictionary<T, U>> {
    public Dictionary<T, U> Value { get; private set; }
    public U this[T key] {
        get => Value[key];
        set => Value[key] = value;
    }
    public Dict() {
        Value = new Dictionary<T, U>();
    }
    public Dict(Dictionary<T, U> Value) {
        this.Value = Value;
    }
    public bool TryGetValue(T key, out U result) => Value.TryGetValue(key, out result);
    public static implicit operator Dictionary<T, U>(Dict<T, U> d) => d.Value;
}
public class Container<T> : IContainer<T> {
    public T Value { get; set; }
    public Container(T Value) {
        this.Value = Value;
    }
    public static implicit operator T(Container<T> c) => c.Value;
}
public class FuncSet<T> {
    public HashSet<T> set = new HashSet<T>();
    public static FuncSet<T> operator -(FuncSet<T> f, T t) {
        f.set.Remove(t);
        return f;
    }
    public static FuncSet<T> operator +(FuncSet<T> f, T t) {
        f.set.Add(t);
        return f;
    }
    public static implicit operator HashSet<T>(FuncSet<T> f) => f.set;
}
