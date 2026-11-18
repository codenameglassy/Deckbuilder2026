// Plain C# holder for the active run. Starting a new run while one is active ends the old one first. When the player
// definition's dev starting deck is in use (Editor and Development builds only), each new run logs a warning naming
// the deck, so a test deck is never mistaken for the real one.
using System;
using UnityEngine;

namespace JourneyToLanka.Run
{
    public sealed class RunService : IRunService
    {
        public event Action<RunState> RunStarted;

        public event Action<RunState> RunEnded;

        public RunState Current { get; private set; }

        public bool HasActiveRun => Current != null;

        public RunState StartNew(RunDefinitionSO definition, int seed)
        {
            EndRun();
            Current = new RunState(definition, seed);
            WarnIfDevDeck(definition);
            RunStarted?.Invoke(Current);
            return Current;
        }

        public void EndRun()
        {
            if (Current == null)
            {
                return;
            }

            RunState ended = Current;
            Current = null;
            RunEnded?.Invoke(ended);
        }

        private static void WarnIfDevDeck(RunDefinitionSO definition)
        {
            if (definition.Player.UsesDevStartingDeck)
            {
                Debug.LogWarning($"Run started with the dev starting deck {definition.Player.StartingDeck.name}. " +
                                 $"Clear Dev Starting Deck on {definition.Player.name} to play the real deck.",
                    definition.Player);
            }
        }
    }
}
