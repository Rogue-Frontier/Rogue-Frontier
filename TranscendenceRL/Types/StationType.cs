using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TranscendenceRL {
	public class StationType : DesignType {
		public string codename;
		public string name;
		public Sovereign Sovereign;
		public StaticTile tile;
		public List<SegmentDesc> segments;

		public void Initialize(TypeCollection collection, XElement e) {
			codename = e.ExpectAttribute("codename");
			name = e.ExpectAttribute("name");
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
