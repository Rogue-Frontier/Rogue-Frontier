using Common;
using System.Xml.Linq;

namespace RogueFrontier;

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
