// Spark burst for combat hits: glowing streaks flung from the hit point, drawn procedurally with no mesh. Each spark is
// six vertices (two triangles) built from SV_VertexID: its index and the burst seed give it a lifetime, a launch
// direction inside a cone around the blow's direction (_Direction, world space; w is 0 when there is none, and the
// sparks then fly in every direction around up), and a speed. Its position and velocity at the burst's age are the
// closed-form solution of motion under linear drag and gravity, so nothing is simulated or uploaded per frame. Each
// spark is a view-space quad stretched along its on-screen velocity, thinning as it ages, coloured from Hot Color to
// Color and fading by Fade Power. Sparks past their life, or past _Count, are moved outside the clip volume.
// Age is driven from C# on game time, so sparks hold still during hit-stop. Premultiplied blending with zero alpha,
// so they add light. No keywords: exactly one variant. Every value comes from SparkBurstLook at spawn; the material
// asset only carries the shader. Drawn with Graphics.RenderPrimitives, one draw per burst.
Shader "Journey to Lanka/VFX/Spark Burst"
{
    Properties
    {
        [HideInInspector] _Origin ("Origin", Vector) = (0, 0, 0, 1)
        [HideInInspector] _Direction ("Direction", Vector) = (0, 0, 0, 0)
        [HideInInspector] _Age ("Age", Float) = 0
        [HideInInspector] _Seed ("Seed", Float) = 0
        [HideInInspector] _Count ("Count", Float) = 0

        [HDR] _HotColor ("Hot Color", Color) = (6, 5, 3.5, 1)
        [HDR] _Color ("Color", Color) = (3.5, 1.4, 0.35, 1)
        _Intensity ("Intensity", Float) = 1

        _Lifetime ("Lifetime", Float) = 0.5
        _LifetimeJitter ("Lifetime Jitter", Range(0, 0.95)) = 0.5
        _SpeedMin ("Speed Min", Float) = 3
        _SpeedMax ("Speed Max", Float) = 8
        _Drag ("Drag", Float) = 3.5
        _Gravity ("Gravity", Float) = 9
        _Spread ("Spread (radians)", Float) = 1.13
        _UpBias ("Up Bias", Float) = 0.35

        _SparkSize ("Spark Size", Float) = 0.05
        _Stretch ("Stretch", Float) = 0.035
        _FadePower ("Fade Power", Float) = 1.4
        _CameraPush ("Camera Push", Float) = 0.4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "SparkBurst"

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define VERTICES_PER_SPARK 6u
            #define RANDOMS_PER_SPARK 5u

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

            // Corners of a spark's two triangles: x runs along its velocity, y across it.
            static const float2 kSparkCorners[6] =
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
                // x: life used, 0 at launch and 1 at death. y: per-spark brightness.
                float2 life : TEXCOORD1;
            };

            float SparkHash(uint n)
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
                uint spark = vertexID / VERTICES_PER_SPARK;
                uint cornerIndex = vertexID - spark * VERTICES_PER_SPARK;
                if (spark >= (uint)_Count)
                {
                    return Hidden();
                }

                uint randomBase = spark * RANDOMS_PER_SPARK + (uint)(_Seed * 4096.0) * 7919u;
                float lifeRandom = SparkHash(randomBase);
                float coneRandom = SparkHash(randomBase + 1u);
                float spinRandom = SparkHash(randomBase + 2u);
                float speedRandom = SparkHash(randomBase + 3u);
                float brightRandom = SparkHash(randomBase + 4u);

                float life = max(_Lifetime * (1.0 - _LifetimeJitter * lifeRandom), 1e-3);
                float age = max(_Age, 0.0);
                float lifeUsed = age / life;
                if (lifeUsed >= 1.0)
                {
                    return Hidden();
                }

                // Launch direction: uniform over a cone around the blow's direction leaned upward, or around up.
                float3 up = float3(0.0, 1.0, 0.0);
                bool hasDirection = _Direction.w > 0.5;
                float3 axis = hasDirection ? normalize(_Direction.xyz + up * _UpBias) : up;
                float spread = hasDirection ? _Spread : PI;
                float cosTheta = lerp(1.0, cos(spread), coneRandom);
                float sinTheta = sqrt(saturate(1.0 - cosTheta * cosTheta));
                float phi = spinRandom * TWO_PI;
                float3 helper = abs(axis.y) < 0.99 ? up : float3(1.0, 0.0, 0.0);
                float3 tangent = normalize(cross(helper, axis));
                float3 bitangent = cross(axis, tangent);
                float3 launch = axis * cosTheta + (tangent * cos(phi) + bitangent * sin(phi)) * sinTheta;
                float3 launchVelocity = launch * lerp(_SpeedMin, _SpeedMax, speedRandom);

                float3 origin = _Origin.xyz;
                float3 toCamera = GetCameraPositionWS() - origin;
                float cameraDistance = length(toCamera);
                origin += toCamera * (min(_CameraPush, cameraDistance * 0.5) / max(cameraDistance, 1e-4));

                // Linear drag k and gravity g: v(t) = g/k + (v0 - g/k) e^(-kt), x(t) = x0 + (g/k) t + (v0 - g/k)(1 - e^(-kt))/k.
                float drag = max(_Drag, 1e-3);
                float3 terminal = float3(0.0, -_Gravity, 0.0) / drag;
                float decay = exp(-drag * age);
                float3 positionWS = origin + terminal * age + (launchVelocity - terminal) * ((1.0 - decay) / drag);
                float3 velocityWS = terminal + (launchVelocity - terminal) * decay;

                float3 positionVS = TransformWorldToView(positionWS);
                float3 velocityVS = mul((float3x3)UNITY_MATRIX_V, velocityWS);
                float screenSpeed = length(velocityVS.xy);
                float2 along = screenSpeed > 1e-4 ? velocityVS.xy / screenSpeed : float2(1.0, 0.0);
                float2 across = float2(-along.y, along.x);

                float width = _SparkSize * (1.0 - 0.5 * lifeUsed);
                float streak = width + _Stretch * length(velocityWS);
                float2 corner = kSparkCorners[cornerIndex];
                positionVS.xy += along * (corner.x * streak * 0.5) + across * (corner.y * width * 0.5);

                Varyings output;
                output.positionCS = TransformWViewToHClip(positionVS);
                output.local = corner;
                output.life = float2(lifeUsed, lerp(0.6, 1.0, brightRandom));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float acrossFalloff = exp(-input.local.y * input.local.y * 3.0);
                float alongFalloff = saturate(1.0 - input.local.x * input.local.x);
                float lifeUsed = input.life.x;
                float3 color = lerp(_HotColor.rgb, _Color.rgb, saturate(lifeUsed * 1.6));
                float fade = PositivePow(1.0 - lifeUsed, _FadePower);
                return half4(color * (acrossFalloff * alongFalloff * fade * input.life.y * _Intensity), 0.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
