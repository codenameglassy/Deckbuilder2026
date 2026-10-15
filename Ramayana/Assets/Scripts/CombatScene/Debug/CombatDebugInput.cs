// TEMPORARY test driver until the hand UI in Step 6, which replaces it. Delete this file then.
// Keys 1 to 9 and 0 play the card in that hand slot at the first living enemy; E or Enter ends the turn.
// Uses Input System actions created in code, so it reacts to key presses instead of polling every frame.
// Cards go through CombatFlow, so they use the same command pipeline the real UI will.
using JourneyToLanka.Combat.Flow;
using JourneyToLanka.Combat.Model;
using JourneyToLanka.Core.Commands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JourneyToLanka.CombatScene
{
    [RequireComponent(typeof(CombatController))]
    [AddComponentMenu("Journey to Lanka/Combat/Debug/Combat Debug Input")]
    public sealed class CombatDebugInput : MonoBehaviour
    {
        private static readonly string[] SlotKeys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        private CombatController _controller;
        private CombatFlow _flow;
        private InputAction[] _slotActions;
        private InputAction _endTurnAction;

        private void Awake()
        {
            _controller = GetComponent<CombatController>();

            _slotActions = new InputAction[SlotKeys.Length];
            for (int i = 0; i < SlotKeys.Length; i++)
            {
                int slot = i;
                var action = new InputAction("PlaySlot" + SlotKeys[i], InputActionType.Button, "<Keyboard>/" + SlotKeys[i]);
                action.performed += _ => PlaySlot(slot);
                _slotActions[i] = action;
            }

            _endTurnAction = new InputAction("EndTurn", InputActionType.Button, "<Keyboard>/e");
            _endTurnAction.AddBinding("<Keyboard>/enter");
            _endTurnAction.performed += _ => EndTurn();
        }

        private void OnEnable()
        {
            _controller.CombatBegan += HandleCombatBegan;
            _flow = _controller.Flow;

            for (int i = 0; i < _slotActions.Length; i++)
            {
                _slotActions[i].Enable();
            }

            _endTurnAction.Enable();
        }

        private void OnDisable()
        {
            _controller.CombatBegan -= HandleCombatBegan;

            for (int i = 0; i < _slotActions.Length; i++)
            {
                _slotActions[i].Disable();
            }

            _endTurnAction.Disable();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _slotActions.Length; i++)
            {
                _slotActions[i].Dispose();
            }

            _endTurnAction.Dispose();
        }

        private void HandleCombatBegan(CombatFlow flow)
        {
            _flow = flow;
        }

        private void PlaySlot(int slot)
        {
            if (_flow == null || !_flow.IsPlayerTurn)
            {
                return;
            }

            CardPile hand = _flow.Model.Deck.Hand;
            if (slot >= hand.Count)
            {
                return;
            }

            CardInstance card = hand[slot];
            int target = card.RequiresChosenTarget ? _flow.Model.FirstLivingEnemyIndex() : -1;
            CardPlayCheck check = _flow.Model.CheckPlay(card.Id, target);

            if (_flow.PlayCard(card.Id, target) == CommandSubmitResult.Rejected)
            {
                Debug.Log($"[CombatDebugInput] Slot {slot + 1} ({card.Definition.Id}) rejected: {check}.", this);
            }
        }

        private void EndTurn()
        {
            if (_flow != null)
            {
                _flow.EndTurn();
            }
        }
    }
}
