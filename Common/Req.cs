using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Common;
public interface IXML { }
public interface IAtt : IXML {
    /// <summary>
    /// Split the value according to <c>separator</c> before parsing
    /// </summary>
    public string separator { get; }
    /// <summary>
    /// Automatically parse the value and pass the result into <c>convert</c>. Otherwise, pass the raw value into <c>convert</c>
    /// </summary>
    public bool parse { get; }
    /// <summary>
    /// Look for subelement named by <c>alias</c> instead of the variable name. Corresponding <c>convert</c> will be called under the original var name
    /// </summary>
    public string alias { get; }
}

public class Req : Attribute, IAtt {
    public string separator { get; set; } = null;
    public bool parse { get; set; } = true;
    public string alias { get; set; } = null;
}
public class Opt : Attribute, IAtt {
    public string separator { get; set; } = null;
    public bool parse { get; set; } = true;
    public string alias { get; set; } = null;
}

public interface IEle : IXML {
    /// <summary>
    /// Auto construct the object from <c>XElement</c> using reflection before passing into <c>convert</c>. Otherwise, <c>convert</c> receives the raw <c>XElement</c>
    /// </summary>
    public bool construct { get; set; }
    /// <summary>
    /// Skip reading the <c>XElement</c> if the variable is not null.
    /// </summary>
    public bool fallback { get; set; }
}
public class Self : Attribute, IEle {
    public bool construct { get; set; } = true;
    
    public bool fallback { get; set; } = false;
}
public class Sub : Attribute, IEle {
    public bool construct { set; get; } = true;
    public bool fallback { get; set; } = false;
    public bool required = false, multiple = false;
    /// <summary>
    /// Look for subelement named by <c>alias</c> instead of the variable name. The corresponding <c>convert</c> will be called under the original var name
    /// </summary>
    public string alias { get; set; }
    public Sub(bool required = false, bool multiple = false) {
        this.required = required;
        this.multiple = multiple;
    }
}