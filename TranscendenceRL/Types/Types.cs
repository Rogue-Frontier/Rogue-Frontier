using Common;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static IslandHopper.IslandHelper;
namespace TranscendenceRL {
	public class TypeCollection {
		public Dictionary<string, XElement> sources;
        public Dictionary<string, DesignType> all;
		public Dictionary<string, ShipClass> shipClass;
		public Dictionary<string, StationType> stationType;
		enum InitState {
			Uninitialized,
			Initializing,
			Initialized
		}
		InitState state;

		//After our first initialization, any types we create later must be initialized immediately. Any dependency types must already be bound
		public TypeCollection() {
			sources = new Dictionary<string, XElement>();
			all = new Dictionary<string, DesignType>();
			shipClass = new Dictionary<string, ShipClass>();
			stationType = new Dictionary<string, StationType>();
			state = InitState.Uninitialized;

			Debug.Print("TypeCollection created");

		}
		public TypeCollection(params string[] modules) : this() {
			Load(modules);
		}
		public TypeCollection(params XElement[] modules) : this() {
			
			//We do two passes
			//The first pass creates DesignType references for each type and stores the source code
			foreach(var m in modules) {
				ProcessRoot(m);
			}
			
			//The second pass initializes each type from the source code
			Initialize();
		}
		void Initialize() {
			state = InitState.Initializing;
            //We don't evaluate all sources; just the ones that are used by DesignTypes
			foreach(string key in all.Keys.ToList()) {
				DesignType type = all[key];
				XElement source = sources[key];
				type.Initialize(this, source);
			}
			state = InitState.Initialized;
		}
		public void Load(params string[] modules) {
			foreach (var m in modules) {
				ProcessRoot(XElement.Parse(File.ReadAllText(m)));
			}
			if(state == InitState.Uninitialized) {
				//We do two passes
				//The first pass creates DesignType references for each type and stores the source code

				//The second pass initializes each type from the source code
				Initialize();
			}
		}
		void ProcessRoot(XElement root) {
			foreach (var element in root.Elements()) {
				ProcessElement(element);
			}
		}
		public void ProcessElement(XElement element) {
			switch (element.Name.LocalName) {
				case "Module":
					XElement module = XDocument.Load(element.ExpectAttribute("file")).Root.ExpectElement("Module");
					ProcessRoot(module);
					break;
                case "Source":
                    AddSource(element);
                    break;
				case "ShipClass":
					AddType<ShipClass>(element);
					break;
				case "StationType":
					AddType<StationType>(element);
					break;
				default:
					throw new Exception($"Unknown element <{element.Name}>");
					//Debug.Print($"Unknown element <{element.Name}>");
					//break;
			}
		}
        void AddSource(XElement element) {
            if (!element.TryAttribute("codename", out string type)) {
                throw new Exception("DesignType requires codename attribute");
            }

            if (sources.ContainsKey(type)) {
                throw new Exception($"DesignType type conflict: {type}");
            } else {
                Debug.Print($"Created Source <{element.Name}> of type {type}");
                sources[type] = element;
            }
        }
		void AddType<T>(XElement element) where T : DesignType, new() {
			if (!element.TryAttribute("codename", out string type)) {
				throw new Exception("DesignType requires codename attribute");
			}

			if (sources.ContainsKey(type)) {
				throw new Exception($"DesignType type conflict: {type}");
			} else {
				Debug.Print($"Created <{element.Name}> of type {type}");
				sources[type] = element;
				all[type] = new T();
                switch(all[type]) {
                    case StationType st:
                        stationType[type] = st;
                        break;
					case ShipClass sc:
						shipClass[type] = sc;
						break;
					default:
						throw new Exception($"Unrecorded {element.Name} of type {type}");
                }

				//If we're uninitialized, then we will definitely initialize later
				if(state != InitState.Uninitialized) {
                    //Otherwise, initialize now
					all[type].Initialize(this, sources[type]);
				}
			}
		}
		public bool Lookup(string type, out DesignType result) {
			return all.TryGetValue(type, out result);
		}

        public bool Lookup<T>(string type, out T result) where T:DesignType {
            if(all.TryGetValue(type, out var t) && t is T) {
                result = (T)t;
                return true;
            } else {
                result = default(T);
                return false;
            }
        }
		public T Lookup<T>(string type) where T:DesignType {
			return (T)all[type];
		}
    }
	public interface DesignType {
		void Initialize(TypeCollection collection, XElement e);
	}
	public class StaticTile {
		public ColoredGlyph Glyph { get; private set; }
		public StaticTile(XElement e) {
			var c = e.TryAttribute("char", "?")[0];
			Color background;
			var s = e.TryAttribute("background", "Transparent");
			try {
				background = (Color)typeof(Color).GetProperty(s).GetValue(null, null);
			} catch {
				throw new Exception($"Invalid background color {s}");
			}

			Color foreground;
			s = e.TryAttribute("foreground", "White");
			try {
				foreground = (Color)typeof(Color).GetProperty(s).GetValue(null, null);
			} catch {
				throw new Exception($"Invalid foreground color {s}");
			}
			Glyph = new ColoredGlyph(c, foreground, background);
		}
		public StaticTile(char c) {
			Glyph = new ColoredGlyph(c, Color.White, Color.Black);
		}
		public StaticTile(char c, string foreground, string background) {
			var fore = (Color)typeof(Color).GetProperty(foreground).GetValue(null, null);
			var back = (Color)typeof(Color).GetProperty(background).GetValue(null, null);
			Glyph = new ColoredGlyph(c, fore, back);
		}
		public StaticTile(ColoredGlyph Glyph) {
			this.Glyph = Glyph;
		}
	}
	public class StationType : DesignType {
		public string codename;
        public string name;
		public StaticTile tile;
		public List<SegmentDesc> segments;

		public void Initialize(TypeCollection collection, XElement e) {
			codename = e.ExpectAttribute("codename");
			name = e.ExpectAttribute("name");
			tile = new StaticTile(e);
			segments = new List<SegmentDesc>();
			if(e.HasElement("Segments", out var xmlSegments)) {
				foreach(var xmlSegment in xmlSegments.Elements()) {
					switch(xmlSegment.Name.LocalName) {
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
	public class ShipClass : DesignType {
		public string codename;
		public string name;
		public double thrust;
		public double maxSpeed;
		public double rotationMaxSpeed;
		public double rotationDecel;
		public double rotationAccel;
		public StaticTile tile;
		public DeviceList weapons;
		public PlayerSettings playerSettings;
		

		public void Initialize(TypeCollection collection, XElement e) {
			codename = e.ExpectAttribute("codename");
			name = e.ExpectAttribute("name");
			thrust = e.ExpectAttributeDouble("thrust");
			maxSpeed = e.ExpectAttributeDouble("maxSpeed");
			rotationMaxSpeed = e.ExpectAttributeDouble("rotationMaxSpeed");
			rotationDecel = e.ExpectAttributeDouble("rotationDecel");
			rotationAccel = e.ExpectAttributeDouble("rotationAccel");
			tile = new StaticTile(e);

			if(e.HasElement("Devices", out XElement xmlDevices)) {
				weapons = new DeviceList(xmlDevices);
			} else {

			}
			if(e.HasElement("PlayerSettings", out XElement xmlPlayerSettings)) {
				playerSettings = new PlayerSettings(xmlPlayerSettings);
			}
		}
		public interface DeviceGenerator {
			List<Device> Generate(TypeCollection c);
		}
		public class DeviceList : DeviceGenerator {
			List<DeviceGenerator> generators;
			public DeviceList(XElement e) {
				generators = new List<DeviceGenerator>();
				foreach(var element in e.Elements()) {
					switch(element.Name.LocalName) {
						case "Weapon":
							generators.Add(new WeaponEntry(element));
							break;
						default:
							throw new Exception($"Unknown <Devices> subelement {element.Name}");
					}
				}
			}
			public List<Device> Generate(TypeCollection tc) {
				var result = new List<Device>();
				generators.ForEach(g => result.AddRange(g.Generate(tc)));
				return result;
			}
		}
		class WeaponEntry : DeviceGenerator {
			public string codename;
			public WeaponEntry(XElement e) {
				this.codename = e.ExpectAttribute(codename);
			}
			public List<Device> Generate(TypeCollection tc) {
				if(tc.Lookup<ItemType>(codename, out var result) && result.weapon != null) {
					return new List<Device> { new Item(result).weapon };
				} else {
					throw new Exception($"Expected <ItemType> type with <Weapon> desc: {codename}");
				}
			}
			//In case we want to make sure immediately that the type is valid
			public void ValidateEager(TypeCollection tc) {

			}
		}
	}
	public class PlayerSettings {
		public bool startingClass;
		public string description;
		public string[] map;
		public PlayerSettings(XElement e) {
			startingClass = e.ExpectAttributeBool("startingClass");
			description = e.ExpectAttribute("description");
			var xmlMap = e.ExpectElement("Map");
			map = xmlMap.Value.Replace("\r\n", "\n").Split('\n');
		}
	}
}
