// EditMode tests for StatusVfxProfileSO: a fighter shows auras for the statuses it holds in the profile's priority
// order, never more than the per-fighter limit; statuses that share an aura prefab show it once; a status without
// an aura is skipped; a status the fighter lost stops being chosen; and Find returns
// a status's row (none for a status the profile does not list).
using System.Collections.Generic;
using JourneyToLanka.Combat.Statuses;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.Tests
{
    public sealed class StatusVfxProfileTests
    {
        private readonly List<Object> _created = new List<Object>();
        private readonly List<int> _chosen = new List<int>();
        private StatusDefinitionSO _burn;
        private StatusDefinitionSO _fear;
        private StatusDefinitionSO _focus;
        private StatusDefinitionSO _powerA;
        private StatusDefinitionSO _powerB;
        private StatusDefinitionSO _unlisted;
        private GameObject _burnAura;
        private GameObject _fearAura;
        private GameObject _powerAura;

        [SetUp]
        public void SetUp()
        {
            _burn = Track(ScriptableObject.CreateInstance<StatusDefinitionSO>());
            _fear = Track(ScriptableObject.CreateInstance<StatusDefinitionSO>());
            _focus = Track(ScriptableObject.CreateInstance<StatusDefinitionSO>());
            _powerA = Track(ScriptableObject.CreateInstance<StatusDefinitionSO>());
            _powerB = Track(ScriptableObject.CreateInstance<StatusDefinitionSO>());
            _unlisted = Track(ScriptableObject.CreateInstance<StatusDefinitionSO>());
            _burnAura = Track(new GameObject("BurnAura"));
            _fearAura = Track(new GameObject("FearAura"));
            _powerAura = Track(new GameObject("PowerAura"));
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
        public void Auras_Follow_Priority_Order_Up_To_The_Limit()
        {
            StatusVfxProfileSO profile = CreateProfile(2);
            StatusSet statuses = Holding(_powerA, _fear, _burn);

            profile.SelectAuras(statuses, _chosen);

            CollectionAssert.AreEqual(new[] { 0, 1 }, _chosen);
        }

        [Test]
        public void Statuses_Sharing_An_Aura_Show_It_Once()
        {
            StatusVfxProfileSO profile = CreateProfile(3);
            StatusSet statuses = Holding(_powerA, _powerB, _burn);

            profile.SelectAuras(statuses, _chosen);

            CollectionAssert.AreEqual(new[] { 0, 2 }, _chosen);
        }

        [Test]
        public void Status_Without_An_Aura_Is_Skipped()
        {
            StatusVfxProfileSO profile = CreateProfile(2);
            StatusSet statuses = Holding(_focus, _powerB);

            profile.SelectAuras(statuses, _chosen);

            CollectionAssert.AreEqual(new[] { 3 }, _chosen);
        }

        [Test]
        public void Lost_Status_Is_No_Longer_Chosen()
        {
            StatusVfxProfileSO profile = CreateProfile(2);
            StatusSet statuses = Holding(_burn, _fear);
            statuses.Remove(_burn);

            profile.SelectAuras(statuses, _chosen);

            CollectionAssert.AreEqual(new[] { 1 }, _chosen);
        }

        [Test]
        public void Zero_Limit_Shows_No_Auras()
        {
            StatusVfxProfileSO profile = CreateProfile(0);

            profile.SelectAuras(Holding(_burn), _chosen);

            Assert.IsEmpty(_chosen);
        }

        [Test]
        public void Find_Returns_The_Row_Of_A_Listed_Status()
        {
            StatusVfxProfileSO profile = CreateProfile(2);

            Assert.AreSame(profile.EntryAt(1), profile.Find(_fear));
            Assert.IsNull(profile.Find(_unlisted));
            Assert.IsNull(profile.Find(null));
        }

        // Rows in priority order: burn, fear, power A, power B (sharing power A's aura), focus (no aura).
        private StatusVfxProfileSO CreateProfile(int maxAuras)
        {
            StatusVfxProfileSO profile = Track(ScriptableObject.CreateInstance<StatusVfxProfileSO>());
            var rows = new (StatusDefinitionSO Status, GameObject Aura)[]
            {
                (_burn, _burnAura), (_fear, _fearAura), (_powerA, _powerAura), (_powerB, _powerAura), (_focus, null),
            };

            using var serialized = new SerializedObject(profile);
            SerializedProperty entries = serialized.FindProperty("_entries");
            entries.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_status").objectReferenceValue = rows[i].Status;
                entry.FindPropertyRelative("_aura._prefab").objectReferenceValue = rows[i].Aura;
            }

            serialized.FindProperty("_maxAurasPerFighter").intValue = maxAuras;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static StatusSet Holding(params StatusDefinitionSO[] statuses)
        {
            var set = new StatusSet();
            for (int i = 0; i < statuses.Length; i++)
            {
                set.Apply(statuses[i], 1, 1);
            }

            return set;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
