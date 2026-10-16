// Spawns pooled floating damage numbers and animates every active one from a single tick. Combatant views call
// Spawn when their fighter loses health (red) or Kavach absorbs damage (blue, offset to the side). The template is
// built in code and the pool is prewarmed at start, so a fight never instantiates numbers mid-play.
// Without a tick service (Play mode not started from Boot) it spawns nothing.
using System.Collections.Generic;
using JourneyToLanka.Core.Pooling;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Journey to Lanka/Combat/Damage Number Spawner")]
    public sealed class DamageNumberSpawner : MonoBehaviour, ITickable
    {
        [SerializeField] private CombatFeedbackStyleSO _style;
        [SerializeField, Min(0)] private int _prewarmCount = 8;

        private readonly List<DamageNumber> _active = new List<DamageNumber>(16);
        private ComponentPool<DamageNumber> _pool;
        private ITickService _ticks;
        private bool _registered;

        private void Awake()
        {
            var holder = new GameObject("Templates");
            holder.transform.SetParent(transform, false);
            holder.SetActive(false);

            var templateObject = new GameObject("DamageNumber");
            templateObject.transform.SetParent(holder.transform, false);
            var label = templateObject.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(3f, 1.5f);
            var template = templateObject.AddComponent<DamageNumber>();
            template.Initialize(label);

            _pool = new ComponentPool<DamageNumber>(template, transform, 8, 32);
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _ticks);
            _pool.Prewarm(_prewarmCount);
        }

        private void OnDisable()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _pool.Release(_active[i]);
            }

            _active.Clear();
            SetRegistered(false);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }

        public void Spawn(Vector3 worldPosition, int value, bool blocked)
        {
            if (_style == null || _ticks == null || value <= 0 || !isActiveAndEnabled)
            {
                return;
            }

            DamageNumber number = _pool.Get();
            Vector3 position = blocked ? worldPosition + _style.BlockedSideOffset : worldPosition;
            number.Play(position, value, blocked ? _style.BlockedColor : _style.HealthLossColor, _style);
            _active.Add(number);
            SetRegistered(true);
        }

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                DamageNumber number = _active[i];
                if (number.Advance(deltaTime))
                {
                    _active.RemoveAt(i);
                    _pool.Release(number);
                }
            }

            if (_active.Count == 0)
            {
                SetRegistered(false);
            }
        }

        private void SetRegistered(bool registered)
        {
            if (_ticks == null || _registered == registered)
            {
                return;
            }

            _registered = registered;
            if (registered)
            {
                _ticks.AddTickable(this);
            }
            else
            {
                _ticks.RemoveTickable(this);
            }
        }
    }
}
