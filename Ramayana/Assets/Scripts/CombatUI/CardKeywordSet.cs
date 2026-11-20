// The live keyword list for one CardKeywordsSO: each entry's word in the current language (through its own
// LocalizedString, so it follows table loads and language changes) paired with its colour tag, ordered longest word
// first so overlapping keywords resolve to the longer one. Changed fires whenever a word or a colour changes, so every
// card description using the set re-colours. Version counts those changes. One set per keywords asset, shared by
// every card (hand, flights, pile viewer, reward screen) through For. A small presentation cache like
// CardFrameMaterials, not a gameplay service; the cache is cleared and every subscription released when Play mode
// starts, so a disabled domain reload never carries stale sets between sessions. Keywords are rebuilt only when
// something changes; reading them allocates nothing.
using System;
using System.Collections.Generic;
using JourneyToLanka.Localization;
using UnityEngine;
using UnityEngine.Localization;

namespace JourneyToLanka.CombatUI
{
    public sealed class CardKeywordSet
    {
        private static readonly Dictionary<CardKeywordsSO, CardKeywordSet> Sets =
            new Dictionary<CardKeywordsSO, CardKeywordSet>(1);
        private static readonly Comparison<CardKeyword> LongestFirst = (a, b) => b.Word.Length.CompareTo(a.Word.Length);

        private readonly CardKeywordsSO _source;
        private readonly List<LocalizedString> _strings = new List<LocalizedString>(8);
        private readonly List<LocalizedString.ChangeHandler> _handlers = new List<LocalizedString.ChangeHandler>(8);
        private readonly List<string> _words = new List<string>(8);
        private readonly List<CardKeyword> _keywords = new List<CardKeyword>(8);

        private CardKeywordSet(CardKeywordsSO source)
        {
            _source = source;
            _source.Changed += Rebind;
            Bind();
        }

        public event Action Changed;

        public IReadOnlyList<CardKeyword> Keywords => _keywords;

        public int Version { get; private set; }

        public static CardKeywordSet For(CardKeywordsSO source)
        {
            if (source == null)
            {
                return null;
            }

            if (!Sets.TryGetValue(source, out CardKeywordSet set))
            {
                set = new CardKeywordSet(source);
                Sets.Add(source, set);
            }

            return set;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            foreach (CardKeywordSet set in Sets.Values)
            {
                set.Release();
            }

            Sets.Clear();
        }

        private void Bind()
        {
            int count = _source.Count;
            for (int i = 0; i < count; i++)
            {
                CardKeywordEntry entry = _source.Get(i);
                _words.Add(null);
                if (string.IsNullOrEmpty(entry.NameKey))
                {
                    _strings.Add(null);
                    _handlers.Add(null);
                    continue;
                }

                int index = i;
                var localized = new LocalizedString(LocalizationTableNames.Combat, entry.NameKey);
                LocalizedString.ChangeHandler handler = value => HandleWordChanged(index, value);
                localized.StringChanged += handler;
                _strings.Add(localized);
                _handlers.Add(handler);
            }

            RebuildKeywords();
        }

        private void Rebind()
        {
            Unbind();
            Bind();
        }

        private void Unbind()
        {
            for (int i = 0; i < _strings.Count; i++)
            {
                if (_strings[i] != null)
                {
                    _strings[i].StringChanged -= _handlers[i];
                }
            }

            _strings.Clear();
            _handlers.Clear();
            _words.Clear();
        }

        private void Release()
        {
            if (_source != null)
            {
                _source.Changed -= Rebind;
            }

            Unbind();
            _keywords.Clear();
            Changed = null;
        }

        private void HandleWordChanged(int index, string value)
        {
            if (index >= _words.Count || _words[index] == value)
            {
                return;
            }

            _words[index] = value;
            RebuildKeywords();
        }

        private void RebuildKeywords()
        {
            _keywords.Clear();
            int count = Mathf.Min(_words.Count, _source != null ? _source.Count : 0);
            for (int i = 0; i < count; i++)
            {
                string word = _words[i];
                if (string.IsNullOrEmpty(word))
                {
                    continue;
                }

                string tag = "<color=#" + ColorUtility.ToHtmlStringRGBA(_source.Get(i).Color) + ">";
                _keywords.Add(new CardKeyword(word, tag));
            }

            _keywords.Sort(LongestFirst);
            Version++;
            Changed?.Invoke();
        }
    }
}
