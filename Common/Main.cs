using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using static SadConsole.ColoredString;
using SadConsole.UI;
using SadConsole.Input;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Data;
using NCalc;
using Con = SadConsole.Console;
namespace Common;

public static class Main {

    public static void Replace(this ScreenSurface c, ScreenSurface next) {
        var p = c.Parent;
        p.Children.Remove(c);
        p.Children.Add(next);
        p.IsFocused = true;
    }
    public static string ExpectFile(string path) =>
         (File.Exists(path)) ? path :
            throw new Exception($"File {path} does not exist");
    public static bool TryFile(string path, out string file) =>
        (file = File.Exists(path) ? path : null) != null;
    public static T GetRandom<T>(this IEnumerable<T> e, Rand r) =>
        e.ElementAt(r.NextInteger(e.Count()));
    public static T GetRandomOrDefault<T>(this IEnumerable<T> e, Rand r) =>
        e.Any() ? e.ElementAt(r.NextInteger(e.Count())) : default(T);
    public static SetDict<(int, int), T> Downsample<T>(this Dictionary<(int, int), T> from, double scale, XY offset = null) {
        offset ??= new(0, 0);
        var result = new SetDict<(int, int), T>();
        foreach (((int x, int y) p, var i) in from) {
            result.Add(((offset + p) / scale).roundDown, i);
        }
        return result;
    }
    public static SetDict<(int, int), T> DownsampleSet<T>(this Dictionary<(int, int), HashSet<T>> from, double scale, XY offset = null) {
        offset ??= new(0, 0);
        var result = new SetDict<(int, int), T>();
        foreach (((int x, int y) p, var items) in from) {
            result.AddRange(((offset + p) / scale).roundDown, items);
        }
        return result;
    }
    public static SetDict<(int, int), T> DownsampleSet<T>(this Dictionary<(int, int), HashSet<T>> from, double scale, Func<T, bool> filter, XY offset = null) {
        offset ??= new(0, 0);
        var result = new SetDict<(int, int), T>();
        foreach (((int x, int y) p, var items) in from) {
            var i = items.Where(filter);
            if (i.Any()) {
                result.AddRange(((offset + p) / scale).roundDown, i);
            }
        }
        return result;
    }
    public static SetDict<(int, int), T> DownsampleSet<T>(this Dictionary<(int, int), HashSet<T>> from, double scale, Func<T, bool> filter, XY offset = null, Predicate<(int, int)> posFilter = null) {
        offset ??= new(0, 0);
        var result = new SetDict<(int, int), T>();
        foreach (((int x, int y) p, var items) in from) {
            var scaled = (((XY)p + offset) / scale).roundDown;
            if (posFilter(scaled)) {
                var i = items.Where(filter);
                if (i.Any()) {
                    result.AddRange(scaled, i);
                }
            }
            
        }
        return result;
    }
    public static double step(double from, double to) {
        double difference = to - from;
        return (Math.Abs(difference) > 1) ?
            Math.Sign(difference) :
            difference;
    }
    public static bool CalcAim(XYZ difference, double speed, out double lower, out double higher) {
        double horizontal = difference.xy.magnitude;
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
            double azimuth = difference.xy.angleRad;
            lower = new XYZ(speed * Math.Cos(azimuth) * Math.Cos(lowerAltitude), speed * Math.Sin(azimuth) * Math.Cos(lowerAltitude), speed * Math.Sin(lowerAltitude));
            higher = new XYZ(speed * Math.Cos(azimuth) * Math.Cos(upperAltitude), speed * Math.Sin(azimuth) * Math.Cos(upperAltitude), speed * Math.Sin(upperAltitude));
            return true;
        } else {
            lower = higher = null;
            return false;
        }
    }
    public static bool InRange(double n, double center, double maxDistance) =>
        n > center - maxDistance && n < center + maxDistance;
    public static bool AreKeysPressed(this Keyboard keyboard, params Keys[] keys) =>
        keys.All(keyboard.IsKeyPressed);
    public static double Round(this double d, double interval) {
        int times = (int)(d / interval);
        var roundedDown = times * interval;
        if (d - roundedDown < interval / 2) {
            return roundedDown;
        } else {
            var roundedUp = roundedDown + interval;
            return roundedUp;
        }
    }
    public static Color NextGray(this Random r, int range) {
        var value = r.Next(range);
        return new (value, value, value);
    }
    public static Color Noise(this Color c, Random r, double range) {
        double increaseFactor = r.NextDouble() * range;
        double multiplier = 1 + increaseFactor;
        return new ((int)Math.Min(255, c.R * multiplier), (int)Math.Min(255, c.G * multiplier), (int)Math.Min(255, c.B * multiplier));
    }
    public static Color NextColor(this Random r, int range) => 
        new (r.Next(range), r.Next(range), r.Next(range));
    public static Color Round(this Color c, int factor) => 
        new (factor * (c.R / factor), factor * (c.G / factor), factor * (c.B / factor));
    //public static Color Add(this Color c, int value) => c.Add(new Color(value, value, value));
    public static Color Add(this Color c1, int r = 0, int g = 0, int b = 0) => 
        new (Math.Min(255, c1.R + r), Math.Min(255, c1.G + g), Math.Min(255, c1.B + b));
    public static Color Add(this Color c1, Color c2) => 
        new (Math.Min(255, c1.R + c2.R), Math.Min(255, c1.G + c2.G), Math.Min(255, c1.B + c2.B));
    public static Color Subtract(this Color c, int value) => 
        c.Subtract(new Color(value, value, value));
    public static Color Subtract(this Color c1, Color c2) =>
        new (Math.Max(0, c1.R - c2.R), Math.Max(0, c1.G - c2.G), Math.Max(0, c1.B - c2.B));
    public static Color Divide(this Color c, int scale) =>
        new (c.R / scale, c.G / scale, c.B / scale);
    public static Color Multiply(this Color c, double r = 1, double g = 1, double b = 1, double a = 1) =>
        new ((int)(c.R * r), (int)(c.G * g), (int)(c.B * b), (int)(c.A * a));
    public static Color Divide(this Color c, double scale) =>
        new ((int)(c.R / scale), (int)(c.G / scale), (int)(c.B / scale));
    public static Color Clamp(this Color c, int max) =>
        new (Math.Min(c.R, max), Math.Min(c.G, max), Math.Min(c.B, max));
    public static Color Gray(int value) => 
        new (value, value, value, 255);
    public static Color Gray(this Color c) => 
        Color.FromHSL(0, 0, c.GetBrightness());
    public static ColoredGlyph Gray(this ColoredGlyph cg) =>
        new (cg.Foreground.Gray(), cg.Background.Gray(), cg.Glyph);
    public static Color WithValues(this Color c, int? red = null, int? green = null, int? blue = null, int? alpha = null) =>
        new(red ?? c.R, green ?? c.G, blue ?? c.B, alpha ?? c.A);
    
    public static Color SetBrightness(this Color c, float brightness) =>
        Color.FromHSL(c.GetHue(), c.GetSaturation(), brightness);
    public static double CalcFireAngle(XY posDiff, XY velDiff, double missileSpeed, out double timeToHit) {
        /*
        var timeToHit = posDiff.Magnitude / missileSpeed;
        var posFuture = posDiff + velDiff * timeToHit;

        var posDiffPrev = posDiff;
        posDiff = posFuture;
        */
        timeToHit = posDiff.magnitude / missileSpeed;
        XY posDiffNext;
        double timeToHitPrev;
        int i = 10;
        do {
            posDiffNext = posDiff + velDiff * timeToHit;
            timeToHitPrev = timeToHit;
            timeToHit = posDiffNext.magnitude / missileSpeed;
        } while (Math.Abs(timeToHit - timeToHitPrev) > 0.1 && i-- > 0);

        return posDiffNext.angleRad;
        /*
        var a = velDiff.Dot(velDiff) - missileSpeed * missileSpeed;
        var b = 2 * velDiff.Dot(posDiff);
        var c = posDiff.Dot(posDiff);

        var p = -b / (2 * a);
        var q = Math.Sqrt(b * b - 4 * a * c) / (2 * a);
        var t1 = -p - q;
        var t2 = p + q;

        timeToHit = t1;
        if(t1 > t2 && t2 > 0) {
            timeToHit = t2;
        }
        var posFuture = posDiff + velDiff * timeToHit;
        return posFuture.Angle;
        */
    }

    /*
    public static bool InRange(double n, double min, double max) {
        return n > min && n < max;
    }
    */
    public static int LineLength(this string lines) =>
        lines.IndexOf('\n');
    public static int LineCount(this string lines) =>
        lines.Split('\n').Length;
    public static T LastItem<T>(this List<T> list) => list[list.Count - 1];
    public static T FirstItem<T>(this List<T> list) => list[0];
    public static string FlipLines(this string s) {
        var lines = new List<string>(s.Split('\n'));
        lines.Reverse();
        var result = new StringBuilder(s.Length - s.LineCount());
        for (int i = 0; i < lines.Count - 1; i++) {
            result.AppendLine(lines[i]);
        }
        result.Append(lines.LastItem());
        return result.ToString();
    }
    public static void PrintLines(this SadConsole.Console console, int x, int y, string lines, Color? foreground = null, Color? background = null, Mirror mirror = Mirror.None) {
        foreach (var line in lines.Replace("\r\n", "\n").Split('\n')) {
            console.Print(x, y, line, foreground ?? Color.White, background ?? Color.Black, mirror);
            y++;
        }
    }
    public static List<XYZ> GetWithin(int radius) {
        var result = new List<XYZ>();
        for (int i = 0; i < radius; i++) {
            result.AddRange(GetSurrounding(i));
        }
        result = new(result.Distinct(new XYZGridComparer()).Where(p => p.Magnitude2 < radius * radius));
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
        for (int y = -radius + 1; y < radius; y++) {
            for (int z = -radius + 1; z < radius; z++) {
                result.Add(new XYZ(radius, y, z));  //East side
                result.Add(new XYZ(-radius, y, z)); //West side
            }
            //Add the top/bottom edges of each side
            result.Add(new XYZ(radius, y, radius));
            result.Add(new XYZ(radius, y, -radius));
            result.Add(new XYZ(-radius, y, radius));
            result.Add(new XYZ(-radius, y, -radius));
        }
        for (int x = -radius + 1; x < radius; x++) {
            for (int z = -radius + 1; z < radius; z++) {
                result.Add(new XYZ(x, radius, z));   //North side
                result.Add(new XYZ(x, -radius, z));   //South side
            }

            result.Add(new XYZ(x, radius, radius)); //North upper
            result.Add(new XYZ(x, radius, -radius));    //North lower
            result.Add(new XYZ(x, -radius, radius));    //South upper
            result.Add(new XYZ(x, -radius, -radius));//South lower
        }
        for (int x = -radius + 1; x < radius; x++) {
            for (int y = -radius + 1; y < radius; y++) {
                result.Add(new XYZ(x, y, radius));   //Top side
                result.Add(new XYZ(x, y, -radius));   //Bottom side
            }
        }
        //Vertical
        for (int z = -radius + 1; z < radius; z++) {
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
    public static bool HasElement(this XElement e, string key, out XElement result) =>
        (result = e.Element(key)) != null;
    public static bool HasElements(this XElement e, string key, out IEnumerable<XElement> result) =>
        (result = e.Elements(key)) != null;
    public static string Att(this XElement e, string key) =>
        e.Attribute(key)?.Value;
    public static string ExpectAtt(this XElement e, string key) =>
        e.Att(key)
            ?? throw e.Missing<string>(key);
    public static XElement ExpectElement(this XElement e, string name) =>
        e.Element(name)
            ?? throw new Exception($"Element <{e.Name}> requires subelement {name} ### {e.Name}");
    
    public static bool TryAtt(this XElement e, string key, out string result) =>
        (result = e.Att(key)) != null;
    public static string TryAtt(this XElement e, string key, string fallback = "") =>
        e.Att(key) ?? fallback;
    public static string TryAttNullable(this XElement e, string key) =>
        e.Att(key);
    public static char TryAttChar(this XElement e, string attribute, char fallback) =>
        e.TryAtt(attribute, out string s) ?
            (s.Length == 1 ?
                s.First() :
            s.StartsWith("\\") && int.TryParse(s.Substring(1), out var result) ?
                (char)result :
            throw e.Invalid<char>(attribute)
            ) : fallback;
    public static char ExpectAttChar(this XElement e, string attribute) =>
        e.TryAtt(attribute, out string s) ?
            (s.Length == 1 ?
                s.First() :
            s.StartsWith("\\") && int.TryParse(s.Substring(1), out var result) ?
                (char)result :
            throw e.Invalid<char>(attribute)
            ) : throw e.Invalid<char>(attribute);
    public static Color TryAttColor(this XElement e, string attribute, Color fallback) {
        if (e.TryAtt(attribute, out string s)) {
            if (int.TryParse(s, NumberStyles.HexNumber, null, out var packed)) {
                return new Color((packed >> 24) & 0xFF, (packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);
            } else try {
                    var f = typeof(Color).GetField(s);
                    return (Color)(f?.GetValue(null) ?? throw e.Invalid<Color>(attribute));
                } catch {
                    throw e.Invalid<Color>(attribute);
                }
        } else {
            return fallback;
        }
    }
    public static Exception Missing<T>(this XElement e, string key) =>
        new Exception($"{typeof(T).Name} requires ${typeof(T).Name} attribute: {key} ### {e.Name}");
    public static Exception Invalid<T>(this XElement e, string key) =>
        new Exception($"{typeof(T).Name} value expected: {key}=\"{e.Attribute(key).Value}\" ### {e.Name}");
    public static int TryAttInt(this XElement e, string key, int fallback = 0) => 
        e.TryAtt(key, out var value) ?
            (int.TryParse(value, out int result) ?
                result :
            value.Any() ?
                Convert.ToInt32(new Expression(value).Evaluate()) :
            throw e.Invalid<int>(key)) :
        fallback;

    public static int? TryAttIntNullable(this XElement e, string key, int? fallback=null) =>
        e.TryAtt(key, out var value) ?
            (value == "null" ? null : 
            int.TryParse(value, out int result) ? result :
            value.Any() ? Convert.ToInt32(new Expression(value).Evaluate()) :
            throw e.Invalid<int?>(key)) :
        fallback;
    public static int? ExpectAttIntNullable(this XElement e, string key, int? fallback = null) =>
    e.TryAtt(key, out var value) ?
        (value == "null" ? null :
        int.TryParse(value, out int result) ? result :
        value.Any() ? Convert.ToInt32(new Expression(value).Evaluate()) :
        throw e.Invalid<int?>(key)) :
    throw e.Missing<int?>(key);
    public static Color ExpectAttColor(this XElement e, string key) {
        if (e.TryAtt(key, out string s)) {
            if (int.TryParse(s, NumberStyles.HexNumber, null, out var packed)) {
                return new Color((packed >> 24) & 0xFF, (packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);
            } else try {
                return (Color)typeof(Color).GetField(s).GetValue(null);
            } catch {
                throw e.Invalid<Color>(key);
            }
        } else {
            throw e.Missing<Color>(key);
        }
    }
    public static int ExpectAttInt(this XElement e, string key) =>
        e.Attribute(key) is XAttribute a ?
            ExpectAttributeInt(a) :
            throw e.Missing<int>(key);

    public static IDice ExpectAttDice(this XElement e, string key) =>
        e.Attribute(key) is XAttribute a ?
            ExpectAttributeDice(a) :
            throw new Exception($"<{e.Name}> requires dice range attribute: {key} ### {e} ### {e.Parent}");
    public static int ExpectAttributeInt(this XAttribute a) =>
        int.TryParse(a.Value, out int result) ? result :
            a.Value.Any() ? Convert.ToInt32(new Expression(a.Value).Evaluate()) :
            throw new Exception($"int value / equation expected: {a.Name} = \"{a.Value}\"");

    public static IDice ExpectAttributeDice(this XAttribute a) =>
        IDice.Parse(a.Value) ?? 
            throw new Exception($"int value / equation expected: {a.Name} = \"{a.Value}\"");

    public static double ExpectAttDouble(this XElement e, string key) =>
        e.TryAtt(key, out var value) ? 
            (double.TryParse(value, out double result) ? result :
            value.Any() ? Convert.ToDouble(new Expression(value).Evaluate()) :
            throw e.Missing<double>(key)) :
        throw e.Missing<double>(key);
    public static bool ExpectAttBool(this XElement e, string key) =>
        e.TryAtt(key, out var value) ?  
            (bool.TryParse(value, out bool result) ? result :
            throw e.Invalid<bool>(key)) :
        throw e.Missing<bool>(key);
    public static double TryAttDouble(this XElement e, string attribute, double fallback = 0) =>
        e.TryAtt(attribute, out var value) ?
            (double.TryParse(value, out double result) ? result :
            value.Any() ? Convert.ToDouble(new Expression(value).Evaluate()) :
            throw e.Invalid<double>(attribute)) :
        fallback;
    public static bool TryAttDouble2(this XElement e, string attribute, out double result, double fallback = 0) {
        var b = e.TryAtt(attribute, out var value);
        result = b ?
            (double.TryParse(value, out result) ? result :
            value.Any() ? Convert.ToDouble(new Expression(value).Evaluate()) :
            throw e.Invalid<double>(attribute)) :
        fallback;
        return b;
    }
    public static IEnumerable<string> GetKeys(this object o) {
        var pr = o.GetType().GetProperties();
        return pr.Select(p => p.Name);
    }
    public static Dictionary<string, T> ToDict<T>(this object o) {
        var pr = o.GetType().GetProperties();
        return pr.ToDictionary(p => p.Name, p => (T)p.GetValue(o, null));
    }
    public static Dictionary<U, T> ToDict<U, T>(this object o, Func<string, U> keyMap) {
        var pr = o.GetType().GetProperties();
        return pr.ToDictionary(p => keyMap(p.Name), p => (T)p.GetValue(o, null));
    }
    public static List<string> SplitLine(this string s, int width) {
        var result = new List<string>();
        var column = 0;
        var line = new StringBuilder();
        var word = new StringBuilder();

        void AddWord() {
            line.Append(word.ToString());
            word.Clear();
        }
        void AddLine() {
            result.Add(line.ToString());
            line.Clear();
            column = 0;
        }
        foreach (var c in s) {
            if (line.Length + column < width) {
                if (c == ' ') {
                    AddWord();
                    line.Append(c);
                    column++;
                } else if (c == '\n') {
                    AddWord();
                    column = 0;
                } else if (c == '-') {
                    word.Append(c);
                    AddWord();
                } else {
                    word.Append(c);
                    column++;
                }
            } else {
                word.Append(c);
                AddLine();
            }
        }
        line.Append(word.ToString());
        if (line.Length > 0) {
            AddLine();
        }

        return result;
    }

    public static List<ColoredString> SplitLine(this ColoredString s, int width) {
        var result = new List<ColoredString>();
        var column = 0;
        var line = new List<ColoredGlyph>();
        var word = new List<ColoredGlyph>();

        void AddWord() {
            line.AddRange(word);
            word.Clear();
        }
        void AddLine() {
            result.Add(new(line.ToArray()));
            line.Clear();
            column = 0;
        }
        foreach (var c in s) {
            if (column < width) {
                var g = c.Glyph;
                if (g == ' ') {
                    AddWord();
                    line.Append(c);
                    column++;
                } else if (g == '\n') {
                    AddWord();
                    column = 0;
                } else if (g == '-') {
                    word.Append(c);
                    AddWord();
                } else {
                    word.Append(c);
                    column++;
                }
            } else {
                word.Append(c);
                AddLine();
            }
        }
        line.AddRange(word);
        if (line.Count > 0) {
            AddLine();
        }

        return result;
    }
    public static void InheritAttributes(this XElement sub, XElement source) {
        foreach (var att in source.Attributes()) {
            if (sub.Attribute(att.Name) == null) {
                sub.SetAttributeValue(att.Name, att.Value);
            }
        }
    }

    public static XY GetBoundaryPoint(XY dimensions, double angleRad) {
        while (angleRad < 0) {
            angleRad += 2 * Math.PI;
        }
        while (angleRad > 2 * Math.PI) {
            angleRad -= 2 * Math.PI;
        }
        var center = dimensions / 2;
        var halfWidth = dimensions.x / 2;
        var halfHeight = dimensions.y / 2;
        var diagonalAngle = dimensions.angleRad;
        if ((angleRad < diagonalAngle || angleRad > Math.PI * 2 - diagonalAngle)  //Right side
            || (angleRad < Math.PI + diagonalAngle && angleRad > Math.PI - diagonalAngle) //Left side
            ) {
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);

            var factor = Math.Abs(halfWidth / cos);
            return center + new XY(cos * factor, sin * factor);
        } else /* if((angle < Math.PI - diagonalAngle && angle > diagonalAngle)	//Top side
				|| (angle < 2 * Math.PI - diagonalAngle && angle > Math.PI + diagonalAngle)
				) */ {
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);
            var factor = Math.Abs(halfHeight / sin);
            return center + new XY(cos * factor, sin * factor);
        } /* else if(angle == diagonalAngle) {
				return new XY(dimensions.x, dimensions.y);
			} else if(angle == Math.PI - diagonalAngle) {
				return new XY(0, dimensions.y);
			} else if(angle == Math.PI + diagonalAngle) {
				return new XY(0, 0);
			} else if(angle == 2 * Math.PI - diagonalAngle) {
				return new XY(dimensions.x, 0);
			} else {
				throw new Exception($"Invalid angle: {angle}");
			} */
    }

    public static List<string> Wrap(this string s, int width) {
        var lines = new List<string> { "" };
        foreach (var word in Regex.Split(s, $"({Regex.Escape(" ")})")) {
            if (lines.Last().Length + word.Length < width) {
                lines[lines.Count - 1] += word;
            } else {
                if (word == " ") {
                    lines.Add("");
                } else {
                    lines.Add(word);
                }

            }
        }
        return lines;
    }
    public static void PaintCentered(this Window w, string s, int x, int y)=>
        w.Print(x - s.Length / 2, y, s);
    public static int ParseInt(this string s, int fallback = 0) =>
        int.TryParse(s, out int result) ? result : fallback;
    public static int ParseIntMin(this string s, int min, int fallback = 0) =>
        Math.Max(s.ParseInt(fallback), min);
    public static int ParseIntMax(this string s, int max, int fallback = 0) =>
        Math.Min(s.ParseInt(fallback), max);
    public static int ParseIntBounded(this string s, int min, int max, int fallback = 0) =>
        Range(min, s.ParseInt(fallback), max);
    public static int Range(int min, int max, int n) =>
        Math.Min(max, Math.Max(min, n));
    public static bool ParseBool(this string s, bool fallback = false) {
        return s == "true" ?
            true : (s == "false" ?
            false : fallback);
    }
    //We expect either no value or a valid value; an invalid value gets an exception
    public static bool TryAttBool(this XElement e, string attribute, bool fallback = false) =>
        e.TryAtt(attribute).ParseBool(fallback);
    public static bool TryAttributeBool(XAttribute a, bool fallback = false) {
        if (a == null) {
            return fallback;
        } else if (bool.TryParse(a.Value, out bool result)) {
            return result;
        } else {
            throw new Exception($"Bool value expected: {a.Name}=\"{a.Value}\"");
        }
    }
    public static bool? TryAttBoolNullable(this XElement e, string name, bool? fallback = null) =>
        e.Attribute(name)?.TryAttributeBoolOptional(fallback);
    
    public static bool? TryAttributeBoolOptional(this XAttribute a, bool? fallback = null) {
        if (a == null) {
            return fallback;
        } else if (bool.TryParse(a.Value, out bool result)) {
            return result;
        } else {
            throw new Exception($"int value expected: {a.Name}=\"{a.Value}\" ### {a.Parent.Name}");
        }
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
    public static TEnum TryAttEnum<TEnum>(this XElement e, string attribute, TEnum fallback = default) where TEnum : struct =>
        e.Attribute(attribute)?.ParseEnum(fallback) ?? fallback;

    public static bool TryAttEnum<TEnum>(this XElement e, string attribute, out TEnum result) where TEnum : struct {
        bool b = e.TryAtt(attribute, out var s);
        result = b ? Enum.Parse<TEnum>(s) : default;
        return b;
    }
    public static object TryAttEnum(this XElement e, Type t, string attribute, object fallback) =>
        Convert.ChangeType(e.Attribute(attribute)?.ParseEnum(t, fallback), t) ?? fallback;
    public static TEnum ExpectAttEnum<TEnum>(this XElement e, string attribute) where TEnum : struct {
        string value = e.ExpectAtt(attribute);
        if (Enum.TryParse(value, out TEnum result)) {
            return result;
        } else {
            throw new Exception($"Enum value of {typeof(TEnum).Name} expected: {attribute}=\"{value}\"");
        }
    }
    //We expect either no value or a valid value; an invalid value gets an exception
    public static TEnum ParseEnum<TEnum>(this XAttribute a, TEnum fallback = default) where TEnum : struct {
        if (a == null) {
            return fallback;
        } else if (Enum.TryParse(a.Value, out TEnum result)) {
            return result;
        } else {
            throw new Exception($"Enum value of {fallback.GetType().Name} expected: {a.Name}=\"{a.Value}\"");
        }
    }
    public static object ParseEnum(this XAttribute a, Type t, object fallback) {
        if (a == null) {
            return fallback;
        } else if (Enum.TryParse(t, a.Value, out var result)) {
            return Convert.ChangeType(result, t);
        } else {
            throw new Exception($"Enum value of {fallback.GetType().Name} expected: {a.Name}=\"{a.Value}\"");
        }
    }
    //Remember to call this to initialize XML properties
    public static void Initialize(this XElement e, object obj, object source = null) {
        var props = obj.GetType().GetFields();
        foreach (var p in props) {
            var a = p.GetCustomAttributes(true).OfType<IAtt>().FirstOrDefault();
            if(a != null) {
                var key = p.Name;

                var value = e.Att(key);
                if (value == null) {

                    if(source != null) {
                        p.SetValue(obj, p.GetValue(source));
                    } else if (a is Opt) {
                    } else {
                        throw new Exception($"<{e.Name}> requires {p.FieldType.Name} attribute: {key} ### {e.Parent.Name}");
                    }
                    continue;
                }

                object parsed = new Dictionary<Type, Func<object>>() {
                    [typeof(string)] = () => value,

                    [typeof(bool)] = ParseBool,
                    [typeof(int)] = ParseInt,
                    [typeof(char)] = ParseChar,
                    [typeof(double)] = ParseDouble,

                    [typeof(bool?)] = ParseBoolNullable,
                    [typeof(int?)] = () => e.ExpectAttIntNullable(key),

                    [typeof(IDice)] = () => e.ExpectAttDice(key),
                    [typeof(Color)] = () => e.ExpectAttColor(key),
                    [typeof(Color?)] = () => e.ExpectAttColor(key),
                }[p.FieldType]();
                p.SetValue(obj, parsed);

                object ParseBool() =>
                    bool.TryParse(value, out var result) ? result : throw Error<bool>();
                object ParseBoolNullable() =>
                    value == "null" ? null : ParseBool();
                object ParseInt() =>
                    value.Any() ? Convert.ToInt32(new Expression(value).Evaluate()) : throw Error<int>();
                object ParseChar() =>
                    (value.Length == 1 ?
                        value.First() :
                    value.StartsWith("\\") && int.TryParse(value.Substring(1), out var result) ?
                        (char)result :
                        throw Error<char>());
                object ParseDouble() =>
                    value.Any() ? Convert.ToDouble(new Expression(value).Evaluate()) : throw Error<double>();
                Exception Error<T>() =>
                    e.Invalid<T>(key);
            }
        }
    }
    public static TValue TryLookup<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue fallback = default) =>
        d.ContainsKey(key) ? d[key] : fallback;
    public static int CalcAccuracy(int difficulty, int skill, Random karma) {
        if (skill > difficulty) {
            return 100;
        } else {
            var miss = difficulty - skill;
            return 100 - karma.Next(miss);
        }
    }
    //Chance that the shot is blocked by an obstacle
    public static bool CalcBlocked(int coverage, int accuracy, Random karma) =>
        karma.Next(coverage) > karma.Next(accuracy);
    public static ColoredGlyph Colored(char c, Color? f = null, Color? b = null) =>
        new(f??Color.White, b??Color.Black, c);
    public static ColoredString WithBackground(this ColoredString c, Color? Background = null) {
        var result = c.SubString(0, c.Count());
        result.SetBackground(Background ?? Color.Black);
        return result;
    }
    public static ColoredString Adjust(this ColoredString c, Color foregroundInc) {
        var result = c.SubString(0, c.Count());
        foreach (var g in result) {
            g.Foreground = Sum(g.Foreground, foregroundInc);
        }
        return result;
    }
    public static ColoredString WithOpacity(this ColoredString s, byte front, byte back = 255) {
        s = s.Clone();
        foreach (var c in s) {
            c.Foreground = c.Foreground.SetAlpha(front);
            c.Background = c.Background.SetAlpha(back);
        }
        return s;
    }
    public static ColoredString Brighten(this ColoredString s, int intensity) {
        return s.Adjust(new(intensity, intensity, intensity, 0));
    }
    public static ColoredGlyphEffect ToEffect(this ColoredGlyph cg) {
        return new() {
            Foreground = cg.Foreground,
            Background = cg.Background,
            Glyph = cg.Glyph
        };
    }
    public static ColoredString ToColoredString(this string s) =>
        s.Colored();
    public static ColoredString Colored(this string s, Color? f = null, Color? b = null) =>
        new(s, f ?? Color.White, b ?? Color.Black);
    public static ColoredString ToColoredString(this ColoredGlyph c) =>
        new(c.ToEffect());
    
    public static ColoredGlyph Brighten(this ColoredGlyph c, int intensity) {
        var result = c.Clone();
        result.Foreground = Sum(result.Foreground, new Color(intensity, intensity, intensity, 0));
        return result;
    }
    public static ColoredString Adjust(this ColoredString c, Color foregroundInc, Color backgroundInc) {
        var result = c.SubString(0, c.Count());
        foreach (var g in result) {
            g.Foreground = Sum(g.Foreground, foregroundInc);
            g.Background = Sum(g.Background, backgroundInc);
        }
        return result;
    }
    public static ColoredGlyph Adjust(this ColoredGlyph c, Color foregroundInc) {
        var result = c.Clone();
        result.Foreground = Sum(result.Foreground, foregroundInc);
        return result;
    }
    public static Color Sum(Color c, Color c2) =>
        new(Range(0, 255, c.R + c2.R), Range(0, 255, c.G + c2.G), Range(0, 255, c.B + c2.B), Range(0, 255, c.A + c2.A));
    
    //Essentially the same as blending this color over Color.Black
    public static Color Premultiply(this Color c) => new((c.R * c.A) / 255, (c.G * c.A) / 255, (c.B * c.A) / 255, c.A);
    //Premultiply and also set the alpha
    public static Color PremultiplySet(this Color c, int alpha) => new((c.R * c.A) / 255, (c.G * c.A) / 255, (c.B * c.A) / 255, alpha);

    //Premultiplies this color and the blends another color over it
    public static Color BlendPremultiply(this Color background, Color foreground, byte setAlpha = 0xff) {

        var alpha = (byte)(foreground.A);
        var inv_alpha = (byte)(255 - foreground.A);
        return new(
            r: (byte)((alpha * foreground.R + inv_alpha * background.R * background.A / 255) >> 8),
            g: (byte)((alpha * foreground.G + inv_alpha * background.G * background.A / 255) >> 8),
            b: (byte)((alpha * foreground.B + inv_alpha * background.B * background.A / 255) >> 8),
            alpha: setAlpha
            );
    }

    //https://stackoverflow.com/a/12016968
    //Blend another color over this color
    public static Color Blend(this Color background, Color foreground, byte setAlpha = 0xff) {
        //Background should be premultiplied because we ignore its alpha value
        var alpha = (byte)(foreground.A);
        var inv_alpha = (byte)(255 - foreground.A);
        return new(
            r: (byte)((alpha * foreground.R + inv_alpha * background.R) >> 8),
            g: (byte)((alpha * foreground.G + inv_alpha * background.G) >> 8),
            b: (byte)((alpha * foreground.B + inv_alpha * background.B) >> 8),
            alpha: setAlpha
            );
    }
    public static ColoredGlyph Blend(this ColoredGlyph back, ColoredGlyph front) {
        var d = new List<CellDecorator>();
        var f = back.Foreground;
        var b = back.Background;
        int g = back.Glyph;

        if (front.Glyph != 0 && front.Glyph != ' ' && front.Foreground.A != 0) {
            d.Add(new(f, g, Mirror.None));

            f = front.Foreground;
            g = front.Glyph;
        }
        b = b.Premultiply().Blend(front.Background);

        return new(f, b, g) { Decorators = d.ToArray() };
    }

    public static ColoredGlyph PremultiplySet(this ColoredGlyph cg, int alpha) {
        if (alpha == 255) {
            return cg;
        }
        return new(cg.Foreground.PremultiplySet(alpha), cg.Background.PremultiplySet(alpha), cg.Glyph);
    }

    //https://stackoverflow.com/a/28037434
    public static double AngleDiffDeg(double from, double to) {
        void mod(ref double a) {
            while (a < 0)
                a += 360;
            while (a >= 360)
                a -= 360;
        }

        mod(ref from);
        mod(ref to);
        
        double diff = (to - from + 180) % 360 - 180;
        return diff < -180 ? diff + 360 : diff;
    }
    public static double AngleDiffRad(double from, double to) {
        var p = Math.PI;
        var t = p * 2;
        void mod(ref double a) {
            while (a < 0)
                a += t;
            while (a >= t)
                a -= t;
        }

        mod(ref from);
        mod(ref to);

        double diff = (to - from + p) % t - p;
        return diff < -p ? diff + t : diff;
    }
    public static bool IsRight(double from, double to) =>
        (XY.Polar(to)-XY.Polar(from)).magnitude2 > (XY.Polar(to)-XY.Polar(from - 0.1)).magnitude2;
    public static Func<T, bool> Or<T>(params Func<T, bool>[] f) {
        Func<T, bool> result = e => true;
        foreach (Func<T, bool> condition in f) {
            if (condition == null)
                continue;
            Func<T, bool> previous = result;
            result = e => (previous(e) || condition(e));
        }
        return result;
    }
    public static Func<T, bool> And<T>(params Func<T, bool>[] f) {
        Func<T, bool> result = e => true;
        foreach (Func<T, bool> condition in f) {
            if (condition == null)
                continue;
            Func<T, bool> previous = result;
            result = e => (previous(e) && condition(e));
        }
        return result;
    }
    public static T Elvis<T>(this object o, T result) =>
        o == null ? default(T) : result;
}

