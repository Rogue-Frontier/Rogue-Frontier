using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IslandHopper {
	class ItemType {
		string name, desc;
		int mass;
		bool explosive;
		Identity identity;
		GunType gun;
		public ItemType(XElement e) {
			name = e.Attribute("name")?.Value ?? "[name]";
			desc = e.Attribute("desc")?.Value ?? "[desc]";
			mass = e.Attribute("mass")?.Value.ParseInt() ?? 0;
			explosive = e.Attribute("explosive")?.Value.ParseBool(false) ?? false;
			identity = new Identity(e);
			if(e.Element("gun") is var g && g != null) {
				gun = new GunType(g);
			}
		}
		class Identity {
			int knownChance;
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
			}
		}
		class GunType {
			enum ProjectileType {
				beam, bullet
			}
			enum Difficulty {
				none = 0,
				easy = 20,
				medium = 40,
				hard = 60,
				expert = 80
			}
			ProjectileType projectile;
			int recoil;
			int difficulty;
			int noise;
			public GunType(XElement e) {
				if(!Enum.TryParse(e.Attribute("projectile")?.Value ?? "", out projectile)) {
					projectile = ProjectileType.bullet;
				}
				recoil = e.Attribute("recoil")?.Value.ParseInt() ?? 0;
				if(Enum.TryParse(e.Attribute("difficulty")?.Value ?? "", out Difficulty d)) {
					difficulty = (int) d;
				}
				noise = e.Attribute("noise")?.Value.ParseInt() ?? 0;
			}
		}
		class Symbol {
			private string c;
			private Color background, foreground;
			public Symbol(XElement e) {
				c = e.Attribute("char")?.Value ?? "?";
				background = (Color)typeof(Color).GetProperty(e.Attribute("background")?.Value ?? "").GetValue(null, null);
				foreground = (Color)typeof(Color).GetProperty(e.Attribute("foreground")?.Value ?? "").GetValue(null, null);
			}
			public ColoredString ToColored() => new ColoredString(c, background, foreground);
		}
	}
}
