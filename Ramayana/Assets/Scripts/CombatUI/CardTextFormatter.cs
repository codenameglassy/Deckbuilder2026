// Fills a card description pattern with its live numbers: each {n} becomes value n's current number, wrapped in a
// rich-text colour tag when it is buffed or weakened. {{ and }} write literal braces. A placeholder with no matching
// value, or anything else in braces, is written as it stands, so a translator's slip never throws or loses text.
// Plain C# writing into a caller-owned StringBuilder, so it is EditMode tested and allocates nothing per call.
using System.Collections.Generic;
using System.Text;
using JourneyToLanka.Combat.Effects;

namespace JourneyToLanka.CombatUI
{
    public static class CardTextFormatter
    {
        private const int MaxIndexDigits = 3;
        private const string CloseColorTag = "</color>";

        public static void Format(string pattern, IReadOnlyList<CardValue> values, string buffedColorTag,
            string weakenedColorTag, StringBuilder output)
        {
            output.Clear();
            if (string.IsNullOrEmpty(pattern))
            {
                return;
            }

            int length = pattern.Length;
            int i = 0;
            while (i < length)
            {
                char c = pattern[i];
                if (c == '{')
                {
                    if (i + 1 < length && pattern[i + 1] == '{')
                    {
                        output.Append('{');
                        i += 2;
                        continue;
                    }

                    if (TryReadPlaceholder(pattern, i, out int index, out int end) && values != null && index < values.Count)
                    {
                        AppendValue(values[index], buffedColorTag, weakenedColorTag, output);
                        i = end + 1;
                        continue;
                    }
                }
                else if (c == '}' && i + 1 < length && pattern[i + 1] == '}')
                {
                    output.Append('}');
                    i += 2;
                    continue;
                }

                output.Append(c);
                i++;
            }
        }

        private static bool TryReadPlaceholder(string pattern, int open, out int index, out int close)
        {
            index = 0;
            close = -1;
            int digits = 0;
            for (int i = open + 1; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (c == '}')
                {
                    close = i;
                    return digits > 0;
                }

                if (c < '0' || c > '9' || digits == MaxIndexDigits)
                {
                    return false;
                }

                index = index * 10 + (c - '0');
                digits++;
            }

            return false;
        }

        private static void AppendValue(in CardValue value, string buffedColorTag, string weakenedColorTag,
            StringBuilder output)
        {
            string colorTag = value.IsBuffed ? buffedColorTag : value.IsWeakened ? weakenedColorTag : null;
            if (string.IsNullOrEmpty(colorTag))
            {
                output.Append(value.Current);
                return;
            }

            output.Append(colorTag);
            output.Append(value.Current);
            output.Append(CloseColorTag);
        }
    }
}
