// Collects the outcome of a builder run and formats the single summary log.
using System.Collections.Generic;
using System.Text;

namespace JourneyToLanka.Core.EditorTools
{
    public sealed class AssetBuilderReport
    {
        private readonly List<string> _created = new List<string>();
        private readonly List<string> _wired = new List<string>();
        private readonly List<string> _skipped = new List<string>();
        private int _existedCount;

        public int CreatedCount => _created.Count;
        public int ExistedCount => _existedCount;
        public int WiredCount => _wired.Count;
        public int SkippedCount => _skipped.Count;

        public void AddCreated(string entry)
        {
            _created.Add(entry);
        }

        public void AddExisted(string entry)
        {
            _existedCount++;
        }

        public void AddWired(string entry)
        {
            _wired.Add(entry);
        }

        public void AddSkipped(string entry)
        {
            _skipped.Add(entry);
        }

        public string BuildSummary(string title)
        {
            var builder = new StringBuilder();
            builder.Append('[').Append(title).Append("] Default assets: ")
                .Append(_created.Count).Append(" created, ")
                .Append(_existedCount).Append(" already existed, ")
                .Append(_wired.Count).Append(" wired, ")
                .Append(_skipped.Count).Append(" skipped.");

            AppendSection(builder, "Created", _created);
            AppendSection(builder, "Wired", _wired);
            AppendSection(builder, "Skipped", _skipped);
            return builder.ToString();
        }

        private static void AppendSection(StringBuilder builder, string label, List<string> entries)
        {
            if (entries.Count == 0)
            {
                return;
            }

            builder.Append('\n').Append(label).Append(':');
            for (int i = 0; i < entries.Count; i++)
            {
                builder.Append("\n  ").Append(entries[i]);
            }
        }
    }
}
