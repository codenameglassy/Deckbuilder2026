// Rama's base stats for a fresh run: maximum health and the starting deck.
// Health carried between fights belongs to run state, not to this asset.
using JourneyToLanka.Combat.Cards;
using UnityEngine;

namespace JourneyToLanka.Combat
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Player Definition", fileName = "Player")]
    public sealed class PlayerDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _nameKey;
        [SerializeField, Min(1)] private int _maxHealth = 78;
        [SerializeField] private StartingDeckSO _startingDeck;

        public string NameKey => _nameKey;

        public int MaxHealth => _maxHealth;

        public StartingDeckSO StartingDeck => _startingDeck;
    }
}
