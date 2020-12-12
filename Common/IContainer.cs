using System;
using System.Collections.Generic;
using System.Text;

namespace Common {
    public interface IContainer<T> {
        T Value { get; }
    }
    public class Container<T> : IContainer<T> {
        public T Value { get; set; }
        public Container(T Value) {
            this.Value = Value;
        }
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
}
