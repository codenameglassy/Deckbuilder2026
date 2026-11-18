// The line on the card reward screen that reports a special arrow dropped by the fight: "Arrow of Agni goes into your
// quiver." or, when every slot was full, "... is left behind. Your quiver is full." The sentence is a UI string with
// a {0} placeholder where the arrow's localized name goes, drawn in the arrow's colour, so each language can place the
// name where its grammar needs it. Both strings follow the player's language. Show binds an arrow, Hide clears the
// line and unsubscribes. It allocates only when the line is shown or the language changes, never per frame.
using System;
using System.Text;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace JourneyToLanka.CombatUI
{
    internal sealed class RewardArrowLine : IDisposable
    {
        private const string Placeholder = "{0}";
        private const string ColorClose = "</color>";

        private readonly TMP_Text _target;
        private readonly LocalizedString _name = new LocalizedString();
        private readonly LocalizedString _sentence = new LocalizedString();
        private readonly LocalizedString.ChangeHandler _nameChanged;
        private readonly LocalizedString.ChangeHandler _sentenceChanged;
        private readonly StringBuilder _builder = new StringBuilder(96);
        private string _nameValue;
        private string _sentenceValue;
        private string _colorOpen;
        private bool _subscribed;

        public RewardArrowLine(TMP_Text target)
        {
            _target = target;
            _nameChanged = HandleNameChanged;
            _sentenceChanged = HandleSentenceChanged;
            Hide();
        }

        public void Show(ArrowDefinitionSO arrow, bool kept)
        {
            if (arrow == null || string.IsNullOrEmpty(arrow.NameKey))
            {
                Hide();
                return;
            }

            _colorOpen = "<color=#" + ColorUtility.ToHtmlStringRGBA(arrow.Color) + ">";
            _nameValue = arrow.Id;
            _sentenceValue = null;
            _name.TableReference = LocalizationTableNames.Combat;
            _name.TableEntryReference = arrow.NameKey;
            _sentence.TableReference = LocalizationTableNames.UI;
            _sentence.TableEntryReference = kept ? CardRewardKeys.ArrowKept : CardRewardKeys.ArrowLeftBehind;
            _target.gameObject.SetActive(true);

            if (_subscribed)
            {
                _name.RefreshString();
                _sentence.RefreshString();
                return;
            }

            _name.StringChanged += _nameChanged;
            _sentence.StringChanged += _sentenceChanged;
            _subscribed = true;
        }

        public void Hide()
        {
            if (_subscribed)
            {
                _name.StringChanged -= _nameChanged;
                _sentence.StringChanged -= _sentenceChanged;
                _subscribed = false;
            }

            _nameValue = null;
            _sentenceValue = null;
            _target.text = string.Empty;
            _target.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            Hide();
        }

        private void HandleNameChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _nameValue = value;
            }

            Render();
        }

        private void HandleSentenceChanged(string value)
        {
            _sentenceValue = value;
            Render();
        }

        private void Render()
        {
            if (_sentenceValue == null || _nameValue == null)
            {
                return;
            }

            _builder.Clear();
            int index = _sentenceValue.IndexOf(Placeholder, StringComparison.Ordinal);
            if (index < 0)
            {
                AppendName();
                _builder.Append(' ');
                _builder.Append(_sentenceValue);
            }
            else
            {
                _builder.Append(_sentenceValue, 0, index);
                AppendName();
                int after = index + Placeholder.Length;
                _builder.Append(_sentenceValue, after, _sentenceValue.Length - after);
            }

            _target.SetText(_builder);
        }

        private void AppendName()
        {
            _builder.Append(_colorOpen);
            _builder.Append(_nameValue);
            _builder.Append(ColorClose);
        }
    }
}
