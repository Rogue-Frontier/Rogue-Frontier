using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Common;
public interface IAtt { }
public class Req : Attribute, IAtt {}
public class Opt : Attribute, IAtt { }