// Every visual value for a combatant view: the world-space health bar (placement, size, colours, the trail that
// drains after a hit, the number on it), the Kavach badge and the Kavach colours the bar switches to while the
// fighter is protected, the tint for a fallen fighter, the hit reaction (flash and shake), the attack motions (an
// enemy's lunge and Rama's thrust), where damage numbers appear, and the outcome preview (the
// ghost over health a card would take, the previewed numbers, the lethal colour and tag, and the pulse), and the row of
// status chips under the bar (the fallback disc drawn in a status's colour until it has an icon, chip size and spacing,
// the short label, the stack count on its small badge, and how faint a ghost chip is while a card is previewed).
// Read-only at runtime. In the Editor, changing a value raises Changed so views in Play mode re-apply it at once
// without rebuilding. Sprites are optional; without one an element draws as a flat colour.
using System;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Combatant View Style", fileName = "CombatantViewStyle")]
    public sealed class CombatantViewStyleSO : ScriptableObject
    {
        [Header("Health Bar")]
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Vector3 _barOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField, Min(0.0001f)] private float _barWorldScale = 0.01f;
        [SerializeField] private Vector2 _barSize = new Vector2(120f, 20f);
        [SerializeField, Min(0f)] private float _barBorder = 2f;
        [SerializeField] private Color _barBorderColor = new Color(0.05f, 0.04f, 0.03f, 0.95f);
        [SerializeField] private Color _barBackgroundColor = new Color(0.18f, 0.06f, 0.05f, 0.95f);
        [SerializeField] private Color _barFillColor = new Color(0.82f, 0.22f, 0.18f);
        [SerializeField] private Color _barTrailColor = new Color(1f, 0.85f, 0.5f);
        [SerializeField] private Color _barTextColor = new Color(0.98f, 0.95f, 0.88f);
        [SerializeField, Min(1f)] private float _barFontSize = 15f;
        [SerializeField, Min(0f)] private float _trailDelay = 0.25f;
        [SerializeField, Min(0.01f)] private float _trailDuration = 0.45f;

        [Header("Kavach")]
        [SerializeField] private Color _kavachFillColor = new Color(0.35f, 0.6f, 0.95f);
        [SerializeField] private Color _kavachBorderColor = new Color(0.6f, 0.85f, 1f);
        [SerializeField] private Vector2 _kavachBadgeSize = new Vector2(30f, 30f);
        [SerializeField, Min(0f)] private float _kavachBadgeGap = 4f;
        [SerializeField] private Sprite _kavachBadgeSprite;
        [SerializeField] private Color _kavachBadgeColor = new Color(0.3f, 0.55f, 0.9f);
        [SerializeField] private Color _kavachTextColor = new Color(0.98f, 0.95f, 0.88f);
        [SerializeField, Min(1f)] private float _kavachFontSize = 17f;
        [SerializeField, Min(0f)] private float _kavachPunchScale = 0.35f;
        [SerializeField, Min(0.01f)] private float _kavachPunchDuration = 0.25f;

        [Header("Fallen")]
        [SerializeField] private Color _fallenTint = new Color(0.25f, 0.25f, 0.25f);

        [Header("Hit Reaction")]
        [SerializeField] private Color _flashColor = new Color(1f, 0.35f, 0.3f);
        [SerializeField, Min(0f)] private float _flashDuration = 0.18f;
        [SerializeField, Min(0f)] private float _shakeDuration = 0.25f;
        [SerializeField, Min(0f)] private float _shakeStrength = 0.12f;
        [SerializeField, Min(0f)] private float _shakeFrequency = 40f;

        [Header("Attack Motion")]
        [SerializeField, Min(0f)] private float _lungeDistance = 0.9f;
        [SerializeField, Min(0f)] private float _lungeWindBack = 0.25f;
        [SerializeField, Range(0f, 0.9f)] private float _lungeWindBackShare = 0.45f;
        [SerializeField, Min(0.01f)] private float _lungeReturnDuration = 0.25f;
        [SerializeField, Min(0f)] private float _thrustDistance = 0.45f;
        [SerializeField, Min(0.01f)] private float _thrustDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float _thrustReturnDuration = 0.18f;

        [Header("Damage Numbers")]
        [SerializeField] private Vector3 _damageNumberOffset = new Vector3(0f, 0.6f, -0.6f);

        [Header("Outcome Preview")]
        [SerializeField] private Color _previewLossColor = new Color(1f, 0.95f, 0.75f, 0.95f);
        [SerializeField, Range(0f, 1f)] private float _previewLossDimAlpha = 0.35f;
        [SerializeField] private Color _previewTextColor = new Color(1f, 0.88f, 0.5f);
        [SerializeField] private Color _previewKavachTextColor = new Color(0.8f, 0.93f, 1f);
        [SerializeField] private Color _lethalColor = new Color(1f, 0.2f, 0.14f);
        [SerializeField, Min(1f)] private float _lethalTagFontSize = 16f;
        [SerializeField] private Vector2 _lethalTagOffset = new Vector2(0f, 20f);
        [SerializeField, Min(0.05f)] private float _previewPulseDuration = 0.45f;

        [Header("Status Chips")]
        [SerializeField] private Sprite _statusChipSprite;
        [SerializeField] private Vector2 _statusChipSize = new Vector2(26f, 26f);
        [SerializeField, Min(0f)] private float _statusChipSpacing = 4f;
        [SerializeField] private Vector2 _statusRowOffset = new Vector2(0f, -6f);
        [SerializeField] private Color _statusLabelColor = new Color(0.1f, 0.07f, 0.05f);
        [SerializeField, Min(1f)] private float _statusLabelFontSize = 9f;
        [SerializeField] private Vector2 _statusCountBadgeSize = new Vector2(16f, 16f);
        [SerializeField] private Color _statusCountBadgeColor = new Color(0.08f, 0.06f, 0.05f, 0.92f);
        [SerializeField] private Color _statusCountColor = new Color(0.98f, 0.95f, 0.88f);
        [SerializeField, Min(1f)] private float _statusCountFontSize = 12f;
        [SerializeField, Range(0f, 1f)] private float _statusGhostAlpha = 0.5f;

        public event Action Changed;

        public TMP_FontAsset Font => _font;
        public Vector3 BarOffset => _barOffset;
        public float BarWorldScale => _barWorldScale;
        public Vector2 BarSize => _barSize;
        public float BarBorder => _barBorder;
        public Color BarBorderColor => _barBorderColor;
        public Color BarBackgroundColor => _barBackgroundColor;
        public Color BarFillColor => _barFillColor;
        public Color BarTrailColor => _barTrailColor;
        public Color BarTextColor => _barTextColor;
        public float BarFontSize => _barFontSize;
        public float TrailDelay => _trailDelay;
        public float TrailDuration => _trailDuration;

        public Color KavachFillColor => _kavachFillColor;
        public Color KavachBorderColor => _kavachBorderColor;
        public Vector2 KavachBadgeSize => _kavachBadgeSize;
        public float KavachBadgeGap => _kavachBadgeGap;
        public Sprite KavachBadgeSprite => _kavachBadgeSprite;
        public Color KavachBadgeColor => _kavachBadgeColor;
        public Color KavachTextColor => _kavachTextColor;
        public float KavachFontSize => _kavachFontSize;
        public float KavachPunchScale => _kavachPunchScale;
        public float KavachPunchDuration => _kavachPunchDuration;

        public Color FallenTint => _fallenTint;

        public Color FlashColor => _flashColor;
        public float FlashDuration => _flashDuration;
        public float ShakeDuration => _shakeDuration;
        public float ShakeStrength => _shakeStrength;
        public float ShakeFrequency => _shakeFrequency;

        public float LungeDistance => _lungeDistance;
        public float LungeWindBack => _lungeWindBack;
        public float LungeWindBackShare => _lungeWindBackShare;
        public float LungeReturnDuration => _lungeReturnDuration;
        public float ThrustDistance => _thrustDistance;
        public float ThrustDuration => _thrustDuration;
        public float ThrustReturnDuration => _thrustReturnDuration;

        public Vector3 DamageNumberOffset => _damageNumberOffset;

        public Color PreviewLossColor => _previewLossColor;
        public float PreviewLossDimAlpha => _previewLossDimAlpha;
        public Color PreviewTextColor => _previewTextColor;
        public Color PreviewKavachTextColor => _previewKavachTextColor;
        public Color LethalColor => _lethalColor;
        public float LethalTagFontSize => _lethalTagFontSize;
        public Vector2 LethalTagOffset => _lethalTagOffset;
        public float PreviewPulseDuration => _previewPulseDuration;

        public Sprite StatusChipSprite => _statusChipSprite;
        public Vector2 StatusChipSize => _statusChipSize;
        public float StatusChipSpacing => _statusChipSpacing;
        public Vector2 StatusRowOffset => _statusRowOffset;
        public Color StatusLabelColor => _statusLabelColor;
        public float StatusLabelFontSize => _statusLabelFontSize;
        public Vector2 StatusCountBadgeSize => _statusCountBadgeSize;
        public Color StatusCountBadgeColor => _statusCountBadgeColor;
        public Color StatusCountColor => _statusCountColor;
        public float StatusCountFontSize => _statusCountFontSize;
        public float StatusGhostAlpha => _statusGhostAlpha;

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
