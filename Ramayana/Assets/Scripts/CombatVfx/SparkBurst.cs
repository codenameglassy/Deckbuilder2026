// One reusable spark burst slot owned by SparkBurstSpawner: its own copy of a builder-made Spark Burst material
// (made once when the spawner builds its slots, destroyed with them), the burst's age on game time, its draw size and
// bounds. Play writes a look into the copy; Advance ages it; Render submits one procedural draw of six vertices per
// spark, with no mesh and no per-frame upload (the shader builds every spark from its index, the age and the seed).
// Plain C#, not a component, since a burst needs no transform.
using UnityEngine;
using UnityEngine.Rendering;

namespace JourneyToLanka.CombatVfx
{
    internal sealed class SparkBurst
    {
        private const int VerticesPerSpark = 6;

        private static readonly int AgeId = Shader.PropertyToID("_Age");

        private RenderParams _renderParams;
        private int _vertexCount;
        private float _age;
        private float _lifetime;

        public SparkBurst(Material sourceMaterial, int layer)
        {
            SourceMaterial = sourceMaterial;
            Material = new Material(sourceMaterial);
            _renderParams = new RenderParams(Material)
            {
                layer = layer,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                motionVectorMode = MotionVectorGenerationMode.ForceNoMotion,
            };
        }

        public Material SourceMaterial { get; }

        public Material Material { get; }

        public bool IsActive { get; private set; }

        public int PlayOrder { get; private set; }

        public void Play(SparkBurstLook look, Vector3 origin, int count, in VfxSpawnContext context, int playOrder)
        {
            look.Apply(Material, origin, count, context);
            _renderParams.worldBounds = look.GetBounds(origin);
            _vertexCount = count * VerticesPerSpark;
            _lifetime = Mathf.Max(0.01f, look.Lifetime);
            _age = 0f;
            PlayOrder = playOrder;
            IsActive = true;
        }

        public bool Advance(float deltaTime)
        {
            _age += deltaTime;
            Material.SetFloat(AgeId, _age);
            if (_age >= _lifetime)
            {
                Stop();
                return true;
            }

            return false;
        }

        public void Render()
        {
            if (IsActive && _vertexCount > 0)
            {
                Graphics.RenderPrimitives(_renderParams, MeshTopology.Triangles, _vertexCount);
            }
        }

        public void Stop()
        {
            IsActive = false;
            PlayOrder = 0;
        }

        public void Dispose()
        {
            Stop();
            if (Material != null)
            {
                Object.Destroy(Material);
            }
        }
    }
}
