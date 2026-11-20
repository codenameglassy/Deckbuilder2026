// Keeps a world-space UI root (a health bar, an enemy intent) the same size on screen wherever its fighter stands.
// Sizes are given in reference pixels per local unit at a 1080-pixel-tall screen, matching the HUD's canvas scaler,
// so a bar 120 units wide at 1.4 pixels per unit covers 168 reference pixels on any screen height. For a perspective
// camera the world size of one reference pixel grows with the root's depth along the camera's view; for an
// orthographic camera it is fixed. The owner calls Apply once a frame (from LateTick, after motion); it writes the
// scale only when it changes, as a uniform world scale under an unevenly scaled parent. Reset forces the next Apply
// to write, after a style change set a scale of its own.
using UnityEngine;

namespace JourneyToLanka.CombatScene
{
    internal sealed class ScreenScaleKeeper
    {
        public const float ReferenceHeight = 1080f;
        private const float Tolerance = 0.0005f;

        private readonly Transform _target;
        private float _lastWorldScale = -1f;

        public ScreenScaleKeeper(Transform target)
        {
            _target = target;
        }

        public void Apply(Camera camera, float pixelsPerUnit)
        {
            if (_target == null || camera == null || pixelsPerUnit <= 0f)
            {
                return;
            }

            float worldScale = WorldPerReferencePixel(camera, _target.position) * pixelsPerUnit;
            if (_lastWorldScale > 0f && Mathf.Abs(worldScale - _lastWorldScale) <= _lastWorldScale * Tolerance)
            {
                return;
            }

            _lastWorldScale = worldScale;
            _target.localScale = CombatantHealthBar.UniformWorldScale(_target.parent, worldScale);
        }

        public void Reset()
        {
            _lastWorldScale = -1f;
        }

        public static float WorldPerReferencePixel(Camera camera, Vector3 position)
        {
            if (camera.orthographic)
            {
                return camera.orthographicSize * 2f / ReferenceHeight;
            }

            Transform view = camera.transform;
            float depth = Mathf.Max(Vector3.Dot(position - view.position, view.forward), camera.nearClipPlane);
            return 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / ReferenceHeight;
        }
    }
}
