// Writes a chain multiplier from its whole percent into a localized pattern: every {0} in the pattern becomes the
// multiplier (100 -> 1, 150 -> 1.5, 125 -> 1.25, 105 -> 1.05, 300 -> 3), everything else is copied as it stands.
// A missing pattern writes the bare multiplier. The decimal separator is a dot. Plain C# writing into a caller-owned
// StringBuilder, so it is EditMode tested; the chain bar calls it only when a chain kind or the pattern changes.
using System.Text;

namespace JourneyToLanka.CombatUI
{
    public static class ChainMultiplierFormat
    {
        private const string Placeholder = "{0}";

        public static void Format(string pattern, int percent, StringBuilder output)
        {
            output.Clear();
            if (string.IsNullOrEmpty(pattern))
            {
                AppendMultiplier(percent, output);
                return;
            }

            int i = 0;
            while (i < pattern.Length)
            {
                if (string.CompareOrdinal(pattern, i, Placeholder, 0, Placeholder.Length) == 0)
                {
                    AppendMultiplier(percent, output);
                    i += Placeholder.Length;
                    continue;
                }

                output.Append(pattern[i]);
                i++;
            }
        }

        public static void AppendMultiplier(int percent, StringBuilder output)
        {
            if (percent < 0)
            {
                percent = 0;
            }

            output.Append(percent / 100);
            int fraction = percent % 100;
            if (fraction == 0)
            {
                return;
            }

            output.Append('.');
            output.Append((char)('0' + fraction / 10));
            int hundredths = fraction % 10;
            if (hundredths != 0)
            {
                output.Append((char)('0' + hundredths));
            }
        }
    }
}
