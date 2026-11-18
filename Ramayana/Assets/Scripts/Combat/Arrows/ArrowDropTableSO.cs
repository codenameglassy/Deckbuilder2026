// How special arrows drop after a won fight, as data: the chance a fight drops one (starting at 40% in the reference
// build, falling by a step when one drops and rising by the same step when none does, kept between a floor and a
// ceiling), the weight of each tier (62 common, 30 uncommon, 8 rare), and the arrows that can drop. NextChance and
// Roll are pure, so a run's seeded random makes every drop replayable; Roll picks a tier by weight and then an arrow
// of that tier, falling back to any arrow when the rolled tier has none. Allocates nothing. Shared and immutable.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Model;
using UnityEngine;

namespace JourneyToLanka.Combat.Arrows
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Arrow Drop Table", fileName = "ArrowDropTable")]
    public sealed class ArrowDropTableSO : ScriptableObject
    {
        [SerializeField, Range(0, 100)] private int _startChance = 40;
        [SerializeField, Range(0, 100)] private int _chanceStep = 10;
        [SerializeField, Range(0, 100)] private int _minChance = 10;
        [SerializeField, Range(0, 100)] private int _maxChance = 90;
        [SerializeField, Min(0)] private int _commonWeight = 62;
        [SerializeField, Min(0)] private int _uncommonWeight = 30;
        [SerializeField, Min(0)] private int _rareWeight = 8;
        [SerializeField] private ArrowDefinitionSO[] _arrows = Array.Empty<ArrowDefinitionSO>();

        public int StartChance => _startChance;

        public IReadOnlyList<ArrowDefinitionSO> Arrows => _arrows;

        public int NextChance(int chance, bool dropped)
        {
            int next = dropped ? chance - _chanceStep : chance + _chanceStep;
            return Math.Clamp(next, Math.Min(_minChance, _maxChance), Math.Max(_minChance, _maxChance));
        }

        public ArrowDefinitionSO Roll(CombatRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            int total = _commonWeight + _uncommonWeight + _rareWeight;
            ArrowTier tier = ArrowTier.Common;
            if (total > 0)
            {
                int roll = random.Range(0, total - 1);
                tier = roll < _commonWeight ? ArrowTier.Common
                    : roll < _commonWeight + _uncommonWeight ? ArrowTier.Uncommon
                    : ArrowTier.Rare;
            }

            ArrowDefinitionSO arrow = Pick(random, tier, true);
            return arrow != null ? arrow : Pick(random, tier, false);
        }

        private ArrowDefinitionSO Pick(CombatRandom random, ArrowTier tier, bool matchTier)
        {
            int count = 0;
            for (int i = 0; i < _arrows.Length; i++)
            {
                if (_arrows[i] != null && (!matchTier || _arrows[i].Tier == tier))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return null;
            }

            int chosen = random.Range(0, count - 1);
            for (int i = 0; i < _arrows.Length; i++)
            {
                if (_arrows[i] != null && (!matchTier || _arrows[i].Tier == tier))
                {
                    if (chosen == 0)
                    {
                        return _arrows[i];
                    }

                    chosen--;
                }
            }

            return null;
        }
    }
}
