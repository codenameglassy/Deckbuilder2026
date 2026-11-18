// Visual values for combat feedback that is not tied to one fighter: floating damage numbers (font, colours for lost
// health, absorbed Kavach and healing, rise, duration, pop), the floating status popups ("Bhaya 2", drawn in the
// status's colour, their size and where they sit relative to the numbers), the special-arrow name popup over Rama when
// he fires one (drawn in the arrow's colour, its size and offset) and enemy intents (offset, colours, optional icons, and the fallback disc a status the move
// applies is drawn with, in the status's colour, until the status has an icon). Numbers and icons only, so nothing
// here needs localization; status and arrow popups take their names from their localization keys. Read-only at runtime; in the Editor, Changed lets intent views re-apply it live.
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
        [SerializeField, Min(0f)] private float _intentRowSpacing = 0.6f;
        [SerializeField] private Vector2 _intentLabelSize = new Vector2(3f, 0.8f);
        [SerializeField] private Color _attackColor = new Color(1f, 0.45f, 0.35f);
        [SerializeField] private Color _defendColor = new Color(0.45f, 0.75f, 1f);
        [SerializeField] private Sprite _attackIcon;
        [SerializeField] private Sprite _defendIcon;
        [SerializeField, Min(0.05f)] private float _intentIconSize = 0.5f;
        [SerializeField] private Sprite _statusIntentSprite;

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
        public float IntentRowSpacing => _intentRowSpacing;
        public Vector2 IntentLabelSize => _intentLabelSize;
        public Color AttackColor => _attackColor;
        public Color DefendColor => _defendColor;
        public Sprite AttackIcon => _attackIcon;
        public Sprite DefendIcon => _defendIcon;
        public float IntentIconSize => _intentIconSize;
        public Sprite StatusIntentSprite => _statusIntentSprite;

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
