// How one source's blows look. Impact is the burst where a hit lands, Slash the blade swept across the target (only
// for hits with an attacker), Sparks the streaks thrown out of the far side, and Cast the mandala that blooms under
// the one who acts (played at Rama's feet for his cards and arrows). Accents are particle prefabs (such as Polygon
// Arsenal effects) layered over those: Accent plays on every hit of this source that takes health, the tier accent on
// heavy hits (Heavy Accent) and killing blows (Kill Accent, with Heavy Accent standing in when it is empty), and Death
// where an enemy whose own blows use this profile falls. Arrow profiles add three more: Launch Accent plays on Rama
// when the arrow is loosed, Missile flies from Rama to each target over the arrow's flight (to the chosen enemy, or to
// every living enemy for an untargeted arrow; an arrow without a missile, such as one that only affects Rama, does not
// fly), and Arrival plays where each missile lands once the arrow resolves, whether or not it dealt damage (a status
// arrow still lands). The missile's height, camera-surface and offset settings place its landing point on the target.
// One shared flyweight per hit source (a card type, an arrow,
// an enemy, round-end burns), chosen through HitVfxLibrarySO; the style's default profile covers anything unmapped.
// Read-only at runtime. Values are read on every spawn, so Play mode edits show on the next hit; Changed (Editor
// only) lets long-lived views re-apply.
using System;
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat VFX/Hit VFX Profile", fileName = "HitVfxProfile")]
    public sealed class HitVfxProfileSO : ScriptableObject
    {
        [SerializeField] private ImpactBurstLook _impact = new ImpactBurstLook();
        [SerializeField] private SlashArcLook _slash = new SlashArcLook();
        [SerializeField] private SparkBurstLook _sparks = new SparkBurstLook();
        [SerializeField] private MandalaRingLook _cast = new MandalaRingLook();

        [Header("Accents")]
        [Tooltip("Played on every hit of this source that takes health, such as an arrow's elemental explosion.")]
        [SerializeField] private PrefabVfxLook _accent = new PrefabVfxLook();
        [Tooltip("Played on heavy hits, and on killing blows when Kill Accent is empty.")]
        [SerializeField] private PrefabVfxLook _heavyAccent = new PrefabVfxLook();
        [Tooltip("Played on killing blows.")]
        [SerializeField] private PrefabVfxLook _killAccent = new PrefabVfxLook();
        [Tooltip("Played where an enemy whose own blows use this profile falls.")]
        [SerializeField] private PrefabVfxLook _death = new PrefabVfxLook();

        [Header("Arrow Flight")]
        [Tooltip("Played on Rama when this arrow is loosed.")]
        [SerializeField] private PrefabVfxLook _launchAccent = new PrefabVfxLook();
        [Tooltip("Flies from Rama to each target during the arrow's flight. Its height, camera surface and offset place the landing point. Empty: the arrow does not fly.")]
        [SerializeField] private PrefabVfxLook _missile = new PrefabVfxLook();
        [Tooltip("Played where each missile lands when the arrow resolves, with or without damage.")]
        [SerializeField] private PrefabVfxLook _arrival = new PrefabVfxLook();

        public event Action Changed;

        public ImpactBurstLook Impact => _impact;

        public SlashArcLook Slash => _slash;

        public SparkBurstLook Sparks => _sparks;

        public MandalaRingLook Cast => _cast;

        public PrefabVfxLook Accent => _accent;

        public PrefabVfxLook HeavyAccent => _heavyAccent;

        public PrefabVfxLook KillAccent => _killAccent;

        public PrefabVfxLook Death => _death;

        public PrefabVfxLook LaunchAccent => _launchAccent;

        public PrefabVfxLook Missile => _missile;

        public PrefabVfxLook Arrival => _arrival;

        // The accent for how hard a hit landed: Kill Accent on a killing blow, Heavy Accent on a heavy hit or a
        // killing blow without a Kill Accent, otherwise none.
        public PrefabVfxLook TierAccent(bool heavy, bool killed)
        {
            if (killed && _killAccent != null && _killAccent.IsEnabled)
            {
                return _killAccent;
            }

            if ((heavy || killed) && _heavyAccent != null && _heavyAccent.IsEnabled)
            {
                return _heavyAccent;
            }

            return null;
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
