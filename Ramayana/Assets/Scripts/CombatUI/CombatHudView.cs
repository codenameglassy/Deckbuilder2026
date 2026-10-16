// The combat HUD: the hand of pooled cards, Prana, draw and discard counts, the End Turn button and the result
// banner. It builds its hierarchy in code under two child canvases split by update rate (the hand changes on every
// play; the HUD panels change less), applies every visual value from its style, and re-applies the style live
// in the Editor. It only presents and forwards input to CombatFlow; it holds no combat rules.
// It finds the fight through the CombatController anchor when the CombatStarted channel fires.
// End Turn also responds to E on the keyboard and the north face button on a gamepad.
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.CombatScene;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [AddComponentMenu("Journey to Lanka/UI/Combat HUD")]
    public sealed class CombatHudView : MonoBehaviour
    {
        [SerializeField] private CombatHudStyleSO _style;
        [SerializeField] private CombatControllerAnchorSO _controllerAnchor;
        [SerializeField] private VoidEventChannelSO _combatStarted;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _handArea;
        private HorizontalLayoutGroup _handLayout;
        private StatPanel _prana;
        private StatPanel _drawPile;
        private StatPanel _discardPile;
        private Button _endTurn;
        private Image _endTurnImage;
        private TextMeshProUGUI _endTurnLabel;
        private LocalizedText _endTurnText;
        private Image _banner;
        private TextMeshProUGUI _bannerLabel;
        private LocalizedText _bannerText;
        private HandPresenter _hand;
        private InputAction _endTurnAction;
        private CombatFlow _flow;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _scaler = GetComponent<CanvasScaler>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Build();

            _endTurn.onClick.AddListener(HandleEndTurnPressed);
            _endTurnAction = new InputAction("EndTurn", InputActionType.Button, "<Keyboard>/e");
            _endTurnAction.AddBinding("<Gamepad>/buttonNorth");
            _endTurnAction.performed += _ => HandleEndTurnPressed();

            if (_style == null)
            {
                Debug.LogError($"{name}: no Combat HUD Style assigned. Run Tools > Journey to Lanka > Wire Selected > Combat UI.", this);
            }

            ApplyStyle();
            RefreshAll();
        }

        private void OnEnable()
        {
            if (_style != null)
            {
                _style.Changed += HandleStyleChanged;
            }

            if (_combatStarted != null)
            {
                _combatStarted.Register(HandleCombatStarted);
            }

            _endTurnAction.Enable();
            TryAttachToRunningCombat();
        }

        private void OnDisable()
        {
            if (_style != null)
            {
                _style.Changed -= HandleStyleChanged;
            }

            if (_combatStarted != null)
            {
                _combatStarted.Unregister(HandleCombatStarted);
            }

            _endTurnAction.Disable();
            Detach();
        }

        private void OnDestroy()
        {
            if (_endTurn != null)
            {
                _endTurn.onClick.RemoveListener(HandleEndTurnPressed);
            }

            _endTurnAction?.Dispose();
            _hand?.Dispose();
            _endTurnText?.Clear();
            _bannerText?.Clear();
            _prana?.Dispose();
            _drawPile?.Dispose();
            _discardPile?.Dispose();
        }

        private void Build()
        {
            RectTransform handCanvas = CreateChildCanvas("HandCanvas");
            _handArea = UIBuildUtility.CreateRect("Hand", handCanvas);
            _handLayout = _handArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            _handLayout.childAlignment = TextAnchor.LowerCenter;
            _handLayout.childControlWidth = false;
            _handLayout.childControlHeight = false;
            _handLayout.childForceExpandWidth = false;
            _handLayout.childForceExpandHeight = false;
            CanvasGroup handGroup = _handArea.gameObject.AddComponent<CanvasGroup>();

            RectTransform hudCanvas = CreateChildCanvas("HudCanvas");
            _prana = StatPanel.Create("Prana", hudCanvas, LocalizationTableNames.UI, CombatUIKeys.Prana);
            _drawPile = StatPanel.Create("DrawPile", hudCanvas, LocalizationTableNames.UI, CombatUIKeys.DrawPile);
            _discardPile = StatPanel.Create("DiscardPile", hudCanvas, LocalizationTableNames.UI, CombatUIKeys.DiscardPile);

            _endTurnImage = UIBuildUtility.CreateImage("EndTurn", hudCanvas, true);
            _endTurn = _endTurnImage.gameObject.AddComponent<Button>();
            _endTurn.targetGraphic = _endTurnImage;
            _endTurn.transition = Selectable.Transition.ColorTint;
            _endTurnLabel = UIBuildUtility.CreateText("Label", _endTurnImage.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(_endTurnLabel.rectTransform);
            _endTurnText = new LocalizedText(_endTurnLabel);

            _banner = UIBuildUtility.CreateImage("ResultBanner", hudCanvas, false);
            _bannerLabel = UIBuildUtility.CreateText("Label", _banner.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(_bannerLabel.rectTransform);
            _bannerText = new LocalizedText(_bannerLabel);

            RectTransform templates = UIBuildUtility.CreateRect("Templates", transform);
            templates.gameObject.SetActive(false);
            CardView template = CardViewFactory.CreateTemplate(templates);

            _hand = new HandPresenter(template, _handArea, _handLayout, handGroup, _endTurn);
        }

        private RectTransform CreateChildCanvas(string canvasName)
        {
            RectTransform rect = UIBuildUtility.CreateRect(canvasName, transform);
            UIBuildUtility.Stretch(rect);
            rect.gameObject.AddComponent<Canvas>();
            rect.gameObject.AddComponent<GraphicRaycaster>();
            return rect;
        }

        private void ApplyStyle()
        {
            if (_style == null)
            {
                return;
            }

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = _style.ReferenceResolution;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            UIBuildUtility.Place(_handArea, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, _style.HandBottomMargin), new Vector2(_style.HandMaxWidth, _style.CardSize.y));

            _prana.ApplyStyle(_style, Vector2.zero, _style.PranaOffset);
            _drawPile.ApplyStyle(_style, Vector2.zero, _style.DrawPileOffset);
            _discardPile.ApplyStyle(_style, new Vector2(1f, 0f), _style.DiscardPileOffset);

            UIBuildUtility.Place(_endTurnImage.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                _style.EndTurnOffset, _style.EndTurnSize);
            UIBuildUtility.StyleImage(_endTurnImage, _style.EndTurnSprite, _style.EndTurnColor);
            _endTurn.colors = CardView.CreateTint(_style);
            UIBuildUtility.StyleText(_endTurnLabel, _style.Font, _style.EndTurnFontSize, _style.EndTurnTextColor);

            UIBuildUtility.Place(_banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, _style.BannerSize);
            UIBuildUtility.StyleImage(_banner, null, _style.BannerColor);
            UIBuildUtility.StyleText(_bannerLabel, _style.Font, _style.BannerFontSize, _style.VictoryTextColor);

            _hand.SetStyle(_style);
        }

        private void HandleStyleChanged()
        {
            ApplyStyle();
            RefreshAll();
        }

        private void HandleCombatStarted()
        {
            TryAttachToRunningCombat();
        }

        private void TryAttachToRunningCombat()
        {
            if (_controllerAnchor == null || !_controllerAnchor.TryGetValue(out CombatController controller))
            {
                return;
            }

            if (controller.Flow != null && controller.Flow != _flow)
            {
                Attach(controller.Flow);
            }
        }

        private void Attach(CombatFlow flow)
        {
            Detach();
            _flow = flow;
            CombatModel model = flow.Model;

            flow.PhaseChanged += HandlePhaseChanged;
            model.Prana.Changed += HandlePranaChanged;
            model.Deck.CardMoved += HandleCardMoved;
            model.Deck.Reshuffled += HandleReshuffled;
            _hand.Attach(flow);

            RefreshAll();
        }

        private void Detach()
        {
            if (_flow == null)
            {
                return;
            }

            CombatModel model = _flow.Model;
            _flow.PhaseChanged -= HandlePhaseChanged;
            model.Prana.Changed -= HandlePranaChanged;
            model.Deck.CardMoved -= HandleCardMoved;
            model.Deck.Reshuffled -= HandleReshuffled;
            _hand.Detach();
            _flow = null;
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            RefreshPhase();
        }

        private void HandlePranaChanged(PranaPool prana)
        {
            RefreshPrana();
            _hand.RefreshPlayable();
        }

        private void HandleCardMoved(CardInstance card, CardPile from, CardPile to)
        {
            RefreshPiles();
        }

        private void HandleReshuffled()
        {
            RefreshPiles();
        }

        private void HandleEndTurnPressed()
        {
            if (_flow != null && _flow.IsPlayerTurn)
            {
                _flow.EndTurn();
            }
        }

        private void RefreshAll()
        {
            RefreshPrana();
            RefreshPiles();
            RefreshPhase();
        }

        private void RefreshPrana()
        {
            if (_flow != null)
            {
                _prana.SetValue(_flow.Model.Prana.Current, _flow.Model.Prana.PerTurn);
            }
        }

        private void RefreshPiles()
        {
            if (_flow != null)
            {
                _drawPile.SetValue(_flow.Model.Deck.DrawPile.Count);
                _discardPile.SetValue(_flow.Model.Deck.DiscardPile.Count);
            }
        }

        private void RefreshPhase()
        {
            CombatPhase phase = _flow != null ? _flow.Phase : CombatPhase.None;
            bool running = phase != CombatPhase.None;
            bool playerTurn = phase == CombatPhase.PlayerTurn;
            bool over = phase == CombatPhase.Victory || phase == CombatPhase.Defeat;

            _prana.SetActive(running);
            _drawPile.SetActive(running);
            _discardPile.SetActive(running);

            _endTurn.gameObject.SetActive(running && !over);
            _endTurn.interactable = playerTurn;
            if (running && !over)
            {
                _endTurnText.Set(LocalizationTableNames.UI, playerTurn ? CombatUIKeys.EndTurn : CombatUIKeys.EnemyTurn);
            }

            _banner.gameObject.SetActive(over);
            if (over && _style != null)
            {
                bool victory = phase == CombatPhase.Victory;
                _bannerText.Set(LocalizationTableNames.UI, victory ? CombatUIKeys.Victory : CombatUIKeys.Defeat);
                _bannerLabel.color = victory ? _style.VictoryTextColor : _style.DefeatTextColor;
            }

            _hand.RefreshPlayable();
            if (playerTurn)
            {
                _hand.SelectFirst();
            }
        }
    }
}
