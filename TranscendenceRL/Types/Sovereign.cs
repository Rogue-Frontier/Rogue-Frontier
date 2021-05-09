using Common;
using Newtonsoft.Json;
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
        public class AutoEnemySovereignSelf : IContainer<AutoSovereign> {
            public Sovereign self;
            public AutoEnemySovereignSelf(Sovereign self) {
                this.self = self;
            }
            [JsonIgnore]
            public AutoSovereign Value => s => s == self ? Disposition.Friend : Disposition.Enemy;
        }
        public class AutoEnemySovereign : IContainer<AutoSovereign> {
            [JsonIgnore]
            public AutoSovereign Value => s => Disposition.Enemy;
        }
        public class AutoEnemySpaceObject : IContainer<AutoSpaceObject> {
            [JsonIgnore]
            public AutoSpaceObject Value => s => Disposition.Enemy;
        }

        public static readonly Sovereign Inanimate = new Sovereign() {
            alignment = Alignment.Neutral
        };
        public static readonly Sovereign Gladiator = new Sovereign() {
            codename = "sovereign_gladiator",
            AutoSovereignDisposition = new AutoEnemySovereign(),
            AutoSpaceObjectDisposition = new AutoEnemySpaceObject()
        };
        public static Sovereign SelfOnly { get {
                Sovereign s = new Sovereign() {
                    codename = "sovereign_self",
                    AutoSpaceObjectDisposition = new AutoEnemySpaceObject()
                };
                s.AutoSovereignDisposition = new AutoEnemySovereignSelf(s);
                return s;
        } }



        public string codename;
        public Alignment alignment;
        //private Sovereign parent;

        public Dict<Sovereign, Disposition> sovDispositions;
        public Dict<Entity, Disposition> entityDispositions;
        public IContainer<AutoSovereign> AutoSovereignDisposition;
        public IContainer<AutoSpaceObject> AutoSpaceObjectDisposition;

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
            }}, {DestructiveChaos, new Dictionary<Alignment, Disposition>{
                {ConstructiveOrder, Enemy },
                {ConstructiveChaos, Enemy },
                {Alignment.Neutral, Enemy },
                {DestructiveOrder, Enemy },
                {DestructiveChaos, Enemy },
            }},
        };

        public delegate Disposition AutoSovereign(Sovereign other);
        public delegate Disposition AutoSpaceObject(SpaceObject other);

        public Sovereign() {
            sovDispositions = new Dict<Sovereign, Disposition>();
            entityDispositions = new Dict<Entity, Disposition>();
        }
        public void Initialize(TypeCollection tc, XElement e) {
            codename = e.ExpectAttribute("codename");
            if(Enum.TryParse<Alignment>(e.ExpectAttribute("alignment"), out Alignment alignment)) {
                this.alignment = alignment;
            } else {
                throw new Exception($"Invalid alignment value {e.ExpectAttribute("alignment")}");
            }

            if(e.HasElement("Relations", out var xmlRelations)) {
                foreach(var xmlRel in xmlRelations.Elements()) {
                    var other = xmlRel.ExpectAttribute("target");
                    var disposition = Enum.Parse<Disposition>(xmlRel.ExpectAttribute("disposition"));
                    var mutual = xmlRel.ExpectAttributeBool("mutual");

                    var sov = tc.sovereign[other];
                    sovDispositions[sov] = disposition;
                    if (mutual) {
                        sov.sovDispositions[this] = disposition;
                    }
                }
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
                sovDispositions[other] = GetAutoDisposition(other);
                return sovDispositions[other];
            }
        }
        public bool IsEnemy(Sovereign other) => GetDisposition(other) == Disposition.Enemy;
        public Disposition GetDisposition(SpaceObject other) {
            if (entityDispositions.TryGetValue(other, out Disposition d)
                //|| (parent?.entityDispositions.TryGetValue(other, out d) == true)
                ) {
                return d;
            }
            if (other.sovereign != null) {
                return GetDisposition(other.sovereign);
            }
            entityDispositions[other] = GetAutoDisposition(other);
            return entityDispositions[other];
        }
        public bool IsFriend(SpaceObject other) => GetDisposition(other) == Disposition.Friend;
        public bool IsEnemy(SpaceObject other) => GetDisposition(other) == Disposition.Enemy;
        public Disposition GetAutoDisposition(Sovereign other) {
            var d = AutoSovereignDisposition?.Value?.Invoke(other);
            if(d.HasValue) {
                return d.Value;
            } else if (other == this) {
                //We don't fight ourselves (usually)
                return Friend;
            } else {
                //Initialize from default values given our alignments
                return dispositionTable[alignment][other.alignment];
            }
        }
        public Disposition GetAutoDisposition(SpaceObject other) {
            return AutoSpaceObjectDisposition?.Value?.Invoke(other) ?? Disposition.Neutral;
        }
    }
    public enum Disposition {
        Neutral,
        Friend,
        Enemy
    }
}
