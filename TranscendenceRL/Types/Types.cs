using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TranscendenceRL.Types;
using System.Runtime.InteropServices;

namespace TranscendenceRL {
	public class TypeCollection {
		public Dictionary<string, XElement> sources;
        public Dictionary<string, DesignType> all;
		public Dictionary<string, GenomeType> genomeType;
		public Dictionary<string, ImageType> imageType;
		public Dictionary<string, ItemType> itemType;
		public Dictionary<string, PowerType> powerType;
		public Dictionary<string, SceneType> sceneType;
		public Dictionary<string, ShipClass> shipClass;
		public Dictionary<string, Sovereign> sovereign;
		public Dictionary<string, StationType> stationType;
		public Dictionary<string, SystemType> systemType;

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
			genomeType = new Dictionary<string, GenomeType>();
			imageType = new Dictionary<string, ImageType>();
			itemType = new Dictionary<string, ItemType>();
			powerType = new Dictionary<string, PowerType>();
			sceneType = new Dictionary<string, SceneType>();
			shipClass = new Dictionary<string, ShipClass>();
			stationType = new Dictionary<string, StationType>();
			sovereign = new Dictionary<string, Sovereign>();
			systemType = new Dictionary<string, SystemType>();
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
				ProcessRoot("", m);
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
				ProcessRoot(m, XElement.Parse(File.ReadAllText(m)));
			}
			if(state == InitState.Uninitialized) {
				//We do two passes
				//The first pass creates DesignType references for each type and stores the source code

				//The second pass initializes each type from the source code
				Initialize();
			}
		}
		void ProcessRoot(string file, XElement root) {
			foreach (var element in root.Elements()) {
				ProcessElement(file, element);
			}
		}
		public void ProcessElement(string file, XElement element) {
			switch (element.Name.LocalName) {
				case "Module":
					var subfile = Path.Combine(Directory.GetParent(file).FullName, element.ExpectAttribute("file"));
					XElement module = XDocument.Load(subfile).Root;
					ProcessRoot(file, module);
					break;
                case "Source":
                    AddSource(element);
                    break;
				case "GenomeType":
					AddType<GenomeType>(element);
					break;
				case "ImageType":
					AddType<ImageType>(element);
					break;
				case "ItemType":
					AddType<ItemType>(element);
					break;
				case "PowerType":
					AddType<PowerType>(element);
					break;
				case "SceneType":
					AddType<SceneType>(element);
					break;
				case "ShipClass":
					AddType<ShipClass>(element);
					break;
				case "StationType":
					AddType<StationType>(element);
					break;
				case "Sovereign":
					AddType<Sovereign>(element);
					break;
				case "SystemType":
					AddType<SystemType>(element);
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
				T t = new T();
				all[type] = t;
                switch(t) {
					case GenomeType gn:
						genomeType[type] = gn;
						break;
					case ImageType im:
						imageType[type] = im;
						break;
					case ItemType it:
						itemType[type] = it;
						break;
					case PowerType pt:
						powerType[type] = pt;
						break;
					case SceneType st:
						sceneType[type] = st;
						break;
					case StationType st:
                        stationType[type] = st;
                        break;
					case ShipClass sc:
						shipClass[type] = sc;
						break;
					case Sovereign sv:
						sovereign[type] = sv;
						break;
					case SystemType ss:
						systemType[type] = ss;
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
		public T Lookup<T>(string codename) where T:DesignType {
			if(all.TryGetValue(codename, out var result)) {
				if(result is T t) {
					return t;
				} else {
					throw new Exception($"Type {codename} is <{result.GetType().Name}>, not <{nameof(T)}>");
				}
			} else {
				throw new Exception($"Unknown type {codename}");
			}
		}
    }
	public interface DesignType {
		void Initialize(TypeCollection collection, XElement e);
	}
	public class StaticTile {
		public ColoredGlyph Glyph { get; private set; }
		public StaticTile(XElement e) {
			char c = e.TryAttributeChar("char", '?');
			Color foreground = e.TryAttributeColor("foreground", Color.White);
			Color background = e.TryAttributeColor("background", Color.Transparent);

			Glyph = new ColoredGlyph(foreground, background, c);
		}
		public StaticTile(char c) {
			Glyph = new ColoredGlyph(Color.White, Color.Black, c);
		}
		public StaticTile(char c, string foreground, string background) {
			var fore = (Color)typeof(Color).GetProperty(foreground).GetValue(null, null);
			var back = (Color)typeof(Color).GetProperty(background).GetValue(null, null);
			Glyph = new ColoredGlyph(fore, back, c);
		}
		public StaticTile(ColoredGlyph Glyph) {
			this.Glyph = Glyph;
		}
	}
}
