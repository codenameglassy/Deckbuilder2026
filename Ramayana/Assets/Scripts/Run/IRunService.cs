// Holds the active run across scene loads. Installed in Boot because the run outlives every gameplay scene;
// gameplay reads it and the run director advances it. Only one run is active at a time.
using System;

namespace JourneyToLanka.Run
{
    public interface IRunService
    {
        event Action<RunState> RunStarted;

        event Action<RunState> RunEnded;

        RunState Current { get; }

        bool HasActiveRun { get; }

        RunState StartNew(RunDefinitionSO definition, int seed);

        void EndRun();
    }
}
