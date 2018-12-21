using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IslandHopper {
	static class Parse {
		static int ParseInt(string s, int fallback) {
			return String.IsNullOrWhiteSpace(s) ? fallback : int.TryParse(s, out int result) ? result : fallback;
		}
	}
	class TypeCollection {
		Dictionary<string, XElement> sources;
		Dictionary<string, DesignType> types;
		enum InitState {
			Uninitialized,
			Initializing,
			Initialized
		}
		InitState state;

		//After our first initialization, any types we create later must be initialized immediately. Any dependency types must already be bound
		public TypeCollection(XElement root) {
			sources = new Dictionary<string, XElement>();
			types = new Dictionary<string, DesignType>();
			state = InitState.Uninitialized;

			Debug.Print("TypeCollection created");

			//We do two passes
			//The first pass creates DesignType references for each type and stores the source code
			ProcessRoot(root);
			//The second pass initializes each type from the source code
			Initialize();
		}
		void Initialize() {
			state = InitState.Initializing;
			foreach(string key in sources.Keys.ToList()) {
				DesignType type = types[key];
				XElement source = sources[key];
				type.Initialize(this, source);
			}
			state = InitState.Initialized;
		}
		void ProcessRoot(XElement root) {
			foreach (var element in root.Elements()) {
				ProcessElement(element);
			}
		}
		public void ProcessElement(XElement element) {
			switch (element.Name.LocalName) {
				case "Module":
					XElement module = XDocument.Load(element.ExpectAttribute("file")).Root.ExpectElement("IslandHopperModule");
					ProcessRoot(module);
					break;
				case "ItemType":
					AddType<ItemType>(element);
					break;
				default:
					//throw new Exception($"Unknown element <{element.Name}>");
					Debug.Print($"Unknown element <{element.Name}>");
					break;
			}
		}
		void AddType<T>(XElement element) where T : DesignType, new() {
			if (!element.TryAttribute("type", out string type)) {
				//throw new Exception("DesignType requires type attribute");
				type = System.Guid.NewGuid().ToString();
			}

			if (sources.ContainsKey(type)) {
				throw new Exception($"DesignType type conflict: {type}");
			} else {
				Debug.Print($"Created <{element.Name} of type {type}");
				sources[type] = element;
				types[type] = new T();
				//If we're uninitialized, then we will definitely initialize later
				if(state != InitState.Uninitialized) {
					types[type].Initialize(this, sources[type]);
				}
			}
		}
		public bool Lookup(string type, out DesignType result) {
			return types.TryGetValue(type, out result);
		}
	}
	interface DesignType {
		void Initialize(TypeCollection collection, XElement e);
	}
	class ItemType : DesignType {
		string name, desc;
		int mass;
		bool explosive;

		//Identity identity;
		int knownChance;
		ItemType unknownType;

		GunType gun;

		public Item CreateItem() =>
			new Item() {
				type = this,
				Gun = gun.CreateGun()
			};

		public void Initialize(TypeCollection collection, XElement e) {
			name = e.ExpectAttribute("name");
			desc = e.ExpectAttribute("desc");
			mass = e.TryAttributeInt("mass", 0);
			explosive = e.TryAttributeBool("explosive", false);

			switch (e.Attribute("known")?.Value) {
				case "true":
					knownChance = 100;
					break;
				case "common":
					knownChance = 80;
					break;
				case "uncommon":
					knownChance = 60;
					break;
				case "rare":
					knownChance = 40;
					break;
				case "exotic":
					knownChance = 20;
					break;
				case "false":
					knownChance = 0;
					break;
			}
			string unknownType = e.TryAttribute("unknownType");
			if (!string.IsNullOrWhiteSpace(unknownType)) {
				if (collection.Lookup(unknownType, out DesignType d) && d is ItemType it) {
					this.unknownType = it;
				} else {
					throw new Exception($"Unknown DesignType: {unknownType}");
				}
			}

			//If we have a gun, initialize it now
			if (e.HasElement("gun", out XElement g)) {
				gun = new GunType(g);
			}
			//Initialize our nested types now (they are not accessible to anyone else at bind time)
			foreach(var inner in e.Elements("ItemType")) {
				collection.ProcessElement(inner);
			}
		}
		public class GunType {
			public Gun CreateGun() => null;
			enum ProjectileType {
				beam, bullet
			}
			Dictionary<string, int> difficultyMap = new Dictionary<string, int> {
				{ "none", 0 },
				{ "easy", 20 },
				{ "medium", 40 },
				{ "hard", 60 },
				{ "expert", 80 },
				{ "master", 100 },
			};
			ProjectileType projectile;
			int recoil;
			int difficulty;
			int noiseRange;
			int damage;
			int speed;

			public GunType(XElement e) {
				if(!Enum.TryParse(e.TryAttribute("projectile"), out projectile)) {
					projectile = ProjectileType.bullet;
				}
				recoil = e.TryAttributeInt("recoil", 0);
				difficulty = difficultyMap.TryLookup(e.TryAttribute("difficulty"), 0);
				noiseRange = e.TryAttributeInt("noise", 0);
				damage = e.TryAttributeInt("damage", 0);
				speed = e.TryAttributeInt("speed", 0);
			}
		}
		class Symbol {
			private string c;
			private Color background, foreground;
			public Symbol(XElement e) {
				c = e.TryAttribute("char", "?");
				background = (Color)typeof(Color).GetProperty(e.TryAttribute("background", "black")).GetValue(null, null);
				foreground = (Color)typeof(Color).GetProperty(e.TryAttribute("foreground", "red")).GetValue(null, null);
			}
			public ColoredString String => new ColoredString(c, background, foreground);
		}
	}
}
