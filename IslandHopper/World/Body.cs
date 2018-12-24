using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	interface IBodyPart {
		EBodyPartType Type { get; }
		string Name { get; }
		int MaxHP { get; }
		int CurrentHP { get; set; }
		int Bleeding { get; set; }
		Item Equipped { get; set; }
	}
	interface Body {
		Dictionary<EBodyPartType, IBodyPart> parts { get; }
	}
	enum EBodyPartType {
		Head,
		Arm,
		Torso,
		Leg,
	}
	class BodyPart : IBodyPart {
		public EBodyPartType Type { get; }
		public string Name { get; private set; }
		public int MaxHP { get; private set; } = 100;
		public int CurrentHP { get; set; } = 100;
		public int Bleeding { get; set; } = 0;
		public Item Equipped { get; set; } = null;
		private static BodyPart CreateStandardPart(EBodyPartType type, string Name) => new BodyPart() { Name = Name, MaxHP = 100, CurrentHP = 100, Equipped = null };
		private static BodyPart CreateHead() => CreateStandardPart(EBodyPartType.Head, "Head");
		private static BodyPart CreateLeftArm() => CreateStandardPart(EBodyPartType.Arm, "Left Arm");
		private static BodyPart CreateRightArm() => CreateStandardPart(EBodyPartType.Arm, "Right Arm");
		private static BodyPart CreateTorso() => CreateStandardPart(EBodyPartType.Torso, "Torso");
		private static BodyPart CreateLeftLeg() => CreateStandardPart(EBodyPartType.Leg, "Left Leg");
		private static BodyPart CreateRightLeg() => CreateStandardPart(EBodyPartType.Leg, "Right Leg");
		public static HashSet<BodyPart> CreateStandardBody() => new HashSet<BodyPart>(new[] { CreateHead(), CreateLeftArm(), CreateRightArm(), CreateTorso(), CreateLeftLeg(), CreateRightLeg() });
	}
}
