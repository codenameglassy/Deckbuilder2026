// Spawns pooled floating damage numbers and animates every active one from a single tick. Combatant views call
// Spawn when their fighter loses health (red) or Kavach absorbs damage (blue, offset to the side), SpawnHeal when it
// is healed (green), and SpawnStatus when a status is applied ("Bhaya 2", the new total, in the status's colour) or
// spent ("Dharma -3"), a little above the numbers, and SpawnArrow when Rama fires a special arrow (its name, larger,
// in the arrow's colour, higher still). Status and arrow names come from their localization keys through one cached
// LocalizedString per asset (LocalizedNameCache), and popup text is built in one reused StringBuilder, so a popup
// allocates nothing once each status or arrow has been named. The template is built in code and the pool is
// prewarmed at start, so a fight never instantiates numbers mid-play.
// Without a tick service (Play mode not started from Boot) it spawns nothing.
using System.Collections.Generic;
using System.Text;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Statuses;
using JourneyToLanka.Core.Pooling;
using JourneyToLanka.Core.Services;
using JourneyToLanka.Core.Ticking;
using JourneyToLanka.Localization;
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
        private readonly LocalizedNameCache _names = new LocalizedNameCache(LocalizationTableNames.Combat);
        private readonly StringBuilder _text = new StringBuilder(32);
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
            _names.Dispose();
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

        public void SpawnHeal(Vector3 worldPosition, int value)
        {
            if (_style == null || _ticks == null || value <= 0 || !isActiveAndEnabled)
            {
                return;
            }

            DamageNumber number = _pool.Get();
            number.Play(worldPosition, value, _style.HealColor, _style);
            _active.Add(number);
            SetRegistered(true);
        }

        public void SpawnStatus(Vector3 worldPosition, StatusDefinitionSO status, int amount, int total)
        {
            if (_style == null || _ticks == null || status == null || amount == 0 || !isActiveAndEnabled)
            {
                return;
            }

            _text.Clear();
            _text.Append(_names.Get(status, status.NameKey, status.Id));
            _text.Append(' ');
            if (amount < 0)
            {
                _text.Append('-');
                AppendNumber(_text, -amount);
            }
            else
            {
                AppendNumber(_text, total);
            }

            DamageNumber number = _pool.Get();
            number.PlayText(worldPosition + _style.StatusPopupOffset, _text, status.Color, _style.StatusPopupFontSize, _style);
            _active.Add(number);
            SetRegistered(true);
        }

        public void SpawnArrow(Vector3 worldPosition, ArrowDefinitionSO arrow)
        {
            if (_style == null || _ticks == null || arrow == null || !isActiveAndEnabled)
            {
                return;
            }

            _text.Clear();
            _text.Append(_names.Get(arrow, arrow.NameKey, arrow.Id));

            DamageNumber number = _pool.Get();
            number.PlayText(worldPosition + _style.ArrowPopupOffset, _text, arrow.Color, _style.ArrowPopupFontSize, _style);
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

        private static void AppendNumber(StringBuilder text, int value)
        {
            if (value <= 0)
            {
                text.Append('0');
                return;
            }

            int start = text.Length;
            while (value > 0)
            {
                text.Insert(start, (char)('0' + value % 10));
                value /= 10;
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
