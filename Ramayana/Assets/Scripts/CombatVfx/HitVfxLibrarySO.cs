// Which hit profile each source's blows use: card types, enemies and arrows each map to a profile, with a default per
// kind for anything unlisted, and round-end damage (Dahan's burn) has its own. The director asks the method for the
// kind of source it is announcing, so no code branches on a source's type; a new card type, enemy or arrow is a new
// row, never new code. A null result means "use the style's default profile". Lookups scan a handful of rows and
// allocate nothing. Shared and read-only at runtime; Changed (Editor only) lets the spawners add pools for new looks.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Enemies;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat VFX/Hit VFX Library", fileName = "HitVfxLibrary")]
    public sealed class HitVfxLibrarySO : ScriptableObject
    {
        [Header("Cards")]
        [SerializeField] private HitVfxEntry<CardTypeSO>[] _cardTypes = Array.Empty<HitVfxEntry<CardTypeSO>>();
        [SerializeField] private HitVfxProfileSO _cardDefault;

        [Header("Enemies")]
        [SerializeField] private HitVfxEntry<EnemyDefinitionSO>[] _enemies = Array.Empty<HitVfxEntry<EnemyDefinitionSO>>();
        [SerializeField] private HitVfxProfileSO _enemyDefault;

        [Header("Arrows")]
        [SerializeField] private HitVfxEntry<ArrowDefinitionSO>[] _arrows = Array.Empty<HitVfxEntry<ArrowDefinitionSO>>();
        [SerializeField] private HitVfxProfileSO _arrowDefault;

        [Header("Round End")]
        [Tooltip("Damage that lands at round end, such as Dahan's burn.")]
        [SerializeField] private HitVfxProfileSO _roundEnd;

        public event Action Changed;

        public HitVfxProfileSO RoundEnd => _roundEnd;

        public HitVfxProfileSO ForCardType(CardTypeSO cardType)
        {
            return Find(_cardTypes, cardType) ?? _cardDefault;
        }

        public HitVfxProfileSO ForEnemy(EnemyDefinitionSO enemy)
        {
            return Find(_enemies, enemy) ?? _enemyDefault;
        }

        public HitVfxProfileSO ForArrow(ArrowDefinitionSO arrow)
        {
            return Find(_arrows, arrow) ?? _arrowDefault;
        }

        // Every profile this library can return, each once. Used at start to prewarm pools, never per frame.
        public void CollectProfiles(List<HitVfxProfileSO> profiles)
        {
            if (profiles == null)
            {
                return;
            }

            AddEntries(_cardTypes, profiles);
            AddEntries(_enemies, profiles);
            AddEntries(_arrows, profiles);
            AddUnique(_cardDefault, profiles);
            AddUnique(_enemyDefault, profiles);
            AddUnique(_arrowDefault, profiles);
            AddUnique(_roundEnd, profiles);
        }

        private static HitVfxProfileSO Find<TKey>(HitVfxEntry<TKey>[] entries, TKey key) where TKey : UnityEngine.Object
        {
            if (entries == null || key == null)
            {
                return null;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Key == key && entries[i].Profile != null)
                {
                    return entries[i].Profile;
                }
            }

            return null;
        }

        private static void AddEntries<TKey>(HitVfxEntry<TKey>[] entries, List<HitVfxProfileSO> profiles) where TKey : UnityEngine.Object
        {
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                AddUnique(entries[i].Profile, profiles);
            }
        }

        private static void AddUnique(HitVfxProfileSO profile, List<HitVfxProfileSO> profiles)
        {
            if (profile != null && !profiles.Contains(profile))
            {
                profiles.Add(profile);
            }
        }

        private void OnDisable()
        {
            Changed = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}
