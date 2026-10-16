// Binds one TextMeshPro text to a localization key. The text updates when its string table loads and again
// whenever the player changes language. Clear unsubscribes, so pooled views call it when they are returned.
using TMPro;
using UnityEngine.Localization;

namespace JourneyToLanka.CombatUI
{
    public sealed class LocalizedText
    {
        private readonly TMP_Text _target;
        private readonly LocalizedString _string = new LocalizedString();
        private bool _subscribed;

        public LocalizedText(TMP_Text target)
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

        public void Clear()
        {
            if (_subscribed)
            {
                _string.StringChanged -= HandleStringChanged;
                _subscribed = false;
            }

            _target.text = string.Empty;
        }

        private void HandleStringChanged(string value)
        {
            _target.text = value;
        }
    }
}
