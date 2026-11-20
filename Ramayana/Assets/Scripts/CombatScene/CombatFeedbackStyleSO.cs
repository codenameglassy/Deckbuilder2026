// Visual values for combat feedback that is not tied to one fighter: floating damage numbers (font, colours for lost
// health, absorbed Kavach and healing, rise, duration, pop), the floating status popups ("Bhaya 2", drawn in the
// status's colour, their size and where they sit relative to the numbers), the special-arrow name popup over Rama when
// he fires one (drawn in the arrow's colour, its size and offset) and enemy intents (the layout strategy, the outlined
// number material, the gap above the health bar or a fixed offset, size on screen through the
// combat camera anchor, colours, optional icons, the ladder of attack icons by total damage, the plate behind each row,
// the pop when an intent changes, and the fallback disc a status the move applies is drawn with, in the status's
// colour, until the status has an icon). Numbers and icons only, so nothing here needs localization; status and arrow
// popups take their names from their localization keys. Read-only at runtime; in the Editor, Changed lets intent views
// re-apply it live.
using System;
using TMPro;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [CreateAssetMenu(menuName = "Journey to Lanka/Combat/Combat Feedback Style", fileName = "CombatFeedbackStyle")]
    public sealed class CombatFeedbackStyleSO : ScriptableObject
    {
        [Header("Damage Numbers")]
        [SerializeField] private TMP_FontAsset _numberFont;
        [SerializeField, Min(0.1f)] private float _numberFontSize = 7f;
        [SerializeField] private Color _healthLossColor = new Color(1f, 0.3f, 0.25f);
        [SerializeField] private Color _blockedColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Color _healColor = new Color(0.45f, 0.95f, 0.55f);
        [SerializeField] private Vector3 _blockedSideOffset = new Vector3(0.6f, -0.2f, 0f);
        [SerializeField, Min(0f)] private float _numberRise = 0.9f;
        [SerializeField, Min(0.05f)] private float _numberDuration = 0.8f;
        [SerializeField, Min(1f)] private float _numberPopScale = 1.35f;

        [Header("Status Popups")]
        [SerializeField, Min(0.1f)] private float _statusPopupFontSize = 4.5f;
        [SerializeField] private Vector3 _statusPopupOffset = new Vector3(0f, 0.45f, 0f);

        [Header("Arrow Popups")]
        [SerializeField, Min(0.1f)] private float _arrowPopupFontSize = 6f;
        [SerializeField] private Vector3 _arrowPopupOffset = new Vector3(0f, 1.1f, 0f);

        [Header("Enemy Intent")]
        [SerializeField] private TMP_FontAsset _intentFont;
        [SerializeField, Min(0.1f)] private float _intentFontSize = 5f;
        [SerializeField] private Vector3 _intentOffset = new Vector3(0f, 2f, 0f);
        [Tooltip("How each intent row arranges its icon, number and plate.")]
        [SerializeField] private IntentLayoutSO _intentLayout;
        [Tooltip("Outlined material for the intent numbers, made from the intent font. Empty uses the font's own.")]
        [SerializeField] private Material _intentNumberMaterial;
        [Tooltip("Reference pixels from the health bar's top to the lowest intent row. Negative: use Intent Offset.")]
        [SerializeField] private float _intentBarGap = 14f;
        [Tooltip("Reference pixels (at 1080p) per intent unit, kept the same at any distance. Zero keeps world size.")]
        [SerializeField, Min(0f)] private float _intentScreenScale = 90f;
        [Tooltip("The combat camera, used to keep intents the same size on screen.")]
        [SerializeField] private CombatCameraAnchorSO _cameraAnchor;
        [SerializeField, Min(0f)] private float _intentRowSpacing = 0.6f;
        [SerializeField] private Vector2 _intentLabelSize = new Vector2(3f, 0.8f);
        [SerializeField] private Color _attackColor = new Color(1f, 0.45f, 0.35f);
        [SerializeField] private Color _defendColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Sprite _attackIcon;
        [SerializeField] private Sprite _defendIcon;
        [SerializeField, Min(0.05f)] private float _intentIconSize = 0.5f;
        [SerializeField] private Sprite _statusIntentSprite;
        [Tooltip("Attack icons by total damage, lowest first. Attacks below the first tier use Attack Icon.")]
        [SerializeField] private IntentAttackTier[] _attackTiers = Array.Empty<IntentAttackTier>();
        [SerializeField, Min(0f)] private float _intentIconGap = 0.08f;
        [Tooltip("Nine-sliced plate behind each intent row. Leave empty for no plate.")]
        [SerializeField] private Sprite _intentPlateSprite;
        [SerializeField] private Color _intentPlateColor = Color.white;
        [SerializeField] private Vector2 _intentPlatePadding = new Vector2(0.14f, 0.08f);
        [Tooltip("How much the intent swells when the enemy picks a new move.")]
        [SerializeField, Min(0f)] private float _intentPopScale = 0.3f;
        [SerializeField, Min(0.01f)] private float _intentPopDuration = 0.35f;

        public event Action Changed;

        public TMP_FontAsset NumberFont => _numberFont;
        public float NumberFontSize => _numberFontSize;
        public Color HealthLossColor => _healthLossColor;
        public Color BlockedColor => _blockedColor;
        public Color HealColor => _healColor;
        public Vector3 BlockedSideOffset => _blockedSideOffset;
        public float NumberRise => _numberRise;
        public float NumberDuration => _numberDuration;
        public float NumberPopScale => _numberPopScale;

        public float StatusPopupFontSize => _statusPopupFontSize;
        public Vector3 StatusPopupOffset => _statusPopupOffset;

        public float ArrowPopupFontSize => _arrowPopupFontSize;
        public Vector3 ArrowPopupOffset => _arrowPopupOffset;

        public TMP_FontAsset IntentFont => _intentFont;
        public float IntentFontSize => _intentFontSize;
        public Vector3 IntentOffset => _intentOffset;
        public float IntentScreenScale => _intentScreenScale;
        public IntentLayoutSO IntentLayout => _intentLayout;
        public Material IntentNumberMaterial => _intentNumberMaterial;
        public float IntentBarGap => _intentBarGap;
        public CombatCameraAnchorSO CameraAnchor => _cameraAnchor;
        public float IntentRowSpacing => _intentRowSpacing;
        public Vector2 IntentLabelSize => _intentLabelSize;
        public Color AttackColor => _attackColor;
        public Color DefendColor => _defendColor;
        public Sprite AttackIcon => _attackIcon;
        public Sprite DefendIcon => _defendIcon;
        public float IntentIconSize => _intentIconSize;
        public Sprite StatusIntentSprite => _statusIntentSprite;
        public float IntentIconGap => _intentIconGap;
        public Sprite IntentPlateSprite => _intentPlateSprite;
        public Color IntentPlateColor => _intentPlateColor;
        public Vector2 IntentPlatePadding => _intentPlatePadding;
        public float IntentPopScale => _intentPopScale;
        public float IntentPopDuration => _intentPopDuration;

        public Sprite AttackIconFor(int totalDamage, out float scale)
        {
            Sprite icon = _attackIcon;
            scale = 1f;
            if (_attackTiers == null)
            {
                return icon;
            }

            for (int i = 0; i < _attackTiers.Length; i++)
            {
                IntentAttackTier tier = _attackTiers[i];
                if (tier.Icon != null && totalDamage >= tier.MinDamage)
                {
                    icon = tier.Icon;
                    scale = tier.Scale;
                }
            }

            return icon;
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
