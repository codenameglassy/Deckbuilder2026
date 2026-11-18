// The localized names of shared definition assets (statuses, special arrows) for combat feedback popups, one
// LocalizedString per asset, created the first time that asset is asked for and kept up to date when the player
// changes language. Get returns the latest name, or the given fallback (the asset's id) until its string table has
// loaded or when the asset has no name key. Dispose unsubscribes every string.
using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using Object = UnityEngine.Object;

namespace JourneyToLanka.CombatScene
{
    internal sealed class LocalizedNameCache : IDisposable
    {
        private readonly string _table;
        private readonly Dictionary<Object, Entry> _entries = new Dictionary<Object, Entry>(8);

        public LocalizedNameCache(string table)
        {
            _table = table;
        }

        public string Get(Object asset, string nameKey, string fallback)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            if (!_entries.TryGetValue(asset, out Entry entry))
            {
                entry = new Entry(_table, nameKey, fallback);
                _entries.Add(asset, entry);
            }

            return entry.Value;
        }

        public void Dispose()
        {
            foreach (Entry entry in _entries.Values)
            {
                entry.Dispose();
            }

            _entries.Clear();
        }

        private sealed class Entry : IDisposable
        {
            private readonly LocalizedString _string;
            private readonly LocalizedString.ChangeHandler _changed;

            public Entry(string table, string nameKey, string fallback)
            {
                Value = fallback ?? string.Empty;
                if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(nameKey))
                {
                    return;
                }

                _string = new LocalizedString(table, nameKey);
                _changed = HandleChanged;
                _string.StringChanged += _changed;
            }

            public string Value { get; private set; }

            public void Dispose()
            {
                if (_string != null)
                {
                    _string.StringChanged -= _changed;
                }
            }

            private void HandleChanged(string value)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Value = value;
                }
            }
        }
    }
}
