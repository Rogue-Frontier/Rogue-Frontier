using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace IslandHopper {
	public static class Helper {
		public static bool CalcAim(Point3 difference, double speed, out double lower, out double higher) {
			double horizontal = difference.xy.Magnitude;
			double vertical = difference.z;
			const double g = 9.8;
			double part1 = speed * speed;
			double part2 = Math.Sqrt(Math.Pow(speed, 4) - g * ((g * horizontal * horizontal) + (2 * vertical * speed * speed)));

			if (double.IsNaN(part2)) {
				lower = higher = 0;
				return false;
			} else {
				lower = Math.Atan2(part1 - part2, (g * horizontal));
				higher = Math.Atan2(part1 + part2, (g * horizontal));
				return true;
			}
		}
		public static bool CalcAim2(Point3 difference, double speed, out Point3 lower, out Point3 higher) {
			if (CalcAim(difference, speed, out var lowerAltitude, out var upperAltitude)) {
				double azimuth = difference.xy.Angle;
				lower = new Point3(speed * Math.Cos(azimuth) * Math.Cos(lowerAltitude), speed * Math.Sin(azimuth) * Math.Cos(lowerAltitude), speed * Math.Sin(lowerAltitude));
				higher = new Point3(speed * Math.Cos(azimuth) * Math.Cos(upperAltitude), speed * Math.Sin(azimuth) * Math.Cos(upperAltitude), speed * Math.Sin(upperAltitude));
				return true;
			} else {
				lower = null;
				higher = null;
				return false;
			}
		}
		public static bool InRange(double n, double center, double maxDistance) {
			return n > center - maxDistance && n < center + maxDistance;
		}
		public static bool AreKeysPressed(this SadConsole.Input.Keyboard keyboard, params Keys[] keys) {
			foreach (var key in keys) {
				if (!keyboard.IsKeyPressed(key))
					return false;
			}
			return true;
		}
		/*
		public static bool InRange(double n, double min, double max) {
			return n > min && n < max;
		}
		*/
		public static int LineLength(this string lines) {
			return lines.IndexOf('\n');
		}
		public static int LineCount(this string lines) {
			/*
			int i = 0;
			int result = 1;
			while((i = lines.IndexOf('\n', i)) != -1) {
				result++;
			}
			return result;
			*/
			return lines.Split('\n').Length;
		}
		public static T LastItem<T>(this List<T> list) => list[list.Count - 1];
		public static T FirstItem<T>(this List<T> list) => list[0];
		public static string FlipLines(this string s) {
			var lines = new List<string>(s.Split('\n'));
			lines.Reverse();
			StringBuilder result = new StringBuilder(s.Length - s.LineCount());
			for (int i = 0; i < lines.Count - 1; i++) {
				result.AppendLine(lines[i]);
			}
			result.Append(lines.LastItem());
			return result.ToString();
		}
		public static void PrintLines(this SadConsole.Console console, int x, int y, string lines, Color? foreground = null, Color? background = null, SpriteEffects? mirror = null) {
			foreach (var line in lines.Replace("\r\n", "\n").Split('\n')) {
				console.Print(x, y, line, foreground, background, mirror);
				y++;
			}
		}
		//This function calculates all the points on a hollow cube of given radius around an origin of (0, 0, 0)
		public static List<Point3> GetSurrounding(int radius) {
			//Cover all the corners
			var result = new List<Point3>() {
				new Point3( radius,  radius,  radius),	//NE Upper
				new Point3( radius,  radius, -radius),	//NE Lower
				new Point3( radius, -radius,  radius),	//SE Upper
				new Point3( radius, -radius, -radius),	//SE Lower
				new Point3(-radius,  radius,  radius),	//NW Uper
				new Point3(-radius,  radius, -radius),	//NW Lower
				new Point3(-radius, -radius,  radius),	//SW Upper
				new Point3(-radius, -radius, -radius),	//SW Lower
			};
			//Fill in the sides of the cube
			for(int y = -radius+1; y < radius; y++) {
				for(int z = -radius+1; z < radius; z++) {
					result.Add(new Point3(radius, y, z));	//East side
					result.Add(new Point3(-radius, y, z));	//West side
				}
				//Add the top/bottom edges of each side
				result.Add(new Point3(radius, y, radius));
				result.Add(new Point3(radius, y, -radius));
				result.Add(new Point3(-radius, y, radius));
				result.Add(new Point3(-radius, y, -radius));
			}
			for(int x = -radius+1; x < radius; x++) {
				for(int z = -radius+1; z < radius; z++) {
					result.Add(new Point3(x,  radius, z));   //North side
					result.Add(new Point3(x, -radius, z));   //South side
				}

				result.Add(new Point3(x, radius, radius));	//North upper
				result.Add(new Point3(x, radius, -radius));	//North lower
				result.Add(new Point3(x, -radius, radius));	//South upper
				result.Add(new Point3(x, -radius, -radius));//South lower
			}
			for (int x = -radius+1; x < radius; x++) {
				for (int y = -radius+1; y < radius; y++) {
					result.Add(new Point3(x, y,  radius));   //Top side
					result.Add(new Point3(x, y, -radius));   //Bottom side
				}
			}
			//Vertical
			for(int z = -radius+1; z < radius; z++) {
				result.Add(new Point3(radius, radius, z));
				result.Add(new Point3(radius, -radius, z));
				result.Add(new Point3(-radius, radius, z));
				result.Add(new Point3(-radius, -radius, z));
			}
			//Sort them based on distance from center
			result.Sort((p1, p2) => {
				double d1 = (p1).Magnitude;
				double d2 = (p2).Magnitude;
				if (d1 < d2) {
					return -1;
				} else if (d1 > d2) {
					return 1;
				} else {
					return 0;
				}
			});
			return result;
		}
		public static int Amplitude(this Random random, int amplitude) => random.Next(-amplitude, amplitude);
		public static bool HasElement(this XElement e, string key, out XElement result) {
			return (result = e.Element(key)) != null;
		}
		public static string ExpectAttribute(this XElement e, string attribute) {
			string result = e.Attribute(attribute)?.Value;
			if (result == null) {
				throw new Exception($"<{e.Name}> requires {attribute} attribute");
			} else {
				return result;
			}
		}
		public static XElement ExpectElement(this XElement e, string name) {
			var result = e.Element(name);
			if (result == null) {
				throw new Exception($"Element <{e.Name}> requires subelement {name}");
			}
			return result;
		}
		public static bool TryAttribute(this XElement e, string attribute, out string result) {
			return (result = e.Attribute(attribute)?.Value) != null;
		}
		public static string TryAttribute(this XElement e, string attribute, string fallback = "") {
			return e.Attribute(attribute)?.Value ?? fallback;
		}
		public static int TryAttributeInt(this XElement e, string attribute, int fallback = 0) {
			return e.TryAttribute(attribute).ParseInt(fallback);
		}

		public static int ParseInt(this string s, int fallback = 0) {
			return int.TryParse(s, out int result) ? result : fallback;
		}
		public static int ParseIntMin(this string s, int min, int fallback = 0) {
			return Math.Max(s.ParseInt(fallback), min);
		}
		public static int ParseIntMax(this string s, int max, int fallback = 0) {
			return Math.Min(s.ParseInt(fallback), max);
		}
		public static int ParseIntBounded(this string s, int min, int max, int fallback = 0) {
			return Range(min, s.ParseInt(fallback), max);
		}
		public static int Range(int min, int max, int n) {
			return Math.Min(max, Math.Max(min, n));
		}
		public static bool ParseBool(this string s, bool fallback = false) {
			return s == "true" ? true : s == "false" ? false : fallback;
		}
		public static bool TryAttributeBool(this XElement e, string attribute, bool fallback = false) {
			return e.TryAttribute("attribute").ParseBool(fallback);
		}
		/*
		public static bool? ParseBool(this string s, bool? fallback = null) {
			switch(s) {
				case "true":
					return true;
				case "false":
					return false;
				default:
					return null;
			}
		}
		*/
		/*
		public static Func<int> ParseIntGenerator(string s) {

		}
		*/
		public static TEnum ParseEnum<TEnum>(this XAttribute a, TEnum fallback = default) where TEnum : struct {
			if (a == null) {
				return fallback;
			} else if (Enum.TryParse<TEnum>(a.Value, out TEnum result)) {
				return result;
			} else {
				return fallback;
			}

		}
		public static TValue TryLookup<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue fallback = default) {
			if (d.ContainsKey(key)) {
				return d[key];
			} else {
				return fallback;
			}
		}

		public static int CalcAccuracy(int difficulty, int skill, Random karma) {
			if (skill > difficulty) {
				return 100;
			} else {
				int miss = difficulty - skill;
				return 100 - karma.Next(miss);
			}
		}
		//Chance that the shot is blocked by an obstacle
		public static bool CalcBlocked(int coverage, int accuracy, Random karma) {
			return karma.Next(coverage) > karma.Next(accuracy);
		}
		public static ColoredGlyph Colored(char c, Color foreground, Color background) {
			ColoredGlyph result = new ColoredGlyph(new Cell(foreground, background));
			result.GlyphCharacter = c;
			return result;
		}
		public static ColoredString WithBackground(this ColoredString c, Color Background) {
			var result = c.SubString(0, c.Count);
			result.SetBackground(Color.Black);
			return result;
		}
		public static ColoredString Adjust(this ColoredString c, Color foregroundInc) {
			ColoredString result = c.SubString(0, c.Count);
			foreach (var g in result) {
				g.Foreground = Sum(g.Foreground, foregroundInc);
			}
			return result;
		}
		public static ColoredString Opacity(this ColoredString s, byte alpha) {
            var result = s.SubString(0, s.Count);
			foreach(var c in result) {
                c.Foreground.A = alpha;
            }
            return result;
		}
		public static ColoredString Brighten(this ColoredString s, int intensity) {
			return s.Adjust(new Color(intensity, intensity, intensity, 0));
		}
		public static ColoredString ToColoredString(this ColoredGlyph c) {
			return new ColoredString(new ColoredGlyph[] { c });
		}
		public static ColoredGlyph Brighten(this ColoredGlyph c, int intensity) {
			ColoredGlyph result = c.Clone();
			result.Foreground = Sum(result.Foreground, new Color(intensity, intensity, intensity, 0));
			return result;
		}
        public static ColoredString Adjust(this ColoredString c, Color foregroundInc, Color backgroundInc) {
            ColoredString result = c.SubString(0, c.Count);
            foreach (var g in result) {
                g.Foreground = Sum(g.Foreground, foregroundInc);
                g.Background = Sum(g.Background, backgroundInc);
            }
            return result;
        }
        public static ColoredGlyph Adjust(this ColoredGlyph c, Color foregroundInc) {
			ColoredGlyph result = c.Clone();
			result.Foreground = Sum(result.Foreground, foregroundInc);
			return result;
		}
		public static Color Sum(Color c, Color c2) {
			return new Color(Range(0, 255, c.R + c2.R), Range(0, 255, c.G + c2.G), Range(0, 255, c.B + c2.B), Range(0, 255, c.A + c2.A));
		}
        public static Func<Entity, bool> Or(params Func<Entity, bool>[] f) {
            Func<Entity, bool> result = e => true;
            foreach(Func<Entity, bool> condition in f) {
                if (condition == null)
                    continue;
                result = e => result(e) || condition(e);
            }
            return result;
        }
        public static T Elvis<T>(this object o, T result) {
            return o != null ? default(T) : result;
        }
	}
}

