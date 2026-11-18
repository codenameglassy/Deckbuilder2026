// Who or what dealt a hit, as presentation sees it: the key asset it came from (the played card's CardTypeSO, the
// fired ArrowDefinitionSO or the acting enemy's EnemyDefinitionSO; none for round-end damage), the attacker's slot,
// so directional effects know where the blow came from, and the hit profile resolved for that source when it was
// announced (null to use the style's default). Slots follow one convention across the combat VFX: PlayerSlot for
// Rama, 0 and up for enemy field slots, NoSlot when nothing struck (a burn). The slot is stored offset so that
// default(HitSource) equals None: no key, no attacker and no profile. Immutable.
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    public readonly struct HitSource
    {
        public const int NoSlot = -2;
        public const int PlayerSlot = -1;

        private readonly int _encodedSlot;

        public HitSource(Object key, int attackerSlot, HitVfxProfileSO profile = null)
        {
            Key = key;
            Profile = profile;
            _encodedSlot = attackerSlot < NoSlot ? 0 : attackerSlot - NoSlot;
        }

        public static HitSource None => default;

        public Object Key { get; }

        public HitVfxProfileSO Profile { get; }

        public int AttackerSlot => _encodedSlot + NoSlot;

        public bool HasAttacker => AttackerSlot != NoSlot;
    }
}
