// One look a pooled QuadVfx can play: the builder-made material asset it draws with (its shader and keywords, and the
// key of its pool; never changed at runtime), whether it plays at all, its base size in world units and duration in
// seconds, and Apply, which writes the look's values and the spawn context into an instance's own material copy just
// before it plays. Implemented by serializable looks embedded in profile assets, so values are read at spawn time and
// Play mode edits show on the next spawn.
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    public interface IQuadVfxLook
    {
        Material Material { get; }

        bool IsEnabled { get; }

        float Size { get; }

        float Duration { get; }

        void Apply(Material target, in VfxSpawnContext context);
    }
}
