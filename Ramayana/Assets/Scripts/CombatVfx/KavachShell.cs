// One fighter's Kavach hull, owned by KavachShellPresenter and built once from the fighter's KavachShellTarget (fight
// setup, never mid-play): for each listed body renderer, a child object drawing that renderer's shell mesh with its
// own copy of the Kavach Shell material. A MeshRenderer body gets a MeshFilter and MeshRenderer child; a
// SkinnedMeshRenderer body gets a SkinnedMeshRenderer child sharing the body's bones, so the hull animates with it.
// Children sit at the body's own transform, so they follow lunges, shakes and pulses with no per-frame work.
// It tracks the Kavach it last saw to tell a gain (flash and swell) from a loss (ease down), and animates strength,
// flash, swell, the absorb ripple and the shatter dissolve on game time. Tick writes the changing values into every
// material and reports whether anything is still moving, so the presenter stops ticking a hull that is merely shown
// (the honeycomb keeps shimmering in the shader). Plain C#, not a component.
using System.Collections.Generic;
using JourneyToLanka.Combat.Model;
using UnityEngine;
using UnityEngine.Rendering;

namespace JourneyToLanka.CombatVfx
{
    internal sealed class KavachShell
    {
        private const float Settled = 0.001f;
        private const string LayerName = "Kavach Shell";

        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int SwellId = Shader.PropertyToID("_Swell");
        private static readonly int RippleOriginId = Shader.PropertyToID("_RippleOrigin");
        private static readonly int RippleProgressId = Shader.PropertyToID("_RippleProgress");
        private static readonly int RippleStrengthId = Shader.PropertyToID("_RippleStrength");
        private static readonly int BreakId = Shader.PropertyToID("_Break");

        private readonly List<GameObject> _layers = new List<GameObject>(2);
        private readonly List<Material> _materials = new List<Material>(2);
        private readonly List<Renderer> _bodies = new List<Renderer>(2);

        private bool _visible;
        private int _lastBlock;
        private float _strength;
        private float _target;
        private float _flash;
        private float _swell;
        private float _rippleTime = -1f;
        private float _rippleStrength;
        private Vector4 _rippleOrigin;
        private float _breakTime = -1f;

        public KavachShell(KavachShellTarget target, Material sourceMaterial, int layer)
        {
            Target = target;
            IReadOnlyList<KavachShellTargetEntry> entries = target.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                KavachShellTargetEntry entry = entries[i];
                if (entry.IsValid)
                {
                    AddLayer(entry, sourceMaterial, layer);
                }
            }
        }

        public KavachShellTarget Target { get; }

        public Combatant Fighter { get; private set; }

        public bool HasLayers => _layers.Count > 0;

        public bool IsBreaking => _breakTime >= 0f;

        public void Bind(Combatant fighter, KavachVfxProfileSO profile)
        {
            Fighter = fighter;
            _lastBlock = fighter.Block;
            _target = profile.StrengthFor(_lastBlock);
            _strength = _target;
            _flash = 0f;
            _swell = 0f;
            _rippleTime = -1f;
            _rippleStrength = 0f;
            _breakTime = -1f;
            ApplyStatic(profile);
            WriteMaterials(profile);
        }

        public void Unbind()
        {
            Fighter = null;
            SetVisible(false);
        }

        public void ApplyStatic(KavachVfxProfileSO profile)
        {
            for (int i = 0; i < _materials.Count; i++)
            {
                profile.ApplyStatic(_materials[i]);
            }
        }

        // Called when the fighter's Kavach changes. A rise flashes and swells the hull; any change retargets its strength.
        public void Refresh(KavachVfxProfileSO profile)
        {
            if (Fighter == null)
            {
                return;
            }

            int block = Fighter.Block;
            if (block > _lastBlock)
            {
                _flash = Mathf.Max(_flash, profile.GainFlash);
                _swell = Mathf.Max(_swell, profile.GainSwell);
                _breakTime = -1f;
            }

            _lastBlock = block;
            _target = profile.StrengthFor(block);
        }

        // A ripple from the side the blow came from: it starts where a line from the body's centre toward the
        // attacker leaves the body's bounds, or on the side facing up when the direction is zero.
        public void Ripple(Vector3 towardAttacker, float strength)
        {
            if (!TryGetBodyBounds(out Bounds bounds))
            {
                return;
            }

            Vector3 direction = towardAttacker.sqrMagnitude > 1e-6f ? towardAttacker.normalized : Vector3.up;
            float reach = Mathf.Abs(direction.x) * bounds.extents.x
                + Mathf.Abs(direction.y) * bounds.extents.y
                + Mathf.Abs(direction.z) * bounds.extents.z;
            Vector3 origin = bounds.center + direction * reach;

            _rippleOrigin = new Vector4(origin.x, origin.y, origin.z, 1f);
            _rippleTime = 0f;
            _rippleStrength = strength;
        }

        public void Shatter()
        {
            _breakTime = 0f;
            _target = 0f;
        }

        public bool Tick(float deltaTime, KavachVfxProfileSO profile)
        {
            bool breaking = _breakTime >= 0f;
            if (breaking)
            {
                _breakTime += deltaTime;
                if (_breakTime >= profile.ShatterDuration)
                {
                    _breakTime = -1f;
                    _strength = 0f;
                    breaking = false;
                }
            }
            else
            {
                _strength = Mathf.MoveTowards(_strength, _target, profile.FadeSpeed * deltaTime);
            }

            _flash = Mathf.MoveTowards(_flash, 0f, profile.FlashDecay * deltaTime);
            _swell = Mathf.MoveTowards(_swell, 0f, profile.SwellDecay * deltaTime);

            bool rippling = _rippleTime >= 0f;
            if (rippling)
            {
                _rippleTime += deltaTime;
                if (_rippleTime >= profile.RippleDuration)
                {
                    _rippleTime = -1f;
                    rippling = false;
                }
            }

            WriteMaterials(profile);
            return breaking || rippling || _flash > Settled || _swell > Settled || Mathf.Abs(_strength - _target) > Settled;
        }

        public void Destroy()
        {
            for (int i = 0; i < _materials.Count; i++)
            {
                if (_materials[i] != null)
                {
                    Object.Destroy(_materials[i]);
                }
            }

            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] != null)
                {
                    Object.Destroy(_layers[i]);
                }
            }

            _materials.Clear();
            _layers.Clear();
            _bodies.Clear();
        }

        private void AddLayer(KavachShellTargetEntry entry, Material sourceMaterial, int layer)
        {
            var layerObject = new GameObject(LayerName) { layer = layer };
            layerObject.SetActive(false);
            Transform layerTransform = layerObject.transform;
            layerTransform.SetParent(entry.Source.transform, false);
            layerTransform.localPosition = Vector3.zero;
            layerTransform.localRotation = Quaternion.identity;
            layerTransform.localScale = Vector3.one;

            var material = new Material(sourceMaterial);
            Renderer shellRenderer;
            if (entry.Source is SkinnedMeshRenderer skinnedBody)
            {
                var skinned = layerObject.AddComponent<SkinnedMeshRenderer>();
                skinned.sharedMesh = entry.ShellMesh;
                skinned.rootBone = skinnedBody.rootBone;
                skinned.bones = skinnedBody.bones;
                // Padded so the pushed and swelling hull is never culled while its body is still on screen.
                Bounds localBounds = skinnedBody.localBounds;
                localBounds.Expand(1f);
                skinned.localBounds = localBounds;
                skinned.updateWhenOffscreen = skinnedBody.updateWhenOffscreen;
                shellRenderer = skinned;
            }
            else
            {
                var filter = layerObject.AddComponent<MeshFilter>();
                filter.sharedMesh = entry.ShellMesh;
                shellRenderer = layerObject.AddComponent<MeshRenderer>();
            }

            shellRenderer.sharedMaterial = material;
            shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shellRenderer.receiveShadows = false;
            shellRenderer.lightProbeUsage = LightProbeUsage.Off;
            shellRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            shellRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            _layers.Add(layerObject);
            _materials.Add(material);
            _bodies.Add(entry.Source);
        }

        private bool TryGetBodyBounds(out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            for (int i = 0; i < _bodies.Count; i++)
            {
                Renderer body = _bodies[i];
                if (body == null)
                {
                    continue;
                }

                if (found)
                {
                    bounds.Encapsulate(body.bounds);
                }
                else
                {
                    bounds = body.bounds;
                    found = true;
                }
            }

            return found;
        }

        private void WriteMaterials(KavachVfxProfileSO profile)
        {
            bool breaking = _breakTime >= 0f;
            bool rippling = _rippleTime >= 0f;
            bool visible = _strength > Settled || breaking || (rippling && _rippleStrength > 0f);
            SetVisible(visible);
            if (!visible)
            {
                return;
            }

            float rippleProgress = rippling ? Mathf.Clamp01(_rippleTime / profile.RippleDuration) : 1f;
            float rippleStrength = rippling ? _rippleStrength : 0f;
            float breakProgress = breaking ? Mathf.Clamp01(_breakTime / profile.ShatterDuration) : 0f;
            for (int i = 0; i < _materials.Count; i++)
            {
                Material material = _materials[i];
                material.SetFloat(StrengthId, _strength);
                material.SetFloat(FlashId, _flash);
                material.SetFloat(SwellId, _swell);
                material.SetVector(RippleOriginId, _rippleOrigin);
                material.SetFloat(RippleProgressId, rippleProgress);
                material.SetFloat(RippleStrengthId, rippleStrength);
                material.SetFloat(BreakId, breakProgress);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_visible == visible)
            {
                return;
            }

            _visible = visible;
            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] != null)
                {
                    _layers[i].SetActive(visible);
                }
            }
        }
    }
}
