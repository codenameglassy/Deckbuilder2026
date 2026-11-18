// One row of HitVfxLibrarySO: a source asset (a card type, an enemy or an arrow) and the hit profile its blows use.
// Serializable; read-only at runtime.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [Serializable]
    public struct HitVfxEntry<TKey> where TKey : UnityEngine.Object
    {
        [SerializeField] private TKey _key;
        [SerializeField] private HitVfxProfileSO _profile;

        public TKey Key => _key;

        public HitVfxProfileSO Profile => _profile;
    }
}
