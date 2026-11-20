// Binds one TextMeshPro text to a localization key whose text is a pattern with numbered placeholders, such as a
// card description ("Deal {0} damage."), and keeps it filled with the current values through CardTextFormatter.
// Before the numbers go in, keywords from an optional CardKeywordSet are coloured (CardKeywordDecorator); the
// decorated pattern is kept and rebuilt only when the pattern or the keywords change. It re-renders when the string
// table loads, when the player changes language, when the keywords change, when the values change and when the
// highlight colours change. The entries must be plain (not Smart) strings, so the placeholders arrive untouched.
// Clear unsubscribes from the string and the keywords, so pooled views call it when they are returned. Formatting
// reuses one StringBuilder for the numbers and one for the keywords.
using System;
using System.Collections.Generic;
using System.Text;
using JourneyToLanka.Combat.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace JourneyToLanka.CombatUI
{
    public sealed class LocalizedPatternText
    {
        private const int BuilderCapacity = 160;

        private readonly TMP_Text _target;
        private readonly LocalizedString _string = new LocalizedString();
        private readonly StringBuilder _builder = new StringBuilder(BuilderCapacity);
        private readonly StringBuilder _keywordBuilder = new StringBuilder(BuilderCapacity);
        private readonly Action _keywordsChanged;
        private CardKeywordSet _keywords;
        private bool _keywordsSubscribed;
        private string _decorated;
        private int _decoratedVersion = -1;
        private IReadOnlyList<CardValue> _values = Array.Empty<CardValue>();
        private string _pattern;
        private string _buffedColorTag;
        private string _weakenedColorTag;
        private bool _subscribed;

        public LocalizedPatternText(TMP_Text target)
        {
            _target = target;
            _keywordsChanged = HandleKeywordsChanged;
        }

        public void SetKeywords(CardKeywordSet keywords)
        {
            if (_keywords == keywords)
            {
                return;
            }

            SubscribeKeywords(false);
            _keywords = keywords;
            _decorated = null;
            SubscribeKeywords(_subscribed);
            Render();
        }

        public void Set(string table, string key)
        {
            if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(key))
            {
                Clear();
                return;
            }

            _string.TableReference = table;
            _string.TableEntryReference = key;

            if (_subscribed)
            {
                _string.RefreshString();
                return;
            }

            _string.StringChanged += HandleStringChanged;
            _subscribed = true;
            SubscribeKeywords(true);
        }

        public void SetValues(IReadOnlyList<CardValue> values)
        {
            _values = values ?? Array.Empty<CardValue>();
            Render();
        }

        public void SetColors(Color buffed, Color weakened)
        {
            _buffedColorTag = "<color=#" + ColorUtility.ToHtmlStringRGBA(buffed) + ">";
            _weakenedColorTag = "<color=#" + ColorUtility.ToHtmlStringRGBA(weakened) + ">";
            Render();
        }

        public void Clear()
        {
            if (_subscribed)
            {
                _string.StringChanged -= HandleStringChanged;
                _subscribed = false;
            }

            SubscribeKeywords(false);
            _pattern = null;
            _decorated = null;
            _values = Array.Empty<CardValue>();
            _target.text = string.Empty;
        }

        private void HandleStringChanged(string value)
        {
            _pattern = value;
            _decorated = null;
            Render();
        }

        private void HandleKeywordsChanged()
        {
            Render();
        }

        private void SubscribeKeywords(bool subscribe)
        {
            if (_keywords == null || _keywordsSubscribed == subscribe)
            {
                return;
            }

            if (subscribe)
            {
                _keywords.Changed += _keywordsChanged;
            }
            else
            {
                _keywords.Changed -= _keywordsChanged;
            }

            _keywordsSubscribed = subscribe;
        }

        private string DecoratedPattern()
        {
            if (_keywords == null)
            {
                return _pattern;
            }

            if (_decorated == null || _decoratedVersion != _keywords.Version)
            {
                CardKeywordDecorator.Decorate(_pattern, _keywords.Keywords, _keywordBuilder);
                _decorated = _keywordBuilder.ToString();
                _decoratedVersion = _keywords.Version;
            }

            return _decorated;
        }

        private void Render()
        {
            if (_pattern == null)
            {
                return;
            }

            CardTextFormatter.Format(DecoratedPattern(), _values, _buffedColorTag, _weakenedColorTag, _builder);
            _target.SetText(_builder);
        }
    }
}
