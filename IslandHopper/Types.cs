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
	class ItemType {
		string name, desc;
		int mass;
		bool explosive;
		Identity identity;
		GunType gun;
		public ItemType(XElement e) {
			name = e.TryAttribute("name", "[name]");
			desc = e.TryAttribute("desc", "[desc]");
			mass = e.TryAttributeInt("mass", 0);
			explosive = e.TryAttributeBool("explosive", false);
			identity = new Identity(e);
			if(e.HasElement("gun", out XElement g)) {
				gun = new GunType(g);
			}
		}
		class Identity {
			int knownChance;
			string unknownType;
			public Identity(XElement e) {
				switch(e.Attribute("known")?.Value) {
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
				unknownType = e.TryAttribute("unknownType");
			}
		}
		class GunType {
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
