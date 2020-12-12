using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using static Common.RandomExtensions;

namespace TranscendenceRL {
    
    public class RandomConverter : TypeConverter {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            return JsonConvert.SerializeObject(((Random)value).Save());
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(Random) || base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            var state = JsonConvert.DeserializeObject<RandomState>(Convert.ToString(value));
            return state.Restore();
        }
    }

    interface SaveGame {
        public static void PrepareConvert() {
            TypeDescriptor.AddAttributes(typeof(Random), new TypeConverterAttribute(typeof(RandomConverter)));
        }
        public static string Serialize(object o) {
            PrepareConvert();
            return JsonConvert.SerializeObject(o, settings);
        }
        public static T Serialize<T>(string s) {
            PrepareConvert();
            return JsonConvert.DeserializeObject<T>(s, settings);
        }
        public static readonly JsonSerializerSettings settings = new JsonSerializerSettings {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
    }
    class LiveGame {
        public World world;
        public Player player;
        public PlayerShip playerShip;
    }
    class DeadGame {
        public World world;
        public Player player;
        public PlayerShip playerShip;
        public Epitaph epitaph;
    }
}
