// EditMode tests for HitVfxLibrarySO: a listed card type gets its own profile, an unlisted or missing one gets the
// card default, enemies and arrows fall back to their defaults, round-end damage has its own profile, CollectProfiles
// lists every reachable profile once, and HitSource carries the profile it was tagged with.
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Cards;
using JourneyToLanka.Combat.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.Tests
{
    public sealed class HitVfxLibraryTests
    {
        private readonly List<Object> _created = new List<Object>();
        private HitVfxLibrarySO _library;
        private CardTypeSO _attack;
        private CardTypeSO _skill;
        private HitVfxProfileSO _attackProfile;
        private HitVfxProfileSO _cardDefault;
        private HitVfxProfileSO _enemyDefault;
        private HitVfxProfileSO _arrowDefault;
        private HitVfxProfileSO _roundEnd;

        [SetUp]
        public void SetUp()
        {
            _library = Create<HitVfxLibrarySO>();
            _attack = Create<CardTypeSO>();
            _skill = Create<CardTypeSO>();
            _attackProfile = Create<HitVfxProfileSO>();
            _cardDefault = Create<HitVfxProfileSO>();
            _enemyDefault = Create<HitVfxProfileSO>();
            _arrowDefault = Create<HitVfxProfileSO>();
            _roundEnd = Create<HitVfxProfileSO>();

            using var serialized = new SerializedObject(_library);
            SerializedProperty cardTypes = serialized.FindProperty("_cardTypes");
            cardTypes.arraySize = 2;
            SetEntry(cardTypes.GetArrayElementAtIndex(0), _attack, _attackProfile);
            SetEntry(cardTypes.GetArrayElementAtIndex(1), _skill, _cardDefault);
            serialized.FindProperty("_cardDefault").objectReferenceValue = _cardDefault;
            serialized.FindProperty("_enemyDefault").objectReferenceValue = _enemyDefault;
            serialized.FindProperty("_arrowDefault").objectReferenceValue = _arrowDefault;
            serialized.FindProperty("_roundEnd").objectReferenceValue = _roundEnd;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        [Test]
        public void Listed_Card_Type_Gets_Its_Profile()
        {
            Assert.AreSame(_attackProfile, _library.ForCardType(_attack));
        }

        [Test]
        public void Unlisted_Or_Missing_Card_Type_Gets_Card_Default()
        {
            Assert.AreSame(_cardDefault, _library.ForCardType(Create<CardTypeSO>()));
            Assert.AreSame(_cardDefault, _library.ForCardType(null));
        }

        [Test]
        public void Enemies_And_Arrows_Fall_Back_To_Their_Defaults()
        {
            Assert.AreSame(_enemyDefault, _library.ForEnemy(Create<EnemyDefinitionSO>()));
            Assert.AreSame(_arrowDefault, _library.ForArrow(Create<ArrowDefinitionSO>()));
        }

        [Test]
        public void Round_End_Has_Its_Own_Profile()
        {
            Assert.AreSame(_roundEnd, _library.RoundEnd);
        }

        [Test]
        public void CollectProfiles_Lists_Each_Profile_Once()
        {
            var profiles = new List<HitVfxProfileSO>();

            _library.CollectProfiles(profiles);

            Assert.AreEqual(5, profiles.Count);
            CollectionAssert.AllItemsAreUnique(profiles);
            CollectionAssert.Contains(profiles, _attackProfile);
            CollectionAssert.Contains(profiles, _cardDefault);
            CollectionAssert.Contains(profiles, _enemyDefault);
            CollectionAssert.Contains(profiles, _arrowDefault);
            CollectionAssert.Contains(profiles, _roundEnd);
        }

        [Test]
        public void HitSource_Carries_Its_Profile()
        {
            var source = new HitSource(_attack, HitSource.PlayerSlot, _attackProfile);

            Assert.AreSame(_attackProfile, source.Profile);
            Assert.IsNull(HitSource.None.Profile);
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);
            return instance;
        }

        private static void SetEntry(SerializedProperty entry, Object key, HitVfxProfileSO profile)
        {
            entry.FindPropertyRelative("_key").objectReferenceValue = key;
            entry.FindPropertyRelative("_profile").objectReferenceValue = profile;
        }
    }
}
