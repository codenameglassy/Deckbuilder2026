// EditMode tests for accent selection on HitVfxProfileSO and accent sizing on PrefabVfxLook: a killing blow plays the
// Kill Accent, with the Heavy Accent standing in when it is empty; a heavy hit plays the Heavy Accent; an ordinary
// hit, or a heavy hit with no Heavy Accent, plays no tier accent; an accent without a prefab is disabled; and an
// accent grows with the hit only when Scale With Hit is on.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace JourneyToLanka.CombatVfx.Tests
{
    public sealed class HitVfxAccentTests
    {
        private readonly List<Object> _created = new List<Object>();
        private GameObject _heavyPrefab;
        private GameObject _killPrefab;

        [SetUp]
        public void SetUp()
        {
            _heavyPrefab = Track(new GameObject("Heavy"));
            _killPrefab = Track(new GameObject("Kill"));
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
        public void Killing_Blow_Plays_Kill_Accent()
        {
            HitVfxProfileSO profile = CreateProfile(_heavyPrefab, _killPrefab);

            Assert.AreSame(profile.KillAccent, profile.TierAccent(true, true));
        }

        [Test]
        public void Killing_Blow_Without_Kill_Accent_Plays_Heavy_Accent()
        {
            HitVfxProfileSO profile = CreateProfile(_heavyPrefab, null);

            Assert.AreSame(profile.HeavyAccent, profile.TierAccent(false, true));
        }

        [Test]
        public void Heavy_Hit_Plays_Heavy_Accent()
        {
            HitVfxProfileSO profile = CreateProfile(_heavyPrefab, _killPrefab);

            Assert.AreSame(profile.HeavyAccent, profile.TierAccent(true, false));
        }

        [Test]
        public void Ordinary_Hit_Plays_No_Tier_Accent()
        {
            HitVfxProfileSO profile = CreateProfile(_heavyPrefab, _killPrefab);

            Assert.IsNull(profile.TierAccent(false, false));
        }

        [Test]
        public void Heavy_Hit_Without_Heavy_Accent_Plays_None()
        {
            HitVfxProfileSO profile = CreateProfile(null, _killPrefab);

            Assert.IsNull(profile.TierAccent(true, false));
        }

        [Test]
        public void Accent_Without_Prefab_Is_Disabled()
        {
            HitVfxProfileSO profile = CreateProfile(null, null);

            Assert.IsFalse(profile.Accent.IsEnabled);
            Assert.IsFalse(profile.Death.IsEnabled);
        }

        [Test]
        public void Accent_Grows_With_Hit_Only_When_Asked()
        {
            HitVfxProfileSO profile = CreateProfile(_heavyPrefab, null);
            var heavyScale = new VfxScale(1.5f, 1f, 1f);

            Assert.AreEqual(2f, profile.HeavyAccent.ScaleFor(heavyScale), 1e-5f);

            using (var serialized = new SerializedObject(profile))
            {
                serialized.FindProperty("_heavyAccent._scaleWithHit").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            Assert.AreEqual(3f, profile.HeavyAccent.ScaleFor(heavyScale), 1e-5f);
        }

        private HitVfxProfileSO CreateProfile(GameObject heavy, GameObject kill)
        {
            HitVfxProfileSO profile = Track(ScriptableObject.CreateInstance<HitVfxProfileSO>());
            using var serialized = new SerializedObject(profile);
            serialized.FindProperty("_heavyAccent._prefab").objectReferenceValue = heavy;
            serialized.FindProperty("_heavyAccent._scale").floatValue = 2f;
            serialized.FindProperty("_killAccent._prefab").objectReferenceValue = kill;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
