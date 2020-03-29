using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using Common;
using IslandHopper;
using System.Text.RegularExpressions;

namespace Common {
	public static class Helper {
		public static bool CalcAim(XYZ difference, double speed, out double lower, out double higher) {
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
		public static bool CalcAim2(XYZ difference, double speed, out XYZ lower, out XYZ higher) {
			if (CalcAim(difference, speed, out var lowerAltitude, out var upperAltitude)) {
				double azimuth = difference.xy.Angle;
				lower = new XYZ(speed * Math.Cos(azimuth) * Math.Cos(lowerAltitude), speed * Math.Sin(azimuth) * Math.Cos(lowerAltitude), speed * Math.Sin(lowerAltitude));
				higher = new XYZ(speed * Math.Cos(azimuth) * Math.Cos(upperAltitude), speed * Math.Sin(azimuth) * Math.Cos(upperAltitude), speed * Math.Sin(upperAltitude));
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
		public static double Round(this double d, double interval) {
			int times = (int)(d / interval);
			var roundedDown = times * interval;
			if (d - roundedDown < interval/2) {
				return roundedDown;
			} else {
				var roundedUp = roundedDown + interval;
				return roundedUp;
			}
		}
		public static Color NextGray(this Random r, int range) {
			var value = r.Next(range);
			return new Color(value, value, value);
		}
		public static Color Noise(this Color c, Random r, double range) {
			double increaseFactor = r.NextDouble() * range;
			double multiplier = 1 + increaseFactor;
			return new Color((int)Math.Min(255, c.R * multiplier), (int)Math.Min(255, c.G * multiplier), (int)Math.Min(255, c.B * multiplier));
		}
		public static Color NextColor(this Random r, int range) => new Color(r.Next(range), r.Next(range), r.Next(range));
		public static Color Round(this Color c, int factor) => new Color(factor * (c.R / factor), factor * (c.G / factor), factor * (c.B / factor));
		public static Color Add(this Color c, int value) => c.Add(new Color(value, value, value));
		public static Color Add(this Color c1, Color c2) => new Color(Math.Min(255, c1.R + c2.R), Math.Min(255, c1.G + c2.G), Math.Min(255, c1.B + c2.B));
		public static Color Subtract(this Color c, int value) => c.Subtract(new Color(value, value, value));
		public static Color Subtract(this Color c1, Color c2) => new Color(Math.Max(0, c1.R - c2.R), Math.Max(0, c1.G - c2.G), Math.Max(0, c1.B - c2.B));
		public static Color Divide(this Color c, int scale) {
			return new Color(c.R / scale, c.G / scale, c.B / scale);
		}
		public static Color WithValues(this Color c, int? red = null, int? green = null, int? blue = null, int? alpha = null) {
			return new Color(red ?? c.R, green ?? c.G, blue ?? c.B, alpha ?? c.A);
		}
		public static double CalcFireAngle(XY posDiff, XY velDiff, double missileSpeed) {
			/*
			var timeToHit = posDiff.Magnitude / missileSpeed;
			var posFuture = posDiff + velDiff * timeToHit;

			var posDiffPrev = posDiff;
			posDiff = posFuture;
			*/

			var a = velDiff.Dot(velDiff) - missileSpeed * missileSpeed;
			var b = 2 * velDiff.Dot(posDiff);
			var c = posDiff.Dot(posDiff);

			var p = -b / (2 * a);
			var q = Math.Sqrt(b * b - 4 * a * c) / (2 * a);
			var t1 = -p - q;
			var t2 = p + q;

			var timeToHit = t1;
			if(t1 > t2 && t2 > 0) {
				timeToHit = t2;
			}
			var posFuture = posDiff + velDiff * timeToHit;
			return posFuture.Angle;
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
		public static void PrintLines(this SadConsole.Console console, int x, int y, string lines, Color? foreground = null, Color? background = null, SpriteEffects mirror = SpriteEffects.None) {
			foreach (var line in lines.Replace("\r\n", "\n").Split('\n')) {
				console.Print(x, y, line, foreground ?? Color.White, background ?? Color.Black, mirror);
				y++;
			}
		}
        public static List<XYZ> GetWithin(int radius) {
            List<XYZ> result = new List<XYZ>();
            for (int i = 0; i < radius; i++) {
                result.AddRange(GetSurrounding(i));
            }
            result = new List<XYZ>(result.Distinct(new XYZGridComparer()));
            return result;
        }
        //This function calculates all the points on a hollow cube of given radius around an origin of (0, 0, 0)
		public static List<XYZ> GetSurrounding(int radius) {
			//Cover all the corners
			var result = new List<XYZ>() {
				new XYZ( radius,  radius,  radius),	//NE Upper
				new XYZ( radius,  radius, -radius),	//NE Lower
				new XYZ( radius, -radius,  radius),	//SE Upper
				new XYZ( radius, -radius, -radius),	//SE Lower
				new XYZ(-radius,  radius,  radius),	//NW Uper
				new XYZ(-radius,  radius, -radius),	//NW Lower
				new XYZ(-radius, -radius,  radius),	//SW Upper
				new XYZ(-radius, -radius, -radius),	//SW Lower
			};
			//Fill in the sides of the cube
			for(int y = -radius+1; y < radius; y++) {
				for(int z = -radius+1; z < radius; z++) {
					result.Add(new XYZ(radius, y, z));	//East side
					result.Add(new XYZ(-radius, y, z));	//West side
				}
				//Add the top/bottom edges of each side
				result.Add(new XYZ(radius, y, radius));
				result.Add(new XYZ(radius, y, -radius));
				result.Add(new XYZ(-radius, y, radius));
				result.Add(new XYZ(-radius, y, -radius));
			}
			for(int x = -radius+1; x < radius; x++) {
				for(int z = -radius+1; z < radius; z++) {
					result.Add(new XYZ(x,  radius, z));   //North side
					result.Add(new XYZ(x, -radius, z));   //South side
				}

				result.Add(new XYZ(x, radius, radius));	//North upper
				result.Add(new XYZ(x, radius, -radius));	//North lower
				result.Add(new XYZ(x, -radius, radius));	//South upper
				result.Add(new XYZ(x, -radius, -radius));//South lower
			}
			for (int x = -radius+1; x < radius; x++) {
				for (int y = -radius+1; y < radius; y++) {
					result.Add(new XYZ(x, y,  radius));   //Top side
					result.Add(new XYZ(x, y, -radius));   //Bottom side
				}
			}
			//Vertical
			for(int z = -radius+1; z < radius; z++) {
				result.Add(new XYZ(radius, radius, z));
				result.Add(new XYZ(radius, -radius, z));
				result.Add(new XYZ(-radius, radius, z));
				result.Add(new XYZ(-radius, -radius, z));
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
		public static bool HasElements(this XElement e, string key, out IEnumerable<XElement> result) {
			return (result = e.Elements(key)) != null;
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
        public static int TryAttributeInt(this XElement e, string attribute, int fallback = 0) => TryAttributeInt(e.Attribute(attribute), fallback);
		//We expect either no value or a valid value; an invalid value gets an exception
		public static int TryAttributeInt(this XAttribute a, int fallback = 0) {
			if(a == null) {
				return fallback;
			} else if(int.TryParse(a.Value, out int result)) {
				return result;
			} else {
				throw new Exception($"int value expected: {a.Name}=\"{a.Value}\"");
			}
		}
		public static int ExpectAttributeInt(this XElement e, string attribute) {
			var a = e.Attribute(attribute);
			if (a == null) {
				throw new Exception($"<{e.Name}> requires int attribute: {attribute}");
			} else {
				return ExpectAttributeInt(a);
			}
		}
		public static int ExpectAttributeInt(this XAttribute a) {
			if(int.TryParse(a.Value, out int result)) {
				return result;
			} else {
				throw new Exception($"int value expected: {a.Name} = \"{a.Value}\"");
			}
		}

		public static double ExpectAttributeDouble(this XElement e, string attribute) {
			var a = e.Attribute(attribute);
			if (a == null) {
				throw new Exception($"<{e.Name}> requires double attribute: {attribute}");
			} else {
				return ExpectAttributeDouble(a);
			}
		}
		public static double ExpectAttributeDouble(this XAttribute a) {
			if (double.TryParse(a.Value, out double result)) {
				return result;
			} else {
				throw new Exception($"double value expected: {a.Name} = \"{a.Value}\"");
			}
		}
		public static bool ExpectAttributeBool(this XElement e, string attribute) {
			var a = e.Attribute(attribute);
			if (a == null) {
				throw new Exception($"<{e.Name}> requires bool attribute: {attribute}");
			} else {
				return ExpectAttributeBool(a);
			}
		}
		public static bool ExpectAttributeBool(this XAttribute a) {
			if (bool.TryParse(a.Value, out bool result)) {
				return result;
			} else {
				throw new Exception($"bool value expected: {a.Name} = \"{a.Value}\"");
			}
		}

		public static double TryAttributeDouble(this XElement e, string attribute, double fallback = 0) => TryAttributeDouble(e.Attribute(attribute), fallback);
        public static double TryAttributeDouble(this XAttribute a, double fallback = 0) {
            if (a == null) {
                return fallback;
            } else if (double.TryParse(a.Value, out double result)) {
                return result;
            } else {
                throw new Exception($"double value expected: {a.Name}=\"{a.Value}\"");
            }
        }
        public static void InheritAttributes(this XElement sub, XElement source) {
            foreach (var attribute in source.Attributes()) {
                if (sub.Attribute(attribute.Name) == null) {
                    sub.SetAttributeValue(attribute.Name, attribute.Value);
                }
            }
        }
		public static List<string> Wrap(this string s, int width) {
			List<string> lines = new List<string> { "" };
			foreach(var word in Regex.Split(s, $"({Regex.Escape(" ")})")) {
				if(lines.Last().Length + word.Length < width) {
					lines[lines.Count - 1] += word;
				} else {
					if(word == " ") {
						lines.Add("");
					} else {
						lines.Add(word);
					}
					
				}
			}
			return lines;
		}
		public static void PaintCentered(this Window w, string s, int x, int y) {
			w.Print(x - s.Length / 2, y, s);
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
		//We expect either no value or a valid value; an invalid value gets an exception
		public static bool TryAttributeBool(XAttribute a, bool fallback = false) {
			if(a == null) {
				return fallback;
			} else if(bool.TryParse(a.Value, out bool result)) {
				return result;
			} else {
				throw new Exception($"Bool value expected: {a.Name}=\"{a.Value}\"");
			}
		}
		public static bool TryAttributeBool(this XElement e, string attribute, bool fallback = false) {
			return e.TryAttribute(attribute).ParseBool(fallback);
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
		//We expect either no value or a valid value; an invalid value gets an exception
		public static TEnum ParseEnum<TEnum>(this XAttribute a, TEnum fallback = default) where TEnum : struct {
			if (a == null) {
				return fallback;
			} else if (Enum.TryParse<TEnum>(a.Value, out TEnum result)) {
				return result;
			} else {
				throw new Exception($"Enum value of {fallback.GetType().Name} expected: {a.Name}=\"{a.Value}\"");
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
		public static ColoredString WithBackground(this ColoredString c, Color? Background = null) {
			var result = c.SubString(0, c.Count);
			result.SetBackground(Background ?? Color.Black);
			return result;
		}
		public static ColoredString Adjust(this ColoredString c, Color foregroundInc) {
			ColoredString result = c.SubString(0, c.Count);
			foreach (var g in result) {
				g.Foreground = Sum(g.Foreground, foregroundInc);
			}
			return result;
		}
		public static ColoredString SetOpacity(this ColoredString s, byte alpha) {
			foreach(var c in s) {
                c.Foreground.A = alpha;
            }
            return s;
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
		//https://stackoverflow.com/a/12016968
		public static Color Blend(this Color background, Color foreground) {
			byte alpha = (byte) (foreground.A + 1);
			byte inv_alpha = (byte)(256 - foreground.A);
			return new Color(
				r: (byte)((alpha * foreground.R + inv_alpha * background.R) >> 8),
				g: (byte)((alpha * foreground.G + inv_alpha * background.G) >> 8),
				b: (byte)((alpha * foreground.B + inv_alpha * background.B) >> 8),
				alpha: (byte) 0xff
				);
		}
		//https://stackoverflow.com/a/28037434
		public static double AngleDiff(double angle1, double angle2) {
			double diff = (angle2 - angle1 + 180) % 360 - 180;
			return diff < -180 ? diff + 360 : diff;
		}

		public static Func<Entity, bool> Or(params Func<Entity, bool>[] f) {
            Func<Entity, bool> result = e => true;
            foreach(Func<Entity, bool> condition in f) {
                if (condition == null)
                    continue;
                Func<Entity, bool> previous = result;
                result = e => (previous(e) || condition(e));
            }
            return result;
        }
        public static Func<Entity, bool> And(params Func<Entity, bool>[] f) {
            Func<Entity, bool> result = e => true;
            foreach (Func<Entity, bool> condition in f) {
                if (condition == null)
                    continue;
                Func<Entity, bool> previous = result;
                result = e => (previous(e) && condition(e));
            }
            return result;
        }
        public static T Elvis<T>(this object o, T result) {
            return o == null ? default(T) : result;
        }
	}
}

