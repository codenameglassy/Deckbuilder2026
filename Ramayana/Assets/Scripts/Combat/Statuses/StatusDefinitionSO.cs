// One kind of status as a Type Object: stable id, localization keys (name, description, and a short label the UI
// shows on a status chip until the status has an icon), icon and colour for the UI, whether it is a
// debuff (for cleansing), the most it can stack to (0 for no cap; Bhaya and Moha cap at 3), how many turns it lasts
// when applied without a duration, and three strategies: how it wears off (decay), how it changes damage (damage
// rule), and what it does when its holder's turn starts or its holder gains a status (trigger, as the powers Vow of
// the Exile and Devotion do). Any strategy may be empty: Dharma has none and is simply a count that stays. Powers are
// statuses with no decay, so they last the whole fight. Shared and immutable.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Statuses/Status", fileName = "Status")]
    public sealed class StatusDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [SerializeField] private string _descriptionKey;
        [SerializeField] private string _shortNameKey;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private bool _isDebuff;
        [SerializeField, Min(0)] private int _maxAmount;
        [SerializeField, Min(0)] private int _defaultDuration;
        [SerializeField] private StatusDecaySO _decay;
        [SerializeField] private StatusDamageRuleSO _damageRule;
        [SerializeField] private StatusTriggerSO _trigger;

        public string Id => _id;

        public string NameKey => _nameKey;

        public string DescriptionKey => _descriptionKey;

        public string ShortNameKey => _shortNameKey;

        public Sprite Icon => _icon;

        public Color Color => _color;

        public bool IsDebuff => _isDebuff;

        public int MaxAmount => _maxAmount;

        public int DefaultDuration => _defaultDuration;

        public StatusDecaySO Decay => _decay;

        public StatusDamageRuleSO DamageRule => _damageRule;

        public StatusTriggerSO Trigger => _trigger;

        public int DamageOrder => _damageRule != null ? _damageRule.Order : int.MaxValue;

        public int Cap(int amount)
        {
            return _maxAmount > 0 && amount > _maxAmount ? _maxAmount : amount;
        }
    }
}
