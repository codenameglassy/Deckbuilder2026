// A card category (Attack, Skill, and later Dharma, Power, Invocation, Astra, Curse) as a Type Object.
// Systems that care about type compare by reference, for example a relic that counts Attacks.
// Visuals for each type live in the card style asset, not here.
using UnityEngine;

namespace JourneyToLanka.Combat.Cards
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Card Type", fileName = "CardType")]
    public sealed class CardTypeSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;

        public string Id => _id;

        public string NameKey => _nameKey;
    }
}
