// The base turn rules shared by every fight: Prana per turn, cards drawn per turn, and the hand limit.
// Relics, allies and powers modify these in the combat model; the asset itself never changes at runtime.
using UnityEngine;

namespace JourneyToLanka.Combat
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Combat Rules", fileName = "CombatRules")]
    public sealed class CombatRulesSO : ScriptableObject
    {
        [SerializeField, Min(0)] private int _pranaPerTurn = 3;
        [SerializeField, Min(0)] private int _cardsPerTurn = 5;
        [SerializeField, Min(1)] private int _maxHandSize = 10;

        public int PranaPerTurn => _pranaPerTurn;

        public int CardsPerTurn => _cardsPerTurn;

        public int MaxHandSize => _maxHandSize;
    }
}
