// Cast mandala for combat actions: an outer and an inner ring, a ring of lotus petals between them and a ring of dots,
// over a soft glow, that blooms outward, turns slowly and fades, evaluated per pixel with no textures. Drawn on the
// shared VFX quad laid flat on the world XZ plane at the object's position and scaled by its uniform scale, so it
// lies on the ground under the one who acts. Fades softly where it meets scene geometry (scene depth texture).
// Progress (0 to 1) is driven from C# on game time: the pattern blooms over the first Bloom Portion, turns by Spin
// turns over the whole duration from a seeded start, and fades out by Fade Power. Premultiplied blending with zero
// alpha, so it adds light. No keywords: exactly one variant. Every value comes from MandalaRingLook at spawn; the
// material asset only carries the shader. SRP Batcher compatible: all properties in UnityPerMaterial.
Shader "Journey to Lanka/VFX/Mandala Ring"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (3, 1.8, 0.6, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.8, 0.4, 0.12, 1)
        _Intensity ("Intensity", Float) = 1
        _Progress ("Progress", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0

        _PetalCount ("Petal Count", Range(3, 16)) = 8
        _PetalDepth ("Petal Depth", Range(0.05, 0.5)) = 0.28
        _LineWidth ("Line Width", Range(0.005, 0.1)) = 0.025
        _BloomPortion ("Bloom Portion", Range(0.05, 1)) = 0.35
        _Spin ("Spin (turns)", Range(-1, 1)) = 0.15
        _FadePower ("Fade Power", Float) = 1.6

        _SoftFade ("Soft Fade Distance", Float) = 0.2
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
            Name "MandalaRing"

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Radii in pattern space, where 1 is the quad's edge once the bloom has finished.
            #define OUTER_RING 0.8
            #define INNER_RING 0.42
            #define DOT_RING 0.89

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _GlowColor;
                float _Intensity;
                float _Progress;
                float _Seed;
                float _PetalCount;
                float _PetalDepth;
                float _LineWidth;
                float _BloomPortion;
                float _Spin;
                float _FadePower;
                float _SoftFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                // Quad position in [-1, 1] on both axes, x along world X and y along world Z.
                float2 local : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
            };

            float MandalaLine(float value, float center, float width)
            {
                float offset = (value - center) / width;
                return exp(-offset * offset);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float scale = length(float3(objectToWorld[0][0], objectToWorld[1][0], objectToWorld[2][0]));

                float2 corner = input.positionOS.xy;
                float3 positionWS = centerWS + float3(corner.x, 0.0, corner.y) * scale;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.local = input.uv * 2.0 - 1.0;
                output.eyeDepth = -TransformWorldToView(positionWS).z;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.local;
                float r = length(p);
                float edge = 1.0 - smoothstep(0.92, 1.0, r);

                float t = saturate(_Progress);
                float bloom = saturate(t / max(_BloomPortion, 1e-3));
                float bloomEase = 1.0 - (1.0 - bloom) * (1.0 - bloom) * (1.0 - bloom);
                float patternScale = lerp(0.35, 1.0, bloomEase);
                float rn = r / patternScale;
                float lineWidth = max(_LineWidth / patternScale, 1e-4);

                float angle = atan2(p.y, p.x) + (_Seed + _Spin * t) * TWO_PI;
                float petalCount = max(round(_PetalCount), 3.0);

                float outerRing = MandalaLine(rn, OUTER_RING, lineWidth);
                float innerRing = MandalaLine(rn, INNER_RING, lineWidth);

                float petalCurve = INNER_RING + _PetalDepth * PositivePow(cos(angle * petalCount * 0.5), 0.6);
                float petals = MandalaLine(rn, petalCurve, lineWidth * 1.1);

                float dotSpacing = TWO_PI / (petalCount * 2.0);
                float dotAngle = (frac(angle / dotSpacing + 0.5) - 0.5) * dotSpacing * DOT_RING;
                float dotDistance = length(float2(dotAngle, rn - DOT_RING));
                float dots = MandalaLine(dotDistance, 0.0, lineWidth * 1.6);

                float glow = saturate(1.0 - rn / OUTER_RING) * 0.25;

                float fadeIn = saturate(t / 0.08);
                float fadeOut = PositivePow(1.0 - t, _FadePower);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float soft = saturate((sceneEyeDepth - input.eyeDepth) / max(_SoftFade, 1e-4));

                float pattern = outerRing + innerRing + petals + dots;
                float3 color = (_Color.rgb * pattern + _GlowColor.rgb * glow) * (fadeIn * fadeOut * edge * soft * _Intensity);
                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
