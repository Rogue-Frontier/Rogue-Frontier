using System;
using System.Collections.Generic;
using System.Text;

namespace Common;
public interface IAtt { }
public class Req : Attribute, IAtt {}
public class Opt : Attribute, IAtt { }