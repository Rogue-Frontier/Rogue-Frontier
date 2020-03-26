using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static TranscendenceRL.Alignment;
using static TranscendenceRL.Disposition;
namespace TranscendenceRL {
    public enum Alignment {
        ConstructiveOrder, ConstructiveChaos, Neutral, DestructiveOrder, DestructiveChaos
    }
    public class Sovereign : DesignType {
        string codename;
        Alignment alignment;
        //private Sovereign parent;
        private Dictionary<Sovereign, Disposition> sovDispositions;
        private Dictionary<Entity, Disposition> entityDispositions;

        public static readonly Dictionary<Alignment, Dictionary<Alignment, Disposition>> dispositionTable = new Dictionary<Alignment, Dictionary<Alignment, Disposition>> {
            { ConstructiveOrder, new Dictionary<Alignment, Disposition>{
                {ConstructiveOrder, Friend },
                {ConstructiveChaos, Disposition.Neutral },
                {Alignment.Neutral, Disposition.Neutral },
                {DestructiveOrder, Enemy },
                {DestructiveChaos, Enemy },
            }}, {ConstructiveChaos, new Dictionary<Alignment, Disposition>{
                {ConstructiveOrder, Disposition.Neutral },
                {ConstructiveChaos, Disposition.Friend },
                {Alignment.Neutral, Disposition.Neutral },
                {DestructiveOrder, Enemy },
                {DestructiveChaos, Enemy }
            }}, {Alignment.Neutral, new Dictionary<Alignment, Disposition> {
                {ConstructiveOrder, Disposition.Neutral },
                {ConstructiveChaos, Disposition.Neutral },
                {Alignment.Neutral, Disposition.Neutral },
                {DestructiveOrder, Disposition.Neutral },
                {DestructiveChaos, Disposition.Enemy }
            }}, {DestructiveOrder, new Dictionary<Alignment, Disposition>{
                {ConstructiveOrder, Enemy },
                {ConstructiveChaos, Enemy },
                {Alignment.Neutral, Disposition.Neutral },
                {DestructiveOrder, Disposition.Neutral },
                {DestructiveChaos, Enemy },
            }}, {DestructiveOrder, new Dictionary<Alignment, Disposition>{
                {ConstructiveOrder, Enemy },
                {ConstructiveChaos, Enemy },
                {Alignment.Neutral, Enemy },
                {DestructiveOrder, Enemy },
                {DestructiveChaos, Enemy },
            }},
        };

        public Sovereign() {
            sovDispositions = new Dictionary<Sovereign, Disposition>();
            entityDispositions = new Dictionary<Entity, Disposition>();
        }
        public void Initialize(TypeCollection tc, XElement e) {
            codename = e.ExpectAttribute("codename");
            if(Enum.TryParse<Alignment>(e.ExpectAttribute("alignment"), out Alignment alignment)) {
                this.alignment = alignment;
            } else {
                throw new Exception($"Invalid alignment value {e.ExpectAttribute("alignment")}");
            }

        }
        public void SetDisposition(Sovereign other, Disposition d) => sovDispositions[other] = d;
        public void SetDisposition(SpaceObject other, Disposition d) => entityDispositions[other] = d;
        public Disposition GetDisposition(Sovereign other) {
            if(sovDispositions.TryGetValue(other, out Disposition d)
                //|| (parent?.sovDispositions.TryGetValue(other, out d) == true)
                ) {
                return d;
            } else {
                sovDispositions[other] = Disposition.Neutral;
                return sovDispositions[other];
            }
        }
        public Disposition GetDisposition(SpaceObject other) {
            if (entityDispositions.TryGetValue(other, out Disposition d)
                //|| (parent?.entityDispositions.TryGetValue(other, out d) == true)
                ) {
                return d;
            }
            if (other.Sovereign != null) {
                return GetDisposition(other.Sovereign);
            }
            entityDispositions[other] = Disposition.Neutral;
            return entityDispositions[other];
        }
        public Disposition InitDisposition(Sovereign other) {
            if(other == this) {
                return Friend;
            } else {
                return dispositionTable[]
            }
        }
    }
    public enum Disposition {
        Neutral,
        Friend,
        Enemy
    }
}
