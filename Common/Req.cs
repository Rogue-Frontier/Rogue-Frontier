using System;
using System.Collections.Generic;
using System.Text;

namespace Common;
public interface IAtt { }
public class Req : Attribute, IAtt {}
/*
public class Req<T> : Attribute, IAtt {
    public Func<T, T> transform;
    public Req(Func<T, T> transform = null) {
        this.transform = transform;
    }
}
*/
public class Opt : Attribute, IAtt { }
public class Opt<T> : Attribute, IAtt {
    public T fallback;
    //public Func<T, T> transform;
    public Opt(T fallback/*, Func<T, T> transform = null*/) {
        this.fallback = fallback;
        //this.transform = transform;
    }
}