using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;

namespace IslandHopper {
	public static class Helper {
		public static bool InRange(double n, double center, double maxDistance) {
			return n > center - maxDistance && n < center + maxDistance;
		}
		public static bool AreKeysPressed(this SadConsole.Input.Keyboard keyboard, params Keys[] keys) {
			foreach(var key in keys) {
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
			for(int i = 0; i < lines.Count-1; i++) {
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
		public static int Amplitude(this Random random, int amplitude) => random.Next(-amplitude, amplitude);
	}
}

