using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class GeneratedGrid<T> : GridTree<T> {
        QTree<T> tree;
        Func<(int, int), T> generator;
        public GeneratedGrid(Func<(int, int), T> generator) {
            tree = new QTree<T>();
            this.generator = generator;
        }
        public T Get(int x, int y) {
            return Initialize(x, y);
        }

        public ref T At(int x, int y) {
            return ref Initialize(x, y);
        }
        public ref T Initialize(int x, int y) {
            ref var t = ref tree.At(x, y);
            if (EqualityComparer<T>.Default.Equals(t, default(T))) {
                t = generator((x, y));
            }
            return ref t;
        }
        public void Set(int x, int y, T t) => tree.Set(x, y, t);
        public bool IsInit(int x, int y) {
            return !EqualityComparer<T>.Default.Equals(tree.Get(x, y), default(T));
        }
    }
}
