// Binds one TextMeshPro text to a localization key whose text is a pattern with numbered placeholders, such as a
// card description ("Deal {0} damage."), and keeps it filled with the current values through CardTextFormatter.
// It re-renders when the string table loads, when the player changes language, when the values change and when the
// highlight colours change. The entries must be plain (not Smart) strings, so the placeholders arrive untouched.
// Clear unsubscribes, so pooled views call it when they are returned. Formatting reuses one StringBuilder.
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
        private IReadOnlyList<CardValue> _values = Array.Empty<CardValue>();
        private string _pattern;
        private string _buffedColorTag;
        private string _weakenedColorTag;
        private bool _subscribed;

        public LocalizedPatternText(TMP_Text target)
        {
            _target = target;
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

            _pattern = null;
            _values = Array.Empty<CardValue>();
            _target.text = string.Empty;
        }

        private void HandleStringChanged(string value)
        {
            _pattern = value;
            Render();
        }

        private void Render()
        {
            if (_pattern == null)
            {
                return;
            }

            CardTextFormatter.Format(_pattern, _values, _buffedColorTag, _weakenedColorTag, _builder);
            _target.SetText(_builder);
        }
    }
}
