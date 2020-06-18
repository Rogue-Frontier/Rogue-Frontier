using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TranscendenceRL {
    public class GenomeType : DesignType {
        public string name, species, gender, subjective, objective, possessiveAdj, possessiveNoun, reflexive;
        public void Initialize(TypeCollection collection, XElement e) {
            name = e.ExpectAttribute(nameof(name));
            species = e.ExpectAttribute(nameof(species));
            gender = e.ExpectAttribute(nameof(gender));
            subjective = e.ExpectAttribute(nameof(subjective));
            objective = e.ExpectAttribute(nameof(objective));
            possessiveAdj = e.ExpectAttribute(nameof(possessiveAdj));
            possessiveNoun = e.ExpectAttribute(nameof(possessiveNoun));
            reflexive = e.ExpectAttribute(nameof(reflexive));
        }
    }
}
