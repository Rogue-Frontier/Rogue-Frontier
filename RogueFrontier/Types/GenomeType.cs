using Common;
using System.Xml.Linq;

namespace RogueFrontier;

public class GenomeType : DesignType {
    public string name, species, gender, subjective, objective, possessiveAdj, possessiveNoun, reflexive;
    public void Initialize(TypeCollection collection, XElement e) {
        name = e.ExpectAtt(nameof(name));
        species = e.ExpectAtt(nameof(species));
        gender = e.ExpectAtt(nameof(gender));
        subjective = e.ExpectAtt(nameof(subjective));
        objective = e.ExpectAtt(nameof(objective));
        possessiveAdj = e.ExpectAtt(nameof(possessiveAdj));
        possessiveNoun = e.ExpectAtt(nameof(possessiveNoun));
        reflexive = e.ExpectAtt(nameof(reflexive));
    }
}
