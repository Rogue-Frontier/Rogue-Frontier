using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using static RogueFrontier.Alignment;
using static RogueFrontier.Disposition;
namespace RogueFrontier;

public enum Alignment {
    ConstructiveOrder, ConstructiveChaos, Neutral, DestructiveOrder, DestructiveChaos
}
public class Sovereign : IDesignType {
    public class AutoEnemySelf : IContainer<AutoSovereign> {
        public Sovereign self;
        public AutoEnemySelf(Sovereign self) {
            this.self = self;
        }
        [JsonIgnore]
        public AutoSovereign Value => s => s == self ? Disposition.Friend : Disposition.Enemy;
    }
    public class AutoEnemy : IContainer<AutoSovereign> {
        [JsonIgnore]
        public AutoSovereign Value => s => Disposition.Enemy;
    }
    public class AutoNeutral : IContainer<AutoSovereign> {
        [JsonIgnore]
        public AutoSovereign Value => s => Disposition.Neutral;
    }

    public static readonly Sovereign Inanimate = new Sovereign() {
        codename = "sovereign_inanimate",
        AutoSovereignDisposition = new AutoNeutral()
    };
    public static readonly Sovereign Gladiator = new Sovereign() {
        codename = "sovereign_gladiator",
        AutoSovereignDisposition = new AutoEnemy(),
    };
    public static Sovereign SelfOnly {
        get {
            Sovereign s = new Sovereign() {
                codename = "sovereign_self",
            };
            s.AutoSovereignDisposition = new AutoEnemySelf(s);
            return s;
        }
    }



    public string codename;
    public Alignment alignment;
    //private Sovereign parent;

    public Dict<string, Disposition> sovDispositions;
    public Dict<Entity, Disposition> entityDispositions;
    public IContainer<AutoSovereign> AutoSovereignDisposition;

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
        sovDispositions = new Dict<string, Disposition>();
        entityDispositions = new Dict<Entity, Disposition>();
    }
    public void Initialize(TypeCollection tc, XElement e) {
        codename = e.ExpectAtt("codename");
        if (Enum.TryParse<Alignment>(e.ExpectAtt("alignment"), out Alignment alignment)) {
            this.alignment = alignment;
        } else {
            throw new Exception($"Invalid alignment value {e.ExpectAtt("alignment")}");
        }

        if (e.HasElement("Relations", out var xmlRelations)) {
            foreach (var xmlRel in xmlRelations.Elements()) {
                var other = xmlRel.ExpectAtt("target");
                var disposition = xmlRel.ExpectAttEnum<Disposition>("disposition");
                var mutual = xmlRel.ExpectAttBool("mutual");

                var sov = tc.Lookup<Sovereign>(other);
                sovDispositions[sov.codename] = disposition;
                if (mutual) {
                    sov.sovDispositions[this.codename] = disposition;
                }
            }
        }

    }
    public void SetDisposition(Sovereign other, Disposition d) => sovDispositions[other.codename] = d;
    public void SetDisposition(SpaceObject other, Disposition d) => entityDispositions[other] = d;
    public Disposition GetDisposition(Sovereign other) {
        if (sovDispositions.TryGetValue(other.codename, out Disposition d)
            //|| (parent?.sovDispositions.TryGetValue(other, out d) == true)
            ) {
            return d;
        } else {
            sovDispositions[other.codename] = GetAutoDisposition(other);
            return sovDispositions[other.codename];
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

        throw new Exception("Unknown sovereign");
        //entityDispositions[other] = GetAutoDisposition(other.sovereign);
        //return entityDispositions[other];
    }
    public bool IsFriend(SpaceObject other) => GetDisposition(other) == Disposition.Friend;
    public bool IsEnemy(SpaceObject other) => GetDisposition(other) == Disposition.Enemy;
    public Disposition GetAutoDisposition(Sovereign other) {
        var d = AutoSovereignDisposition?.Value?.Invoke(other);
        if (d.HasValue) {
            return d.Value;
        } else {
            //Initialize from default values given our alignments
            return dispositionTable[alignment][other.alignment];
        }
    }
}
public enum Disposition {
    Neutral,
    Friend,
    Enemy
}
