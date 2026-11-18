// The combat HUD: the hand of pooled cards, Prana, draw and discard counts, the threat panel, the End Turn button,
// card motion and zoom, the rejected-card message, the targeting line, the turn banner, the result banner, the pile
// viewer and the enemy info panel. It builds its hierarchy in code under seven child canvases split by update rate
// (the hand changes on every play, the HUD panels on events, the enemy info panel follows its enemy, card flights,
// zoom and the message animate every frame while shown, as do the targeting line and the turn banner; the pile viewer
// is a modal overlay on top), applies every visual value from its styles (the Combat HUD, Combat Inspect and Targeting
// Line styles, and the card type visuals the HUD style lists), and re-applies them live in the Editor. The targeting line and the enemy info panel project enemies
// through the combat camera anchor. It only presents and
// forwards input to CombatFlow; it holds no combat rules. The threat panel shows during the player's turn while an
// attack is coming, and refreshes when an intent, Rama's Kavach or health (lost or healed), or an enemy's life changes.
// It finds the fight through the CombatController anchor when the CombatStarted channel fires, and hands the
// controller's enemy target views to the targeting presenter for select-then-confirm card targeting.
// Cards are played by clicking or by dragging (HandInputSurface over the hand, HandDragController behind it). End Turn
// glows gently once nothing in the hand is playable during the player's turn (EndTurnNudge). Prana shows as a row of
// orbs (PranaPanel) that drain and refill with the pool, preview the cost of the card being considered, and shake
// when a card is rejected for lack of Prana. The card being considered is also previewed on the fighters' health
// bars (OutcomePreviewPresenter), and the threat panel shows the threat as it would be after it.
// End Turn also responds to E on the keyboard and the north face button on a gamepad. The draw pile opens in the
// viewer from its panel, Q or the left shoulder; the discard pile from its panel, R or the right shoulder. The draw
// pile is shown sorted by cost then name so it never reveals the draw order; the discard pile shows newest first.
// Holding Tab or clicking in the left stick shows Rama's info panel (health, Kavach and his statuses explained) while
// held; hovering any fighter shows theirs.
// Rama's special arrows sit in the quiver panel (QuiverPanel) in the left column. During the player's turn an arrow
// fires from a click on its slot, from the submit button on a selected slot (move up from the hand to reach the
// quiver), or from the 1 to 3 keys. An arrow that needs an enemy starts arrow targeting from its slot when more than
// one enemy lives (the same highlight, keys and line as cards); otherwise it fires at once, at the only living enemy
// or untargeted. Cancelling arrow targeting puts the selection back on the slot. The arrow being considered is
// previewed on the fighters like a card (it takes priority), with its name and live description above the quiver.
// While the viewer is open the hand, targeting, the quiver and End Turn are blocked. While a special arrow is in
// flight (CombatFlow.CanPlayerAct is false) presses on the quiver, the arrow keys and End Turn are ignored; nothing
// dims, since the flight is too short for that to read as anything but a flicker.
using System;
using System.Collections.Generic;
using JourneyToLanka.Combat.Arrows;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.CombatScene;
using JourneyToLanka.Core.Events;
using JourneyToLanka.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JourneyToLanka.CombatUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [AddComponentMenu("Journey to Lanka/UI/Combat HUD")]
    public sealed class CombatHudView : MonoBehaviour
    {
        private static readonly (CardPlayCheck Check, string Key)[] RejectionKeys =
        {
            (CardPlayCheck.NotEnoughPrana, CombatUIKeys.NotEnoughPrana),
            (CardPlayCheck.InvalidTarget, CombatUIKeys.ChooseTarget),
        };

        private static readonly Comparison<CardInstance> CostThenName = CompareCostThenName;
        private static readonly Action<CardPile, List<CardInstance>> SortedByCost = FillSortedByCost;
        private static readonly Action<CardPile, List<CardInstance>> NewestFirst = FillNewestFirst;

        [SerializeField] private CombatHudStyleSO _style;
        [SerializeField] private CombatInspectStyleSO _inspectStyle;
        [SerializeField] private TargetingLineStyleSO _targetingLineStyle;
        [SerializeField] private CombatCameraAnchorSO _cameraAnchor;
        [SerializeField] private CombatControllerAnchorSO _controllerAnchor;
        [SerializeField] private VoidEventChannelSO _combatStarted;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _handArea;
        private HandInputSurface _handInput;
        private PranaPanel _prana;
        private QuiverPanel _quiver;
        private StatPanel _drawPile;
        private StatPanel _discardPile;
        private Button _endTurn;
        private Image _endTurnImage;
        private TextMeshProUGUI _endTurnLabel;
        private LocalizedText _endTurnText;
        private EndTurnNudge _endTurnNudge;
        private Image _banner;
        private TextMeshProUGUI _bannerLabel;
        private LocalizedText _bannerText;
        private ThreatPanel _threat;
        private TurnBanner _turnBanner;
        private CardMotionPresenter _motion;
        private HudToast _toast;
        private CardZoomPresenter _zoom;
        private PileViewer _pileViewer;
        private EnemyInfoPresenter _enemyInfo;
        private TargetingLinePresenter _targetingLine;
        private OutcomePreviewPresenter _outcomes;
        private HandPresenter _hand;
        private TargetingPresenter _targeting;
        private InputAction _endTurnAction;
        private InputAction _viewDrawAction;
        private InputAction _viewDiscardAction;
        private InputAction _inspectSelfAction;
        private InputAction[] _arrowKeyActions;
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
            _viewDrawAction = new InputAction("ViewDrawPile", InputActionType.Button, "<Keyboard>/q");
            _viewDrawAction.AddBinding("<Gamepad>/leftShoulder");
            _viewDrawAction.performed += _ => ToggleDrawPile();
            _viewDiscardAction = new InputAction("ViewDiscardPile", InputActionType.Button, "<Keyboard>/r");
            _viewDiscardAction.AddBinding("<Gamepad>/rightShoulder");
            _viewDiscardAction.performed += _ => ToggleDiscardPile();
            _inspectSelfAction = new InputAction("InspectSelf", InputActionType.Button, "<Keyboard>/tab");
            _inspectSelfAction.AddBinding("<Gamepad>/leftStickPress");
            _inspectSelfAction.performed += _ => _enemyInfo?.SetSelfInspect(true);
            _inspectSelfAction.canceled += _ => _enemyInfo?.SetSelfInspect(false);
            _arrowKeyActions = new InputAction[3];
            for (int i = 0; i < _arrowKeyActions.Length; i++)
            {
                int slot = i;
                var action = new InputAction("FireArrow" + (i + 1), InputActionType.Button, "<Keyboard>/" + (i + 1));
                action.AddBinding("<Keyboard>/numpad" + (i + 1));
                action.performed += _ => HandleArrowSlotActivated(slot);
                _arrowKeyActions[i] = action;
            }

            if (_style == null)
            {
                Debug.LogError($"{name}: no Combat HUD Style assigned. Run Tools > Journey to Lanka > Wire Selected > Combat UI.", this);
            }

            if (_style != null && _style.HandLayout == null)
            {
                Debug.LogError($"{name}: the Combat HUD Style has no Hand Layout. Run Tools > Journey to Lanka > Create Default Assets > Hand Layouts.", this);
            }

            if (_style != null && _style.PranaOrbs == null)
            {
                Debug.LogError($"{name}: the Combat HUD Style has no Prana Orbs style. Run Tools > Journey to Lanka > Create Default Assets > Prana Orbs.", this);
            }

            if (_style != null && _style.Quiver == null)
            {
                Debug.LogError($"{name}: the Combat HUD Style has no Quiver style. Run Tools > Journey to Lanka > Create Default Assets > Quiver.", this);
            }

            if (_inspectStyle == null)
            {
                Debug.LogError($"{name}: no Combat Inspect Style assigned. Run Tools > Journey to Lanka > Wire Selected > Combat Inspect.", this);
            }

            if (_targetingLineStyle == null || _cameraAnchor == null)
            {
                Debug.LogError($"{name}: Targeting Line Style or Combat Camera Anchor not assigned. Run Tools > Journey to Lanka > Wire Selected > Targeting Line.", this);
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

            SetCardTypeVisualsListening(true);
            SetPranaOrbsListening(true);
            SetQuiverListening(true);

            if (_inspectStyle != null)
            {
                _inspectStyle.Changed += HandleStyleChanged;
            }

            if (_targetingLineStyle != null)
            {
                _targetingLineStyle.Changed += HandleStyleChanged;
            }

            if (_combatStarted != null)
            {
                _combatStarted.Register(HandleCombatStarted);
            }

            _endTurnAction.Enable();
            _viewDrawAction.Enable();
            _viewDiscardAction.Enable();
            _inspectSelfAction.Enable();
            SetArrowKeysEnabled(true);
            TryAttachToRunningCombat();
        }

        private void OnDisable()
        {
            if (_style != null)
            {
                _style.Changed -= HandleStyleChanged;
            }

            SetCardTypeVisualsListening(false);
            SetPranaOrbsListening(false);
            SetQuiverListening(false);

            if (_inspectStyle != null)
            {
                _inspectStyle.Changed -= HandleStyleChanged;
            }

            if (_targetingLineStyle != null)
            {
                _targetingLineStyle.Changed -= HandleStyleChanged;
            }

            if (_combatStarted != null)
            {
                _combatStarted.Unregister(HandleCombatStarted);
            }

            _endTurnAction.Disable();
            _viewDrawAction.Disable();
            _viewDiscardAction.Disable();
            _inspectSelfAction.Disable();
            SetArrowKeysEnabled(false);
            _enemyInfo?.SetSelfInspect(false);
            Detach();
        }

        private void OnDestroy()
        {
            if (_endTurn != null)
            {
                _endTurn.onClick.RemoveListener(HandleEndTurnPressed);
            }

            _endTurnAction?.Dispose();
            _viewDrawAction?.Dispose();
            _viewDiscardAction?.Dispose();
            _inspectSelfAction?.Dispose();
            if (_arrowKeyActions != null)
            {
                for (int i = 0; i < _arrowKeyActions.Length; i++)
                {
                    _arrowKeyActions[i].Dispose();
                }
            }

            if (_hand != null)
            {
                _hand.PlayRejected -= HandlePlayRejected;
                _hand.PlayableChanged -= RefreshEndTurnNudge;
                _hand.ConsideredCardChanged -= HandleConsideredCardChanged;
                _hand.NavigationChanged -= HandleHandNavigationChanged;
                _hand.Dispose();
            }

            if (_quiver != null)
            {
                _quiver.SlotActivated -= HandleArrowSlotActivated;
                _quiver.ConsideredSlotChanged -= HandleConsideredArrowChanged;
                _quiver.NavigationChanged -= HandleQuiverNavigationChanged;
                _quiver.SelectionLost -= HandleQuiverSelectionLost;
                _quiver.Dispose();
            }

            if (_targeting != null)
            {
                _targeting.ArrowCancelled -= HandleArrowCancelled;
            }

            _endTurnNudge?.Dispose();

            if (_pileViewer != null)
            {
                _pileViewer.OpenChanged -= HandlePileViewerOpenChanged;
                _pileViewer.Dispose();
            }

            if (_outcomes != null)
            {
                _outcomes.PreviewChanged -= RefreshThreat;
                _outcomes.Dispose();
            }

            _enemyInfo?.Dispose();
            _targetingLine?.Dispose();
            _zoom?.Dispose();
            _motion?.Dispose();
            _toast?.Dispose();
            _targeting?.Dispose();
            _endTurnText?.Clear();
            _bannerText?.Clear();
            _threat?.Dispose();
            _turnBanner?.Dispose();
            _prana?.Dispose();
            _drawPile?.Dispose();
            _discardPile?.Dispose();
        }

        private void Build()
        {
            RectTransform handCanvas = CreateChildCanvas("HandCanvas", true);
            _handArea = UIBuildUtility.CreateRect("Hand", handCanvas);
            CanvasGroup handGroup = _handArea.gameObject.AddComponent<CanvasGroup>();
            _handInput = HandInputSurface.Create(handCanvas);

            RectTransform hudCanvas = CreateChildCanvas("HudCanvas", true);
            _prana = PranaPanel.Create(hudCanvas);
            _quiver = QuiverPanel.Create(hudCanvas);
            _drawPile = StatPanel.Create("DrawPile", hudCanvas, LocalizationTableNames.UI, CombatUIKeys.DrawPile);
            _discardPile = StatPanel.Create("DiscardPile", hudCanvas, LocalizationTableNames.UI, CombatUIKeys.DiscardPile);
            _threat = ThreatPanel.Create(hudCanvas);

            _endTurnImage = UIBuildUtility.CreateImage("EndTurn", hudCanvas, true);
            _endTurn = _endTurnImage.gameObject.AddComponent<Button>();
            _endTurn.targetGraphic = _endTurnImage;
            _endTurn.transition = Selectable.Transition.ColorTint;
            _endTurnLabel = UIBuildUtility.CreateText("Label", _endTurnImage.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(_endTurnLabel.rectTransform);
            _endTurnText = new LocalizedText(_endTurnLabel);
            _endTurnNudge = new EndTurnNudge(_endTurnImage);

            _banner = UIBuildUtility.CreateImage("ResultBanner", hudCanvas, false);
            _bannerLabel = UIBuildUtility.CreateText("Label", _banner.transform, TextAlignmentOptions.Center);
            UIBuildUtility.Stretch(_bannerLabel.rectTransform);
            _bannerText = new LocalizedText(_bannerLabel);

            RectTransform enemyInfoCanvas = CreateChildCanvas("EnemyInfoCanvas", false);

            RectTransform motionCanvas = CreateChildCanvas("MotionCanvas", false);
            CanvasGroup motionGroup = motionCanvas.gameObject.AddComponent<CanvasGroup>();
            motionGroup.interactable = false;
            motionGroup.blocksRaycasts = false;

            RectTransform lineCanvas = CreateChildCanvas("TargetingLineCanvas", false);
            _targetingLine = TargetingLinePresenter.Create(lineCanvas);

            RectTransform bannerCanvas = CreateChildCanvas("TurnBannerCanvas", false);
            _turnBanner = TurnBanner.Create(bannerCanvas);

            RectTransform templates = UIBuildUtility.CreateRect("Templates", transform);
            templates.gameObject.SetActive(false);
            CardView template = CardViewFactory.CreateTemplate(templates);

            RectTransform viewerCanvas = CreateChildCanvas("PileViewerCanvas", true);
            _pileViewer = PileViewer.Create(viewerCanvas, template);
            _pileViewer.OpenChanged += HandlePileViewerOpenChanged;
            _drawPile.EnableButton(ToggleDrawPile);
            _discardPile.EnableButton(ToggleDiscardPile);

            _motion = new CardMotionPresenter(template, motionCanvas, _drawPile, _discardPile);
            _zoom = new CardZoomPresenter(template, motionCanvas);
            _toast = HudToast.Create(motionCanvas);
            _enemyInfo = EnemyInfoPresenter.Create(enemyInfoCanvas);
            _targeting = new TargetingPresenter();
            _hand = new HandPresenter(template, _handArea, _handInput, handGroup, _endTurn, _targeting, _targetingLine,
                _motion, _zoom);
            _hand.PlayRejected += HandlePlayRejected;
            _hand.PlayableChanged += RefreshEndTurnNudge;
            _hand.ConsideredCardChanged += HandleConsideredCardChanged;
            _hand.NavigationChanged += HandleHandNavigationChanged;
            _targeting.ArrowCancelled += HandleArrowCancelled;
            _quiver.SlotActivated += HandleArrowSlotActivated;
            _quiver.ConsideredSlotChanged += HandleConsideredArrowChanged;
            _quiver.NavigationChanged += HandleQuiverNavigationChanged;
            _quiver.SelectionLost += HandleQuiverSelectionLost;
            _outcomes = new OutcomePreviewPresenter();
            _outcomes.PreviewChanged += RefreshThreat;
        }

        private RectTransform CreateChildCanvas(string canvasName, bool receivesInput)
        {
            RectTransform rect = UIBuildUtility.CreateRect(canvasName, transform);
            UIBuildUtility.Stretch(rect);
            rect.gameObject.AddComponent<Canvas>();
            if (receivesInput)
            {
                rect.gameObject.AddComponent<GraphicRaycaster>();
            }

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

            _prana.ApplyStyle(_style, _style.PranaOrbs, _style.PranaOffset);
            _quiver.ApplyStyle(_style, _style.Quiver);
            _drawPile.ApplyStyle(_style, Vector2.zero, _style.DrawPileOffset);
            _discardPile.ApplyStyle(_style, new Vector2(1f, 0f), _style.DiscardPileOffset);

            UIBuildUtility.Place(_endTurnImage.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                _style.EndTurnOffset, _style.EndTurnSize);
            UIBuildUtility.StyleImage(_endTurnImage, _style.EndTurnSprite, _style.EndTurnColor);
            _endTurnNudge.SetStyle(_style);
            _endTurn.colors = CardView.CreateTint(_style);
            UIBuildUtility.StyleText(_endTurnLabel, _style.Font, _style.EndTurnFontSize, _style.EndTurnTextColor);

            UIBuildUtility.Place(_banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, _style.BannerSize);
            UIBuildUtility.StyleImage(_banner, null, _style.BannerColor);
            UIBuildUtility.StyleText(_bannerLabel, _style.Font, _style.BannerFontSize, _style.VictoryTextColor);

            _threat.ApplyStyle(_style);
            _turnBanner.ApplyStyle(_style);
            _motion.SetStyle(_style);
            _zoom.SetStyles(_style, _inspectStyle);
            _pileViewer.ApplyStyle(_style, _inspectStyle);
            _enemyInfo.ApplyStyle(_style, _inspectStyle);
            _targetingLine.SetStyle(_targetingLineStyle, _cameraAnchor);
            _toast.ApplyStyle(_style);
            _hand.SetStyle(_style);
            _targeting.SetStyle(_style);
        }

        private void HandleStyleChanged()
        {
            ApplyStyle();
            RefreshAll();
        }

        private void SetCardTypeVisualsListening(bool listening)
        {
            if (_style == null)
            {
                return;
            }

            SetVisualsListening(_style.DefaultCardTypeVisuals, listening);
            for (int i = 0; i < _style.CardTypeVisualsCount; i++)
            {
                SetVisualsListening(_style.GetCardTypeVisuals(i), listening);
            }
        }

        private void SetPranaOrbsListening(bool listening)
        {
            PranaOrbsStyleSO orbs = _style != null ? _style.PranaOrbs : null;
            if (orbs == null)
            {
                return;
            }

            orbs.Changed -= HandleStyleChanged;
            if (listening)
            {
                orbs.Changed += HandleStyleChanged;
            }
        }

        private void SetQuiverListening(bool listening)
        {
            QuiverStyleSO quiver = _style != null ? _style.Quiver : null;
            if (quiver == null)
            {
                return;
            }

            quiver.Changed -= HandleStyleChanged;
            if (listening)
            {
                quiver.Changed += HandleStyleChanged;
            }
        }

        private void SetArrowKeysEnabled(bool enabled)
        {
            for (int i = 0; i < _arrowKeyActions.Length; i++)
            {
                if (enabled)
                {
                    _arrowKeyActions[i].Enable();
                }
                else
                {
                    _arrowKeyActions[i].Disable();
                }
            }
        }

        private void SetVisualsListening(CardTypeVisualsSO visuals, bool listening)
        {
            if (visuals == null)
            {
                return;
            }

            visuals.Changed -= HandleStyleChanged;
            if (listening)
            {
                visuals.Changed += HandleStyleChanged;
            }
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
                Attach(controller.Flow, controller);
            }
        }

        private void Attach(CombatFlow flow, CombatController controller)
        {
            Detach();
            _flow = flow;
            CombatModel model = flow.Model;

            flow.PhaseChanged += HandlePhaseChanged;
            model.Prana.Changed += HandlePranaChanged;
            model.Deck.CardMoved += HandleCardMoved;
            model.Deck.Reshuffled += HandleReshuffled;
            model.Player.BlockChanged += HandlePlayerBlockChanged;
            model.Player.Damaged += HandlePlayerDamaged;
            model.Player.Healed += HandlePlayerHealed;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                EnemyCombatant enemy = model.Enemies[i];
                enemy.IntentChanged += HandleIntentChanged;
                enemy.Fighter.Died += HandleEnemyDied;
            }

            _motion.Attach(model.Deck);
            _hand.Attach(flow);
            _targeting.Attach(flow, controller);
            _quiver.Attach(model, _targeting);
            _enemyInfo.Attach(flow, controller, _targeting, _cameraAnchor);
            _targetingLine.Attach(_targeting, controller);
            _outcomes.Attach(flow, controller, _targeting);

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
            model.Player.BlockChanged -= HandlePlayerBlockChanged;
            model.Player.Damaged -= HandlePlayerDamaged;
            model.Player.Healed -= HandlePlayerHealed;
            for (int i = 0; i < model.Enemies.Count; i++)
            {
                EnemyCombatant enemy = model.Enemies[i];
                enemy.IntentChanged -= HandleIntentChanged;
                enemy.Fighter.Died -= HandleEnemyDied;
            }

            _pileViewer.Close();
            _outcomes.Detach();
            _enemyInfo.Detach();
            _targetingLine.Detach();
            _zoom.HideNow();
            _targeting.Detach();
            _quiver.Detach();
            _motion.Detach();
            _hand.Detach();
            _flow = null;
            _endTurnNudge.SetActive(false);
            _turnBanner.Hide();
            _toast.Hide();
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            RefreshPhase();
            PlayTurnBanner(phase);
        }

        private void HandlePranaChanged(PranaPool prana)
        {
            RefreshPrana(true);
            _hand.RefreshPlayable();
        }

        private void HandleConsideredCardChanged(CardInstance card)
        {
            _prana.SetPreviewCost(card != null ? card.Cost : -1);
            _outcomes.SetCard(card);
        }

        private void HandleConsideredArrowChanged(int slot)
        {
            _outcomes.SetArrow(slot);
        }

        private void HandleArrowSlotActivated(int slot)
        {
            if (_flow == null || !_flow.CanPlayerAct || _pileViewer.IsOpen)
            {
                return;
            }

            CombatModel model = _flow.Model;
            if (!model.Quiver.HasArrow(slot))
            {
                return;
            }

            if (_targeting.IsArrowMode && _targeting.ArrowSlot == slot)
            {
                _targeting.Cancel();
                return;
            }

            ArrowDefinitionSO arrow = model.Quiver[slot];
            if (arrow.RequiresChosenTarget && _targeting.TryBeginArrow(_quiver.GetSlotRect(slot), slot))
            {
                return;
            }

            _targeting.Abort();
            _flow.FireArrow(slot, arrow.RequiresChosenTarget ? model.FirstLivingEnemyIndex() : -1);
        }

        private void HandleArrowCancelled(int slot)
        {
            _quiver.Select(slot);
        }

        private void HandleQuiverNavigationChanged()
        {
            _hand.SetUpNeighbour(_quiver.FirstSelectable);
        }

        private void HandleHandNavigationChanged()
        {
            Selectable firstCard = _hand.FirstCardButton;
            _quiver.SetDownNeighbour(firstCard != null ? firstCard : _endTurn);
        }

        private void HandleQuiverSelectionLost()
        {
            if (_flow != null && _flow.IsPlayerTurn && !_pileViewer.IsOpen)
            {
                _hand.SelectFirst();
            }
        }

        private void HandleCardMoved(CardInstance card, CardPile from, CardPile to)
        {
            RefreshPiles();
            if (_pileViewer.IsOpen && (from == _pileViewer.Pile || to == _pileViewer.Pile))
            {
                _pileViewer.Refresh();
            }
        }

        private void HandleReshuffled()
        {
            RefreshPiles();
            _pileViewer.Refresh();
        }

        private void ToggleDrawPile()
        {
            if (_flow != null)
            {
                TogglePile(_flow.Model.Deck.DrawPile, CombatUIKeys.DrawPileTitle, SortedByCost);
            }
        }

        private void ToggleDiscardPile()
        {
            if (_flow != null)
            {
                TogglePile(_flow.Model.Deck.DiscardPile, CombatUIKeys.DiscardPileTitle, NewestFirst);
            }
        }

        private void TogglePile(CardPile pile, string titleKey, Action<CardPile, List<CardInstance>> fill)
        {
            if (_pileViewer.IsOpen && _pileViewer.Pile == pile)
            {
                _pileViewer.Close();
                return;
            }

            if (_flow.IsOver || _flow.Phase == CombatPhase.None)
            {
                return;
            }

            _pileViewer.Open(pile, titleKey, fill);
        }

        private void HandlePileViewerOpenChanged(bool open)
        {
            if (open)
            {
                _targeting.Abort();
                _zoom.HideNow();
            }

            _hand.SetBlocked(open);
            RefreshTurnControls();

            EventSystem eventSystem = EventSystem.current;
            if (!open && _flow != null && _flow.IsPlayerTurn && eventSystem != null && eventSystem.currentSelectedGameObject == null)
            {
                _hand.SelectFirst();
            }
        }

        private void HandlePlayerBlockChanged(Combatant player)
        {
            RefreshThreat();
        }

        private void HandlePlayerDamaged(Combatant player, DamageResult result)
        {
            RefreshThreat();
        }

        private void HandlePlayerHealed(Combatant player, int restored)
        {
            RefreshThreat();
        }

        private void HandleIntentChanged(EnemyCombatant enemy)
        {
            RefreshThreat();
        }

        private void HandleEnemyDied(Combatant enemy)
        {
            RefreshThreat();
        }

        private void HandlePlayRejected(CardPlayCheck check)
        {
            for (int i = 0; i < RejectionKeys.Length; i++)
            {
                if (RejectionKeys[i].Check == check)
                {
                    if (check == CardPlayCheck.NotEnoughPrana)
                    {
                        _prana.PlayRejected();
                    }

                    _toast.Show(RejectionKeys[i].Key);
                    return;
                }
            }
        }

        private void HandleEndTurnPressed()
        {
            if (_flow != null && _flow.CanPlayerAct && !_pileViewer.IsOpen)
            {
                _flow.EndTurn();
            }
        }

        private void RefreshAll()
        {
            RefreshPrana(false);
            RefreshPiles();
            RefreshPhase();
        }

        private void RefreshPrana(bool animate)
        {
            if (_flow != null)
            {
                _prana.Show(_flow.Model.Prana.Current, _flow.Model.Prana.PerTurn, animate);
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
            _quiver.SetActive(running && !over);
            _drawPile.SetActive(running);
            _discardPile.SetActive(running);

            _hand.RefreshPlayable();
            _endTurn.gameObject.SetActive(running && !over);
            RefreshTurnControls();
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

            if (over)
            {
                _turnBanner.Hide();
                _toast.Hide();
                _pileViewer.Close();
            }

            RefreshThreat();
            if (playerTurn && !_pileViewer.IsOpen)
            {
                _hand.SelectFirst();
            }
        }

        private void RefreshTurnControls()
        {
            bool usable = _flow != null && _flow.IsPlayerTurn && !_pileViewer.IsOpen;
            _endTurn.interactable = usable;
            _quiver.SetInteractable(usable);
            RefreshEndTurnNudge();
        }

        private void RefreshEndTurnNudge()
        {
            _endTurnNudge.SetActive(_flow != null && _flow.IsPlayerTurn && _endTurn.interactable
                && _endTurn.gameObject.activeInHierarchy && !_hand.HasPlayableCard);
        }

        private static void FillSortedByCost(CardPile pile, List<CardInstance> cards)
        {
            for (int i = 0; i < pile.Count; i++)
            {
                cards.Add(pile[i]);
            }

            cards.Sort(CostThenName);
        }

        private static void FillNewestFirst(CardPile pile, List<CardInstance> cards)
        {
            for (int i = pile.Count - 1; i >= 0; i--)
            {
                cards.Add(pile[i]);
            }
        }

        private static int CompareCostThenName(CardInstance a, CardInstance b)
        {
            int byCost = a.Cost.CompareTo(b.Cost);
            if (byCost != 0)
            {
                return byCost;
            }

            int byName = string.CompareOrdinal(a.Definition.NameKey, b.Definition.NameKey);
            return byName != 0 ? byName : a.Id.CompareTo(b.Id);
        }

        private void RefreshThreat()
        {
            if (_flow == null || !_flow.IsPlayerTurn)
            {
                _threat.Hide();
                return;
            }

            IncomingThreat threat = IncomingThreat.Calculate(_flow.Model);
            if (!threat.HasAttack)
            {
                _threat.Hide();
                return;
            }

            bool previewing = _outcomes.IsActive;
            _threat.Show(previewing ? IncomingThreat.Calculate(_flow.Model, _outcomes.Outcome) : threat, previewing);
        }

        private void PlayTurnBanner(CombatPhase phase)
        {
            if (_style == null)
            {
                return;
            }

            if (phase == CombatPhase.PlayerTurn)
            {
                _turnBanner.Play(CombatUIKeys.YourTurn, _style.PlayerTurnTextColor);
            }
            else if (phase == CombatPhase.EnemyTurn)
            {
                _turnBanner.Play(CombatUIKeys.EnemyTurn, _style.EnemyTurnTextColor);
            }
            else
            {
                _turnBanner.Hide();
            }
        }
    }
}
