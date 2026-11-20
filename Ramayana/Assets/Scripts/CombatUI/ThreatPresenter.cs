// Tells Rama's health bar how much health the enemies' next moves will take from him, so the bar can mark it (striped
// segment and a hanging "-11" tag, see HealthBarThreat). The HUD decides when: during the player's turn while an attack
// is coming, and, while the player considers a card, with the threat as it would be after that card (shown in the
// preview colour). When Kavach absorbs everything there is nothing to mark. It only forwards an IncomingThreat to
// CombatController.PlayerView and clears the mark when hidden or detached; it draws nothing itself.
using JourneyToLanka.Combat.Model;
using JourneyToLanka.CombatScene;

namespace JourneyToLanka.CombatUI
{
    internal sealed class ThreatPresenter
    {
        private CombatController _controller;

        public void Attach(CombatController controller)
        {
            _controller = controller;
        }

        public void Detach()
        {
            Hide();
            _controller = null;
        }

        public void Show(in IncomingThreat threat, bool previewing)
        {
            CombatantView player = _controller != null ? _controller.PlayerView : null;
            if (player == null)
            {
                return;
            }

            if (threat.Taken > 0)
            {
                player.ShowThreat(threat.Taken, threat.IsLethal, previewing);
            }
            else
            {
                player.ClearThreat();
            }
        }

        public void Hide()
        {
            CombatantView player = _controller != null ? _controller.PlayerView : null;
            if (player != null)
            {
                player.ClearThreat();
            }
        }
    }
}
