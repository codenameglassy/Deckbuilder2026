// Impact burst for combat hits: a hot core flash, an expanding shockwave ring and a star of rays of uneven length,
// torn by value noise, all evaluated per pixel with no textures. Drawn on the shared VFX quad (unit square centred on
// its pivot); the vertex shader turns the quad to face the camera, rolls it by the per-spawn seed, scales it by the
// object's uniform scale and pushes it toward the camera so it sits in front of the fighter it hits. Fades softly
// where it meets scene geometry (scene depth texture), and every term reaches exactly zero before the quad's edge.
// Progress (0 to 1) is driven from C# on game time, so a burst holds still during hit-stop.
// Colour families are a local shader feature set by the material asset the builder makes for each (never at
// runtime): Radiant (no keyword) is the base burst; Fire (_FAMILY_FIRE) adds rising flame tongues and burns its rays
// down; Shadow (_FAMILY_SHADOW) adds a smoky core that darkens what is behind it (Shadow Opacity) under an ember ring.
// Premultiplied blending: light is added with zero alpha, and only the shadow smoke writes alpha to darken.
// Three variants, compiled only for families a built material uses. Every value comes from ImpactBurstLook at spawn;
// the material asset only carries the shader and its family. SRP Batcher compatible: all properties in
// UnityPerMaterial, no per-renderer property blocks.
Shader "Journey to Lanka/VFX/Impact Burst"
{
    Properties
    {
        [KeywordEnum(Radiant, Fire, Shadow)] _Family ("Family", Float) = 0
        [HDR] _Color ("Color", Color) = (3.2, 1.5, 0.45, 1)
        [HDR] _CoreColor ("Core Color", Color) = (5, 4.2, 3.2, 1)
        _Intensity ("Intensity", Float) = 1
        _Progress ("Progress", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0

        _CoreSize ("Core Size", Range(0.01, 1)) = 0.3
        _CoreFade ("Core Fade", Float) = 3

        _RingStart ("Ring Start", Range(0, 1)) = 0.12
        _RingEnd ("Ring End", Range(0, 1)) = 0.85
        _RingWidth ("Ring Width", Range(0.005, 0.5)) = 0.07
        _RingFade ("Ring Fade", Float) = 1.4

        _RayCount ("Ray Count", Range(0, 16)) = 7
        _RaySharpness ("Ray Sharpness", Range(1, 64)) = 18
        _RayLength ("Ray Length", Range(0.05, 1)) = 0.95
        _RayFade ("Ray Fade", Float) = 2

        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.35
        _NoiseScale ("Noise Scale", Float) = 5

        _CameraPush ("Camera Push", Float) = 0.35
        _SoftFade ("Soft Fade Distance", Float) = 0.25
        _ShadowOpacity ("Shadow Opacity", Range(0, 1)) = 0.6
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
            Name "ImpactBurst"

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma shader_feature_local _ _FAMILY_FIRE _FAMILY_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                float _Intensity;
                float _Progress;
                float _Seed;
                float _CoreSize;
                float _CoreFade;
                float _RingStart;
                float _RingEnd;
                float _RingWidth;
                float _RingFade;
                float _RayCount;
                float _RaySharpness;
                float _RayLength;
                float _RayFade;
                float _NoiseStrength;
                float _NoiseScale;
                float _CameraPush;
                float _SoftFade;
                float _ShadowOpacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                // Quad position in [-1, 1] on both axes; length is the distance from the centre in quad radii.
                float2 local : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
            };

            float ImpactHash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float ImpactHash21(float2 p)
            {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float ImpactValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = ImpactHash21(cell);
                float b = ImpactHash21(cell + float2(1.0, 0.0));
                float c = ImpactHash21(cell + float2(0.0, 1.0));
                float d = ImpactHash21(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float scale = length(float3(objectToWorld[0][0], objectToWorld[1][0], objectToWorld[2][0]));

                float3 cameraRight = UNITY_MATRIX_V[0].xyz;
                float3 cameraUp = UNITY_MATRIX_V[1].xyz;

                float sinRoll;
                float cosRoll;
                sincos(_Seed * TWO_PI, sinRoll, cosRoll);
                float2 corner = input.positionOS.xy;
                float2 rolled = float2(corner.x * cosRoll - corner.y * sinRoll, corner.x * sinRoll + corner.y * cosRoll);

                float3 toCamera = GetCameraPositionWS() - centerWS;
                float cameraDistance = length(toCamera);
                float push = min(_CameraPush, cameraDistance * 0.5);
                float3 pushedCenter = centerWS + toCamera * (push / max(cameraDistance, 1e-4));

                float3 positionWS = pushedCenter + (cameraRight * rolled.x + cameraUp * rolled.y) * scale;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.local = input.uv * 2.0 - 1.0;
                output.eyeDepth = -TransformWorldToView(positionWS).z;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.local;
                float r = length(p);
                float edge = 1.0 - smoothstep(0.8, 1.0, r);

                float t = saturate(_Progress);
                float remain = 1.0 - t;
                float grow = 1.0 - remain * remain * remain;

                float noise = ImpactValueNoise(p * _NoiseScale + _Seed * 37.0 + float2(0.0, -t * 1.5));
                float breakup = saturate(1.0 + (noise * 2.0 - 1.0) * _NoiseStrength);

                float coreRadius = max(_CoreSize * (0.6 + 0.4 * grow), 1e-3);
                float core = exp(-(r * r) / (coreRadius * coreRadius)) * PositivePow(remain, _CoreFade);

                float ringRadius = lerp(_RingStart, _RingEnd, grow);
                float ringWidth = max(_RingWidth * (0.5 + remain), 1e-3);
                float ringOffset = (r - ringRadius) / ringWidth;
                float ring = exp(-ringOffset * ringOffset) * PositivePow(remain, _RingFade) * breakup;

                float rays = 0.0;
                if (_RayCount >= 1.0)
                {
                    float angle = atan2(p.y, p.x);
                    float spoke = PositivePow(cos(angle * _RayCount * 0.5), _RaySharpness);
                    // Index of the nearest ray, wrapped so the rays either side of the atan2 seam share one length.
                    float ray = round(angle * _RayCount / TWO_PI);
                    ray -= _RayCount * floor(ray / _RayCount);
                    float reach = _RayLength * lerp(0.55, 1.0, ImpactHash11(ray + _Seed * 91.0)) * lerp(0.5, 1.0, grow);
                    float along = saturate(1.0 - r / max(reach, 1e-3));
                    rays = spoke * along * along * PositivePow(remain, _RayFade) * breakup;
                }

                float3 color = _Color.rgb * (ring + rays) + _CoreColor.rgb * core;
                float alpha = 0.0;

                #if defined(_FAMILY_FIRE)
                    // Flame tongues: noise stretched upward and scrolling up, strongest above the centre, so the burst
                    // reads as fire catching rather than a clean star. The rays burn down to embers.
                    float flameNoise = ImpactValueNoise(float2(p.x * _NoiseScale * 1.4, p.y * _NoiseScale * 0.55 - t * 5.0) + _Seed * 13.0);
                    float rise = smoothstep(-0.7, 0.35, p.y + 0.15 * grow);
                    float tongue = saturate(flameNoise * 1.7 - r * 1.3 + 0.15) * rise * PositivePow(remain, 1.2);
                    color = _Color.rgb * (ring + rays * 0.5 + tongue * 1.6) + _CoreColor.rgb * core;
                #elif defined(_FAMILY_SHADOW)
                    // Smoke core: dark, torn and swelling outward, under an ember ring in the burst's colour.
                    float smokeNoise = ImpactValueNoise(p * _NoiseScale * 0.8 - float2(0.0, t * 2.0) + _Seed * 7.0);
                    float smokeRadius = 0.3 + 0.45 * grow;
                    float smoke = saturate(1.0 - r / smokeRadius) * saturate(smokeNoise * 1.5 - 0.1) * PositivePow(remain, 0.8);
                    alpha = smoke * _ShadowOpacity;
                    color = _Color.rgb * (ring + rays) * (1.0 - alpha) + _CoreColor.rgb * core * 0.5;
                #endif

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float soft = saturate((sceneEyeDepth - input.eyeDepth) / max(_SoftFade, 1e-4));
                float fade = edge * soft;

                return half4(color * (_Intensity * fade), saturate(alpha * fade));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
