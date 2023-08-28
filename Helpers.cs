﻿using Google.Protobuf.Collections;
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
                    value = kv.Value.StringValue;
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
                    return kv.Value.StringValue;
                }
            }
            return defaultValue;
        }

        public static Dictionary<string, string> ToDictionary(this RepeatedField<KeyValue> attributes)
        {
            var dict = new Dictionary<string, string>();
            foreach (var kv in attributes)
            {
                string value="<empty>";
                switch (kv.Value.ValueCase)
                {
                    case AnyValue.ValueOneofCase.StringValue:
                        value = kv.Value.StringValue;
                        break;
                    case AnyValue.ValueOneofCase.IntValue:
                        value = kv.Value.IntValue.ToString();
                        break;
                    case AnyValue.ValueOneofCase.DoubleValue:
                        value = kv.Value.DoubleValue.ToString();
                        break;
                    case AnyValue.ValueOneofCase.BoolValue:
                        value = kv.Value.BoolValue.ToString();
                        break;
                    case AnyValue.ValueOneofCase.BytesValue:
                        value = kv.Value.BytesValue.ToHexString();
                        break;
                    default:
                        value = kv.Value.ToString();
                        break;
                }
                dict.Add(kv.Key, kv.Value.StringValue);
            }
            return dict;
        }

        public static string ConcatString(this Dictionary<string, string> dict)
        {
            StringBuilder sb = new();
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                sb.Append($"{kv.Key}: ");
                sb.Append((String.IsNullOrEmpty(kv.Value)) ? "\'\'" : kv.Value);
            }
            return sb.ToString();
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
            long unixTimeTicks = (long)(unixTimeNanoSeconds / 100);
            long ticks = unixTimeTicks + UnixEpochTicks;
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

        public static string[][] BarColors = new string[][]
        {
        new string [] { "#f6d5d5", "#f2c0c0", "#edabab", "#e99696", "#e48181", "#e06c6c", "#db5757", "#d74242", "#d22d2d", "#bd2828", "#a82424", "#931f1f", "#7e1b1b", "#691616", "#541212"},
        new string [] { "#fbeeea", "#f6ddd5", "#f2ccc0", "#edbbab", "#e9ab96", "#e49a81", "#e0896c", "#db7857", "#d76742", "#d2562d", "#bd4d28", "#a84524", "#933c1f", "#7e341b", "#542212"},
        new string [] { "#fbf2ea", "#f6e6d5", "#f2d9c0", "#edccab", "#e9bf96", "#e4b381", "#e0a66c", "#db9957", "#d78c42", "#d2802d", "#bd7328", "#a86624", "#93591f", "#7e4d1b", "#543312"},
        new string [] { "#fbf6ea", "#f6eed5", "#f2e5c0", "#edddab", "#e9d496", "#e4cb81", "#e0c36c", "#dbba57", "#d7b242", "#d2a92d", "#bd9828", "#a88724", "#93761f", "#7e651b", "#544412"},
        new string [] { "#fbfbea", "#f6f6d5", "#f2f2c0", "#ededab", "#e9e996", "#e4e481", "#e0e06c", "#dbdb57", "#d7d742", "#d2d22d", "#bdbd28", "#a8a824", "#93931f", "#7e7e1b", "#545412"},
        new string [] { "#f6fbea", "#eef6d5", "#e5f2c0", "#ddedab", "#d4e996", "#cbe481", "#c3e06c", "#badb57", "#b2d742", "#a9d22d", "#98bd28", "#87a824", "#76931f", "#657e1b", "#445412"},
        new string [] { "#eefbea", "#ddf6d5", "#ccf2c0", "#bbedab", "#abe996", "#9ae481", "#89e06c", "#78db57", "#67d742", "#56d22d", "#4dbd28", "#45a824", "#3c931f", "#347e1b", "#225412"},
        new string [] { "#eafbf2", "#d5f6e6", "#c0f2d9", "#abedcc", "#96e9bf", "#81e4b3", "#6ce0a6", "#57db99", "#42d78c", "#2dd280", "#28bd73", "#24a866", "#1f9359", "#1b7e4d", "#125433"},
        new string [] { "#eafbfb", "#d5f6f6", "#c0f2f2", "#abeded", "#96e9e9", "#81e4e4", "#6ce0e0", "#57dbdb", "#42d7d7", "#2dd2d2", "#28bdbd", "#24a8a8", "#1f9393", "#1b7e7e", "#125454"},
        new string [] { "#eaf6fb", "#d5eef6", "#c0e5f2", "#abdded", "#96d4e9", "#81cbe4", "#6cc3e0", "#57badb", "#42b2d7", "#2da9d2", "#2898bd", "#2487a8", "#1f7693", "#1b657e", "#124454"},
        new string [] { "#eaf2fb", "#d5e6f6", "#c0d9f2", "#abcced", "#96bfe9", "#81b3e4", "#6ca6e0", "#5799db", "#428cd7", "#2d80d2", "#2873bd", "#2466a8", "#1f5993", "#1b4d7e", "#123354"},
        new string [] { "#eaeafb", "#d5d5f6", "#c0c0f2", "#ababed", "#9696e9", "#8181e4", "#6c6ce0", "#5757db", "#4242d7", "#2d2dd2", "#2828bd", "#2424a8", "#1f1f93", "#1b1b7e", "#121254"},
        new string [] { "#f6eafb", "#eed5f6", "#e5c0f2", "#ddabed", "#d496e9", "#cb81e4", "#c36ce0", "#ba57db", "#b242d7", "#a92dd2", "#9828bd", "#8724a8", "#761f93", "#651b7e", "#441254"},
        new string [] { "#fbeaf6", "#f6d5ee", "#f2c0e5", "#edabdd", "#e996d4", "#e481cb", "#e06cc3", "#db57ba", "#d742b2", "#d22da9", "#bd2898", "#a82487", "#931f76", "#7e1b65", "#541244"},
        new string [] { "#fbeaee", "#f6d5dd", "#f2c0cc", "#edabbb", "#e996ab", "#e4819a", "#e06c89", "#db5778", "#d74267", "#d22d56", "#bd284d", "#a82445", "#931f3c", "#7e1b34", "#541222"}
        };
    }
}
