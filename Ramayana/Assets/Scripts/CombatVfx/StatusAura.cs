// One status aura playing on a fighter, as StatusAuraPresenter tracks it: which fighter (by slot), which row of the
// status VFX profile, the pooled copy playing it, and whether it has been told to stop (it then fades out in place and
// is returned once its particles are gone). Plain data, kept in a reusable list.
namespace JourneyToLanka.CombatVfx
{
    internal struct StatusAura
    {
        public StatusAura(int slot, int entryIndex, PrefabVfxInstance instance)
        {
            Slot = slot;
            EntryIndex = entryIndex;
            Instance = instance;
            Stopping = false;
        }

        public int Slot { get; }

        public int EntryIndex { get; }

        public PrefabVfxInstance Instance { get; }

        public bool Stopping { get; set; }
    }
}
