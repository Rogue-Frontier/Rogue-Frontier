using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TranscendenceRL {
    class ElementReader {
        HashSet<string> attributesRead;
        HashSet<string> attribuesMissing;
        HashSet<string> subelementsRead;
        HashSet<string> subelementsTooMany;
        HashSet<string> subelementsMissing;
        XElement e;
        public ElementReader(XElement e) {
            this.e = e;
        }
        public bool HasOneElement(string element, out XElement result) {
            if(e.HasElements(element, out var subelements)) {
                if(subelements.Count() == 1) {
                    result = subelements.First();
                    subelementsRead.Add(element);
                    return true;
                } else {
                    result = subelements.First();
                    subelementsTooMany.Add(element);
                    return true;
                }
            } else {
                result = null;
                subelementsMissing.Add(element);
                return false;
            }
        }
    }
}
