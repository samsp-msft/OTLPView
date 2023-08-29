using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using System.Text;

namespace OTLPView
{
    public static class Helpers
    {
        public static bool FindStringValue(this RepeatedField<KeyValue> attributes, string key, out string value)
        {
            value = null;
            foreach (var kv in attributes)
            {
                if (kv.Key == key)
                {
                    value = kv.Value.ValueString();
                    return true;
                }
            }
            return false;
        }

        public static string FindStringValueOrDefault(this RepeatedField<KeyValue> attributes, string key, string defaultValue)
        {
            foreach (var kv in attributes)
            {
                if (kv.Key == key)
                {
                    return kv.Value.ValueString();
                }
            }
            return defaultValue;
        }

        public static string Left(this string value, int length)
        {
            if (value.Length <= length)
            {
                return value;
            }
            return value.Substring(0, length);
        }

        public static string Right(this string value, int length)
        {
            if (value.Length <= length)
            {
                return value;
            }
            return value.Substring(value.Length - length, length);
        }

        public static string ValueString(this AnyValue value)
        {
            switch (value.ValueCase)
            {
                case AnyValue.ValueOneofCase.StringValue:
                    return value.StringValue;
                case AnyValue.ValueOneofCase.IntValue:
                    return value.IntValue.ToString();
                case AnyValue.ValueOneofCase.DoubleValue:
                    return value.DoubleValue.ToString();
                case AnyValue.ValueOneofCase.BoolValue:
                    return value.BoolValue.ToString();
                case AnyValue.ValueOneofCase.BytesValue:
                    return value.BytesValue.ToHexString();
                default:
                    return value.ToString();
            }
        }

        public static Dictionary<string, string> ToDictionary(this RepeatedField<KeyValue> attributes)
        {
            var dict = new Dictionary<string, string>();
            foreach (var kv in attributes)
            {
                dict.TryAdd(kv.Key, kv.Value.ValueString());
            }
            return dict;
        }

        public static string ConcatString(this IReadOnlyDictionary<string, string> dict)
        {
            StringBuilder sb = new();
            var first = true;
            foreach (var kv in dict)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                sb.Append($"{kv.Key}: ");
                sb.Append((string.IsNullOrEmpty(kv.Value)) ? "\'\'" : kv.Value);
            }
            return sb.ToString();
        }

        public static string ValueOrDefault(this Dictionary<string, string> dict, string key, string defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }


        private const int DaysPerYear = 365;
        // Number of days in 4 years
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        // Number of days in 100 years
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        // Number of days in 400 years
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097
        private const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear;
        private const int HoursPerDay = 24;
        internal const int MicrosecondsPerMillisecond = 1000;
        private const long TicksPerMicrosecond = 10;
        private const long TicksPerMillisecond = TicksPerMicrosecond * MicrosecondsPerMillisecond;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * HoursPerDay;
        private const long UnixEpochTicks = DaysTo1970 * TicksPerDay;

        public static DateTime UnixNanoSecondsToDateTime(ulong unixTimeNanoSeconds)
        {
            var unixTimeTicks = (long)(unixTimeNanoSeconds / 100);
            var ticks = unixTimeTicks + UnixEpochTicks;
            return new DateTime(ticks);
        }


        public static string ToHexString(this Google.Protobuf.ByteString bytes)
        {

      

            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append($"{b:x2}");
            }
            return sb.ToString();
        }

        //public static string[][] BarColors = new string[][]
        //{
        //new string [] { "#f6d5d5", "#f2c0c0", "#edabab", "#e99696", "#e48181", "#e06c6c", "#db5757", "#d74242", "#d22d2d", "#bd2828", "#a82424", "#931f1f", "#7e1b1b", "#691616", "#541212"},
        //new string [] { "#fbeeea", "#f6ddd5", "#f2ccc0", "#edbbab", "#e9ab96", "#e49a81", "#e0896c", "#db7857", "#d76742", "#d2562d", "#bd4d28", "#a84524", "#933c1f", "#7e341b", "#542212"},
        //new string [] { "#fbf2ea", "#f6e6d5", "#f2d9c0", "#edccab", "#e9bf96", "#e4b381", "#e0a66c", "#db9957", "#d78c42", "#d2802d", "#bd7328", "#a86624", "#93591f", "#7e4d1b", "#543312"},
        //new string [] { "#fbf6ea", "#f6eed5", "#f2e5c0", "#edddab", "#e9d496", "#e4cb81", "#e0c36c", "#dbba57", "#d7b242", "#d2a92d", "#bd9828", "#a88724", "#93761f", "#7e651b", "#544412"},
        //new string [] { "#fbfbea", "#f6f6d5", "#f2f2c0", "#ededab", "#e9e996", "#e4e481", "#e0e06c", "#dbdb57", "#d7d742", "#d2d22d", "#bdbd28", "#a8a824", "#93931f", "#7e7e1b", "#545412"},
        //new string [] { "#f6fbea", "#eef6d5", "#e5f2c0", "#ddedab", "#d4e996", "#cbe481", "#c3e06c", "#badb57", "#b2d742", "#a9d22d", "#98bd28", "#87a824", "#76931f", "#657e1b", "#445412"},
        //new string [] { "#eefbea", "#ddf6d5", "#ccf2c0", "#bbedab", "#abe996", "#9ae481", "#89e06c", "#78db57", "#67d742", "#56d22d", "#4dbd28", "#45a824", "#3c931f", "#347e1b", "#225412"},
        //new string [] { "#eafbf2", "#d5f6e6", "#c0f2d9", "#abedcc", "#96e9bf", "#81e4b3", "#6ce0a6", "#57db99", "#42d78c", "#2dd280", "#28bd73", "#24a866", "#1f9359", "#1b7e4d", "#125433"},
        //new string [] { "#eafbfb", "#d5f6f6", "#c0f2f2", "#abeded", "#96e9e9", "#81e4e4", "#6ce0e0", "#57dbdb", "#42d7d7", "#2dd2d2", "#28bdbd", "#24a8a8", "#1f9393", "#1b7e7e", "#125454"},
        //new string [] { "#eaf6fb", "#d5eef6", "#c0e5f2", "#abdded", "#96d4e9", "#81cbe4", "#6cc3e0", "#57badb", "#42b2d7", "#2da9d2", "#2898bd", "#2487a8", "#1f7693", "#1b657e", "#124454"},
        //new string [] { "#eaf2fb", "#d5e6f6", "#c0d9f2", "#abcced", "#96bfe9", "#81b3e4", "#6ca6e0", "#5799db", "#428cd7", "#2d80d2", "#2873bd", "#2466a8", "#1f5993", "#1b4d7e", "#123354"},
        //new string [] { "#eaeafb", "#d5d5f6", "#c0c0f2", "#ababed", "#9696e9", "#8181e4", "#6c6ce0", "#5757db", "#4242d7", "#2d2dd2", "#2828bd", "#2424a8", "#1f1f93", "#1b1b7e", "#121254"},
        //new string [] { "#f6eafb", "#eed5f6", "#e5c0f2", "#ddabed", "#d496e9", "#cb81e4", "#c36ce0", "#ba57db", "#b242d7", "#a92dd2", "#9828bd", "#8724a8", "#761f93", "#651b7e", "#441254"},
        //new string [] { "#fbeaf6", "#f6d5ee", "#f2c0e5", "#edabdd", "#e996d4", "#e481cb", "#e06cc3", "#db57ba", "#d742b2", "#d22da9", "#bd2898", "#a82487", "#931f76", "#7e1b65", "#541244"},
        //new string [] { "#fbeaee", "#f6d5dd", "#f2c0cc", "#edabbb", "#e996ab", "#e4819a", "#e06c89", "#db5778", "#d74267", "#d22d56", "#bd284d", "#a82445", "#931f3c", "#7e1b34", "#541222"}
        //};

        public static int[][] ColorSequence = new int[][]
        {
        new int [] { 0xf6d5d5, 0xf2c0c0, 0xedabab, 0xe99696, 0xe48181, 0xe06c6c, 0xdb5757, 0xd74242, 0xd22d2d, 0xbd2828, 0xa82424, 0x931f1f, 0x7e1b1b, 0x691616, 0x541212},
        new int [] { 0xfbeeea, 0xf6ddd5, 0xf2ccc0, 0xedbbab, 0xe9ab96, 0xe49a81, 0xe0896c, 0xdb7857, 0xd76742, 0xd2562d, 0xbd4d28, 0xa84524, 0x933c1f, 0x7e341b, 0x542212},
        new int [] { 0xfbf2ea, 0xf6e6d5, 0xf2d9c0, 0xedccab, 0xe9bf96, 0xe4b381, 0xe0a66c, 0xdb9957, 0xd78c42, 0xd2802d, 0xbd7328, 0xa86624, 0x93591f, 0x7e4d1b, 0x543312},
        new int [] { 0xfbf6ea, 0xf6eed5, 0xf2e5c0, 0xedddab, 0xe9d496, 0xe4cb81, 0xe0c36c, 0xdbba57, 0xd7b242, 0xd2a92d, 0xbd9828, 0xa88724, 0x93761f, 0x7e651b, 0x544412},
        new int [] { 0xfbfbea, 0xf6f6d5, 0xf2f2c0, 0xededab, 0xe9e996, 0xe4e481, 0xe0e06c, 0xdbdb57, 0xd7d742, 0xd2d22d, 0xbdbd28, 0xa8a824, 0x93931f, 0x7e7e1b, 0x545412},
        new int [] { 0xf6fbea, 0xeef6d5, 0xe5f2c0, 0xddedab, 0xd4e996, 0xcbe481, 0xc3e06c, 0xbadb57, 0xb2d742, 0xa9d22d, 0x98bd28, 0x87a824, 0x76931f, 0x657e1b, 0x445412},
        new int [] { 0xeefbea, 0xddf6d5, 0xccf2c0, 0xbbedab, 0xabe996, 0x9ae481, 0x89e06c, 0x78db57, 0x67d742, 0x56d22d, 0x4dbd28, 0x45a824, 0x3c931f, 0x347e1b, 0x225412},
        new int [] { 0xeafbf2, 0xd5f6e6, 0xc0f2d9, 0xabedcc, 0x96e9bf, 0x81e4b3, 0x6ce0a6, 0x57db99, 0x42d78c, 0x2dd280, 0x28bd73, 0x24a866, 0x1f9359, 0x1b7e4d, 0x125433},
        new int [] { 0xeafbfb, 0xd5f6f6, 0xc0f2f2, 0xabeded, 0x96e9e9, 0x81e4e4, 0x6ce0e0, 0x57dbdb, 0x42d7d7, 0x2dd2d2, 0x28bdbd, 0x24a8a8, 0x1f9393, 0x1b7e7e, 0x125454},
        new int [] { 0xeaf6fb, 0xd5eef6, 0xc0e5f2, 0xabdded, 0x96d4e9, 0x81cbe4, 0x6cc3e0, 0x57badb, 0x42b2d7, 0x2da9d2, 0x2898bd, 0x2487a8, 0x1f7693, 0x1b657e, 0x124454},
        new int [] { 0xeaf2fb, 0xd5e6f6, 0xc0d9f2, 0xabcced, 0x96bfe9, 0x81b3e4, 0x6ca6e0, 0x5799db, 0x428cd7, 0x2d80d2, 0x2873bd, 0x2466a8, 0x1f5993, 0x1b4d7e, 0x123354},
        new int [] { 0xeaeafb, 0xd5d5f6, 0xc0c0f2, 0xababed, 0x9696e9, 0x8181e4, 0x6c6ce0, 0x5757db, 0x4242d7, 0x2d2dd2, 0x2828bd, 0x2424a8, 0x1f1f93, 0x1b1b7e, 0x121254},
        new int [] { 0xf6eafb, 0xeed5f6, 0xe5c0f2, 0xddabed, 0xd496e9, 0xcb81e4, 0xc36ce0, 0xba57db, 0xb242d7, 0xa92dd2, 0x9828bd, 0x8724a8, 0x761f93, 0x651b7e, 0x441254},
        new int [] { 0xfbeaf6, 0xf6d5ee, 0xf2c0e5, 0xedabdd, 0xe996d4, 0xe481cb, 0xe06cc3, 0xdb57ba, 0xd742b2, 0xd22da9, 0xbd2898, 0xa82487, 0x931f76, 0x7e1b65, 0x541244},
        new int [] { 0xfbeaee, 0xf6d5dd, 0xf2c0cc, 0xedabbb, 0xe996ab, 0xe4819a, 0xe06c89, 0xdb5778, 0xd74267, 0xd22d56, 0xbd284d, 0xa82445, 0x931f3c, 0x7e1b34, 0x541222}
        };

    
    }
}
