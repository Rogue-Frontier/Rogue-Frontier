using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common {
    public class DictCounter<T> {
        public Dictionary<T, int> dict;
        public int this[T t] {
            get => dict.TryGetValue(t, out int result) ? result : 0;
            set => dict[t] = value;
        }
        public DictCounter() {
            dict = new Dictionary<T, int>();
        }
        public void Add(T t) {
            if(dict.ContainsKey(t)) {
                dict[t]++;
            } else {
                dict[t] = 1;
            }
        }
    }
}
