// Kavach shatter shards: flat, irregular fragments of the shield thrown out when Kavach breaks, drawn procedurally
// with no mesh, one draw per burst. It shares the Spark Burst property contract (driven by a SparkBurstLook and the
// spark spawner), so launch, drag and gravity are the same closed-form motion; only the shape differs. Each shard is
// six vertices (two triangles) built from SV_VertexID: a quad whose corners are jittered per shard, spinning in the
// view plane at a rate set by its speed, and glinting as it turns, with brighter edges like cut glass. Spark Size is
// the shard's width; Stretch is unused. Shards past their life, or past _Count, are moved outside the clip volume.
// Age is driven from C# on game time, so shards hold still during hit-stop. Premultiplied blending with zero alpha.
// No keywords: exactly one variant.
Shader "Journey to Lanka/VFX/Kavach Shards"
{
    Properties
    {
        [HideInInspector] _Origin ("Origin", Vector) = (0, 0, 0, 1)
        [HideInInspector] _Direction ("Direction", Vector) = (0, 0, 0, 0)
        [HideInInspector] _Age ("Age", Float) = 0
        [HideInInspector] _Seed ("Seed", Float) = 0
        [HideInInspector] _Count ("Count", Float) = 0

        [HDR] _HotColor ("Hot Color", Color) = (2.5, 4.5, 5, 1)
        [HDR] _Color ("Color", Color) = (0.5, 1.8, 2.4, 1)
        _Intensity ("Intensity", Float) = 1

        _Lifetime ("Lifetime", Float) = 0.7
        _LifetimeJitter ("Lifetime Jitter", Range(0, 0.95)) = 0.4
        _SpeedMin ("Speed Min", Float) = 2
        _SpeedMax ("Speed Max", Float) = 5
        _Drag ("Drag", Float) = 2.5
        _Gravity ("Gravity", Float) = 7
        _Spread ("Spread (radians)", Float) = 3.14
        _UpBias ("Up Bias", Float) = 0.5

        _SparkSize ("Shard Size", Float) = 0.12
        _Stretch ("Stretch (unused)", Float) = 0
        _FadePower ("Fade Power", Float) = 1.2
        _CameraPush ("Camera Push", Float) = 0.3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "KavachShards"

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define VERTICES_PER_SHARD 6u
            #define RANDOMS_PER_SHARD 12u

            CBUFFER_START(UnityPerMaterial)
                float4 _Origin;
                float4 _Direction;
                float _Age;
                float _Seed;
                float _Count;
                half4 _HotColor;
                half4 _Color;
                float _Intensity;
                float _Lifetime;
                float _LifetimeJitter;
                float _SpeedMin;
                float _SpeedMax;
                float _Drag;
                float _Gravity;
                float _Spread;
                float _UpBias;
                float _SparkSize;
                float _Stretch;
                float _FadePower;
                float _CameraPush;
            CBUFFER_END

            static const float2 kShardCorners[6] =
            {
                float2(-1.0, -1.0),
                float2(1.0, -1.0),
                float2(1.0, 1.0),
                float2(-1.0, -1.0),
                float2(1.0, 1.0),
                float2(-1.0, 1.0)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 local : TEXCOORD0;
                // x: life used, 0 at launch and 1 at death. y: glint, how much light the shard catches now.
                float2 life : TEXCOORD1;
            };

            float ShardHash(uint n)
            {
                n = (n << 13u) ^ n;
                n = n * (n * n * 15731u + 789221u) + 1376312589u;
                return float(n & 0x7fffffffu) / 2147483647.0;
            }

            Varyings Hidden()
            {
                Varyings output;
                output.positionCS = float4(2.0, 2.0, 2.0, 1.0);
                output.local = float2(0.0, 0.0);
                output.life = float2(1.0, 0.0);
                return output;
            }

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                uint shard = vertexID / VERTICES_PER_SHARD;
                uint cornerIndex = vertexID - shard * VERTICES_PER_SHARD;
                if (shard >= (uint)_Count)
                {
                    return Hidden();
                }

                uint randomBase = shard * RANDOMS_PER_SHARD + (uint)(_Seed * 4096.0) * 7919u;
                float lifeRandom = ShardHash(randomBase);
                float coneRandom = ShardHash(randomBase + 1u);
                float aroundRandom = ShardHash(randomBase + 2u);
                float speedRandom = ShardHash(randomBase + 3u);
                float sizeRandom = ShardHash(randomBase + 4u);
                float phaseRandom = ShardHash(randomBase + 5u);
                float spinSignRandom = ShardHash(randomBase + 6u);

                float life = max(_Lifetime * (1.0 - _LifetimeJitter * lifeRandom), 1e-3);
                float age = max(_Age, 0.0);
                float lifeUsed = age / life;
                if (lifeUsed >= 1.0)
                {
                    return Hidden();
                }

                float3 up = float3(0.0, 1.0, 0.0);
                bool hasDirection = _Direction.w > 0.5;
                float3 axis = hasDirection ? normalize(_Direction.xyz + up * _UpBias) : up;
                float spread = hasDirection ? _Spread : PI;
                float cosTheta = lerp(1.0, cos(spread), coneRandom);
                float sinTheta = sqrt(saturate(1.0 - cosTheta * cosTheta));
                float phi = aroundRandom * TWO_PI;
                float3 helper = abs(axis.y) < 0.99 ? up : float3(1.0, 0.0, 0.0);
                float3 tangent = normalize(cross(helper, axis));
                float3 bitangent = cross(axis, tangent);
                float3 launch = axis * cosTheta + (tangent * cos(phi) + bitangent * sin(phi)) * sinTheta;
                float speed = lerp(_SpeedMin, _SpeedMax, speedRandom);
                float3 launchVelocity = launch * speed;

                float3 origin = _Origin.xyz;
                float3 toCamera = GetCameraPositionWS() - origin;
                float cameraDistance = length(toCamera);
                origin += toCamera * (min(_CameraPush, cameraDistance * 0.5) / max(cameraDistance, 1e-4));

                // Linear drag k and gravity g: x(t) = x0 + (g/k) t + (v0 - g/k)(1 - e^(-kt))/k.
                float drag = max(_Drag, 1e-3);
                float3 terminal = float3(0.0, -_Gravity, 0.0) / drag;
                float decay = exp(-drag * age);
                float3 positionWS = origin + terminal * age + (launchVelocity - terminal) * ((1.0 - decay) / drag);

                // Each corner is pushed in or out by its own random amount, so every shard has its own outline.
                float2 corner = kShardCorners[cornerIndex];
                uint cornerKey = (corner.x > 0.0 ? 1u : 0u) + (corner.y > 0.0 ? 2u : 0u);
                float jitter = lerp(0.45, 1.15, ShardHash(randomBase + 7u + cornerKey));
                float2 shape = corner * jitter;

                // Spin in the view plane, faster for faster shards, slowing as drag bleeds speed off.
                float spinDirection = spinSignRandom < 0.5 ? -1.0 : 1.0;
                float spin = phaseRandom * TWO_PI + spinDirection * speed * 2.5 * ((1.0 - decay) / drag);
                float sinSpin;
                float cosSpin;
                sincos(spin, sinSpin, cosSpin);
                float2 turned = float2(shape.x * cosSpin - shape.y * sinSpin, shape.x * sinSpin + shape.y * cosSpin);

                float size = _SparkSize * lerp(0.6, 1.4, sizeRandom) * (1.0 - 0.3 * lifeUsed) * 0.5;
                float3 positionVS = TransformWorldToView(positionWS);
                positionVS.xy += turned * size;

                Varyings output;
                output.positionCS = TransformWViewToHClip(positionVS);
                output.local = corner;
                output.life = float2(lifeUsed, 0.5 + 0.5 * cos(spin * 2.0));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 local = abs(input.local);
                float edge = smoothstep(0.6, 1.0, max(local.x, local.y));
                float lifeUsed = input.life.x;
                float glint = input.life.y;
                float3 color = lerp(_HotColor.rgb, _Color.rgb, saturate(lifeUsed * 1.4));
                float fade = PositivePow(1.0 - lifeUsed, _FadePower);
                float light = (0.35 + 0.65 * glint) * (0.4 + 0.9 * edge);
                return half4(color * (light * fade * _Intensity), 0.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
