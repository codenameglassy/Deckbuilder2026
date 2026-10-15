// Shared definition of one enemy: stable id, localization key, health range rolled at combat start,
// and a repeating move pattern with an optional starting offset (the reference build's Gate-Warden starts at 2).
using System;
using UnityEngine;

namespace JourneyToLanka.Combat.Enemies
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Enemy", fileName = "Enemy")]
    public sealed class EnemyDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [SerializeField, Min(1)] private int _minHealth = 30;
        [SerializeField, Min(1)] private int _maxHealth = 34;
        [SerializeField] private EnemyMoveSO[] _moves = Array.Empty<EnemyMoveSO>();
        [SerializeField, Min(0)] private int _startMoveIndex;

        public string Id => _id;

        public string NameKey => _nameKey;

        public int MinHealth => _minHealth;

        public int MaxHealth => _maxHealth;

        public int MoveCount => _moves != null ? _moves.Length : 0;

        public int StartMoveIndex => _startMoveIndex;

        public EnemyMoveSO GetMove(int patternIndex)
        {
            int count = MoveCount;
            if (count == 0)
            {
                return null;
            }

            int index = patternIndex % count;
            if (index < 0)
            {
                index += count;
            }

            return _moves[index];
        }

        private void OnValidate()
        {
            _maxHealth = Mathf.Max(_maxHealth, _minHealth);
            _startMoveIndex = Mathf.Clamp(_startMoveIndex, 0, Mathf.Max(0, MoveCount - 1));
        }
    }
}
