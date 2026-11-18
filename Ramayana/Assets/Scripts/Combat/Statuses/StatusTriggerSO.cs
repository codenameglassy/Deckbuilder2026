// Strategy for what a status does when something happens to its holder, beyond counting down and changing damage:
// at the start of its holder's turn (Vow of the Exile gains Dharma), or when its holder gains a status (Devotion turns
// gained Dharma into Kavach). It acts only through the context's actions, so a card preview simulates it too. A
// status with no trigger asset does nothing at these moments. Concrete triggers are shared, immutable assets.
using UnityEngine;

namespace JourneyToLanka.Combat.Statuses
{
    public abstract class StatusTriggerSO : ScriptableObject
    {
        public virtual void OnOwnerTurnStart(in StatusInstance status, in StatusTickContext context)
        {
        }

        public virtual void OnOwnerGainedStatus(in StatusInstance status, StatusDefinitionSO gained, int amount,
            in StatusTickContext context)
        {
        }
    }
}
