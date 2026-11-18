// Rama's base stats for a fresh run: maximum health, the starting deck, how many special arrows his quiver holds
// (three in the reference build) and the arrows he starts with (none in the reference build; fill it to try arrows
// without winning fights first). Health and arrows carried between fights belong to run state, not to this asset.
// For testing, Dev Starting Deck replaces the starting deck in the Editor and in Development builds only; release
// builds compile that branch out and always use Starting Deck. UsesDevStartingDeck reports when the override is live
// so the run can warn about it.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
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
        [SerializeField, Min(0)] private int _quiverSize = 3;
        [SerializeField] private ArrowDefinitionSO[] _startingArrows = Array.Empty<ArrowDefinitionSO>();

        [Header("Development")]
        [Tooltip("Replaces Starting Deck in the Editor and in Development builds only. Release builds ignore it. " +
                 "Leave empty to play the real starting deck.")]
        [SerializeField] private StartingDeckSO _devStartingDeck;

        public string NameKey => _nameKey;

        public int MaxHealth => _maxHealth;

        public StartingDeckSO StartingDeck => UsesDevStartingDeck ? _devStartingDeck : _startingDeck;

        public bool UsesDevStartingDeck
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return _devStartingDeck != null;
#else
                return false;
#endif
            }
        }

        public int QuiverSize => _quiverSize;

        public IReadOnlyList<ArrowDefinitionSO> StartingArrows => _startingArrows;

        public Quiver CreateQuiver()
        {
            return new Quiver(_quiverSize, _startingArrows);
        }
    }
}
