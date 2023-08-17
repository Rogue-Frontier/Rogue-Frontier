using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Common;
public interface IAtt { }
public interface IStr : IAtt {
    public string separator { get; }
}
public class Req : Attribute, IStr {
    public string separator { get; }
    public Req(string separator = "") {
        this.separator = separator;
    }
}
public class Opt : Attribute, IStr {
    public string separator { get; }
    public Opt(string separator = "") {
        this.separator = separator;
    }
}

public class Sub : Attribute, IAtt {
    public bool required, multiple;
    public Sub(bool required = false, bool multiple = false) {
        this.required = required;
        this.multiple = multiple;
    }
}