// TEMPORARY on-screen readout until the hand UI in Step 6, which replaces it. Delete this file then.
// Shows phase, turn, Prana, the hand with slot numbers, pile counts, and each enemy's health, Kavach and next
// move. Card and move ids are developer text, not localized. The text is rebuilt only when combat state
// changes (a dirty flag set by model events), then drawn with IMGUI.
using System.Text;
using JourneyToLanka.Combat.Enemies;
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    [RequireComponent(typeof(CombatController))]
    [AddComponentMenu("Journey to Lanka/Combat/Debug/Combat Debug HUD")]
    public sealed class CombatDebugHud : MonoBehaviour
    {
        [SerializeField] private Rect _area = new Rect(10f, 10f, 560f, 260f);

        private readonly StringBuilder _builder = new StringBuilder(1024);
        private CombatController _controller;
        private CombatFlow _flow;
        private string _text = "Waiting for combat to start.";
        private bool _dirty;

        private void Awake()
        {
            _controller = GetComponent<CombatController>();
        }

        private void OnEnable()
        {
            _controller.CombatBegan += Attach;
            if (_controller.Flow != null)
            {
                Attach(_controller.Flow);
            }
        }

        private void OnDisable()
        {
            _controller.CombatBegan -= Attach;
            Detach();
        }

        private void OnGUI()
        {
            if (_dirty)
            {
                Rebuild();
            }

            GUI.Box(_area, GUIContent.none);
            GUI.Label(new Rect(_area.x + 8f, _area.y + 6f, _area.width - 16f, _area.height - 12f), _text);
        }

        private void Attach(CombatFlow flow)
        {
            Detach();
            _flow = flow;
            CombatModel model = flow.Model;

            flow.PhaseChanged += HandlePhaseChanged;
            model.Deck.CardMoved += HandleCardMoved;
            model.Deck.Reshuffled += MarkDirty;
            model.Prana.Changed += HandlePranaChanged;
            model.Player.Damaged += HandleDamaged;
            model.Player.BlockChanged += HandleCombatantChanged;

            for (int i = 0; i < model.Enemies.Count; i++)
            {
                EnemyCombatant enemy = model.Enemies[i];
                enemy.IntentChanged += HandleIntentChanged;
                enemy.Fighter.Damaged += HandleDamaged;
                enemy.Fighter.BlockChanged += HandleCombatantChanged;
            }

            _dirty = true;
        }

        private void Detach()
        {
            if (_flow == null)
            {
                return;
            }

            CombatModel model = _flow.Model;
            _flow.PhaseChanged -= HandlePhaseChanged;
            model.Deck.CardMoved -= HandleCardMoved;
            model.Deck.Reshuffled -= MarkDirty;
            model.Prana.Changed -= HandlePranaChanged;
            model.Player.Damaged -= HandleDamaged;
            model.Player.BlockChanged -= HandleCombatantChanged;

            for (int i = 0; i < model.Enemies.Count; i++)
            {
                EnemyCombatant enemy = model.Enemies[i];
                enemy.IntentChanged -= HandleIntentChanged;
                enemy.Fighter.Damaged -= HandleDamaged;
                enemy.Fighter.BlockChanged -= HandleCombatantChanged;
            }

            _flow = null;
        }

        private void MarkDirty()
        {
            _dirty = true;
        }

        private void HandlePhaseChanged(CombatPhase phase)
        {
            _dirty = true;
        }

        private void HandleCardMoved(CardInstance card, CardPile from, CardPile to)
        {
            _dirty = true;
        }

        private void HandlePranaChanged(PranaPool prana)
        {
            _dirty = true;
        }

        private void HandleDamaged(Combatant combatant, DamageResult result)
        {
            _dirty = true;
        }

        private void HandleCombatantChanged(Combatant combatant)
        {
            _dirty = true;
        }

        private void HandleIntentChanged(EnemyCombatant enemy)
        {
            _dirty = true;
        }

        private void Rebuild()
        {
            _dirty = false;
            if (_flow == null)
            {
                return;
            }

            CombatModel model = _flow.Model;
            _builder.Clear();
            _builder.Append("Seed ").Append(_controller.Seed)
                .Append("   Turn ").Append(model.TurnNumber)
                .Append("   ").Append(_flow.Phase)
                .Append("   Prana ").Append(model.Prana.Current).Append('/').Append(model.Prana.PerTurn)
                .Append('\n');

            _builder.Append("Rama ").Append(model.Player.Health).Append('/').Append(model.Player.MaxHealth)
                .Append("   Kavach ").Append(model.Player.Block)
                .Append("   Draw ").Append(model.Deck.DrawPile.Count)
                .Append("   Discard ").Append(model.Deck.DiscardPile.Count)
                .Append('\n');

            CardPile hand = model.Deck.Hand;
            _builder.Append("Hand:");
            for (int i = 0; i < hand.Count; i++)
            {
                CardInstance card = hand[i];
                _builder.Append("  [").Append(i == 9 ? 0 : i + 1).Append("] ")
                    .Append(card.Definition.Id).Append(" (").Append(card.Cost).Append(')');
            }

            _builder.Append('\n');

            for (int i = 0; i < model.Enemies.Count; i++)
            {
                EnemyCombatant enemy = model.Enemies[i];
                EnemyMoveSO move = enemy.CurrentMove;
                _builder.Append("Enemy ").Append(i).Append(' ').Append(enemy.Definition.Id)
                    .Append("  ").Append(enemy.Fighter.Health).Append('/').Append(enemy.Fighter.MaxHealth)
                    .Append("   Kavach ").Append(enemy.Fighter.Block);

                if (enemy.IsAlive)
                {
                    _builder.Append("   Next: ").Append(move != null ? move.Id : "none");
                }

                _builder.Append('\n');
            }

            _builder.Append("\nKeys: 1-9, 0 play a card   E or Enter end turn");
            _text = _builder.ToString();
        }
    }
}
