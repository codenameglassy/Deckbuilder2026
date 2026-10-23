// Plain C# holder for the active run. Starting a new run while one is active ends the old one first.
using System;

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
    }
}
