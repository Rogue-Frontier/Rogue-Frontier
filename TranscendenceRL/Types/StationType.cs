using Common;
using System.Collections.Generic;
using System.Xml.Linq;
using SadRogue.Primitives;
using Color = SadRogue.Primitives.Color;

namespace TranscendenceRL {
	public class StationType : DesignType {
		public string codename;
		public string name;
		public int hp;
		public Sovereign Sovereign;
		public StaticTile tile;
		public WeaponList weapons;
		public List<SegmentDesc> segments;
		public ShipList guards;
		
		public string[] heroImage;
		public Color heroImageTint;

		public SceneDesc scene;

		public void Initialize(TypeCollection collection, XElement e) {
			codename = e.ExpectAttribute("codename");
			name = e.ExpectAttribute("name");
			hp = e.ExpectAttributeInt("hp");
			Sovereign = collection.Lookup<Sovereign>(e.ExpectAttribute("sovereign"));
			tile = new StaticTile(e);
			segments = new List<SegmentDesc>();
			if (e.HasElement("Segments", out var xmlSegments)) {
				foreach (var xmlSegment in xmlSegments.Elements()) {
					switch (xmlSegment.Name.LocalName) {
						case "Ring":
							string foreground = xmlSegment.TryAttribute("foreground", "White");
							string background = xmlSegment.TryAttribute("foreground", "Black");
							segments.AddRange(CreateRing(foreground, background));
							break;
						case "Point":
							segments.Add(new SegmentDesc(xmlSegment));
							break;
					}
				}
			}
			if(e.HasElement("Weapons", out var xmlWeapons)) {
				weapons = new WeaponList(xmlWeapons);
			}
			if(e.HasElement("Guards", out var xmlGuards)) {
				guards = new ShipList(xmlGuards);
			}
			if (e.HasElement("HeroImage", out var heroImage)) {
				this.heroImage = heroImage.Value.Replace("\r\n", "\n").Split('\n');
				this.heroImageTint = heroImage.TryAttributeColor("tint", Color.White);
			}
		}
		public static List<SegmentDesc> CreateRing(string foreground = "White", string background = "Black") {
			SegmentDesc Create(int x, int y, char c) {
				return new SegmentDesc(new XY(x, y), new StaticTile(c, foreground, background));
			}
			return new List<SegmentDesc> {
								Create(0, 1, '-'),
								Create(1, 1, '\\'),
								Create(1, 0, '|'),
								Create(1, -1, '/'),
								Create(0, -1, '-'),
								Create(-1, -1, '\\'),
								Create(-1, 0, '|'),
								Create(-1, 1, '/')
							};
		}

		public class SegmentDesc {
			public XY offset;
			public StaticTile tile;
			public SegmentDesc(XY offset, StaticTile tile) {
				this.offset = offset;
				this.tile = tile;
			}
			public SegmentDesc(XElement e) {
				var x = e.ExpectAttributeDouble("offsetX");
				var y = e.ExpectAttributeDouble("offsetY");
				offset = new XY(x, y);
				tile = new StaticTile(e);
			}
		}
	}
}
