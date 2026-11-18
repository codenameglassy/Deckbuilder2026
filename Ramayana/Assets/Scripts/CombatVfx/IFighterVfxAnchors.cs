// Where an accent sits on a fighter right now, by slot (Rama is HitSource.PlayerSlot, enemies 0 and up): the look's
// height on the fighter's collider, brought out to the camera-facing surface when the look asks for it, plus its
// offset. Implemented by CombatVfxDirector, which owns the fighters' colliders and the camera, so presenters that keep
// effects on moving fighters place them exactly as one-shot accents are placed. False when the slot has no fighter.
using UnityEngine;

namespace JourneyToLanka.CombatVfx
{
    public interface IFighterVfxAnchors
    {
        bool TryPlace(int slot, PrefabVfxLook look, out Vector3 point);
    }
}
