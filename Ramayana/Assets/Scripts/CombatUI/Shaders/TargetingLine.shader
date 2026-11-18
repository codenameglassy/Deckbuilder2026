// Targeting line for card-to-enemy aiming: a saffron-gold energy ribbon with a bright core, a soft glow, chevrons
// flowing toward the target, a taper from the card to the target and an anti-aliased arrowhead.
// GPU-driven: the mesh is a fixed strip plus one arrowhead quad, built once by TargetingLineGraphic. The vertex shader
// bends it along a cubic Bezier from four control points (_P01, _P23) set by TargetingLinePresenter, so moving the
// line never rebuilds the mesh or the canvas. Shapes are distance fields evaluated per pixel with screen-space
// anti-aliasing, so it is crisp at any resolution and uses no textures. No keywords, so there is exactly one variant.
// Premultiplied alpha: the body is solid, the glow is additive (zero alpha). Every value is driven from
// TargetingLineStyleSO at runtime; the material asset only carries the shader. SRP Batcher compatible.
// Mesh encoding (uv0): ribbon vertices x = t along the curve in [0, 1], y = side (-1 or 1);
//                      arrowhead vertices x = HEAD_CODE_OFFSET + position along the head in head lengths (from
//                      -HEAD_MARGIN behind the base to 1 + HEAD_MARGIN beyond the tip), y = side (-1 or 1).
//                      A vertex is part of the head when x is above HEAD_CODE_THRESHOLD, which sits between the
//                      ribbon's largest code (1) and the head's smallest (HEAD_CODE_OFFSET - HEAD_MARGIN).
Shader "Journey to Lanka/UI/Targeting Line"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _P01 ("Control Points 0 and 1", Vector) = (0, 0, 0, 0)
        [HideInInspector] _P23 ("Control Points 2 and 3", Vector) = (0, 0, 0, 0)

        _ColorStart ("Body Start", Color) = (1, 0.55, 0.15, 0.95)
        _ColorEnd ("Body End", Color) = (1, 0.8, 0.35, 1)
        _CoreColor ("Core", Color) = (1, 0.97, 0.86, 1)
        _GlowColor ("Glow", Color) = (1, 0.5, 0.1, 0.55)
        _ChevronColor ("Chevrons", Color) = (1, 1, 0.9, 1)

        _WidthStart ("Width At Card", Float) = 8
        _WidthEnd ("Width At Target", Float) = 20
        _GlowScale ("Glow Scale", Float) = 3
        _CoreWidth ("Core Width", Float) = 0.35
        _GlowFalloff ("Glow Falloff", Float) = 2.2
        _HeadLength ("Head Length", Float) = 60
        _HeadWidth ("Head Width", Float) = 56

        _ChevronSpacing ("Chevron Spacing", Float) = 34
        _ChevronDuty ("Chevron Duty", Float) = 0.35
        _ChevronSlant ("Chevron Slant", Float) = 0.9
        _FlowSpeed ("Flow Speed", Float) = 1.6
        _ChevronIntensity ("Chevron Intensity", Float) = 0.8
        _ShimmerAmount ("Shimmer Amount", Float) = 0.12
        _StartFade ("Start Fade", Float) = 0.08

        _Length ("Length", Float) = 100
        _Reveal ("Reveal", Float) = 1
        _Pulse ("Pulse", Float) = 0
        _PulseWiden ("Pulse Widen", Float) = 0.35
        _PulseBrighten ("Pulse Brighten", Float) = 0.6
        _Intensity ("Intensity", Float) = 1
        _Alpha ("Alpha", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "TargetingLine"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Must match TargetingLineGraphic. The arrowhead quad extends HEAD_MARGIN head lengths behind the base and
            // beyond the tip, which bounds how far the head's glow may reach.
            #define HEAD_CODE_OFFSET 4.0
            #define HEAD_CODE_THRESHOLD 2.0
            #define HEAD_MARGIN 0.6

            CBUFFER_START(UnityPerMaterial)
                float4 _P01;
                float4 _P23;
                half4 _ColorStart;
                half4 _ColorEnd;
                half4 _CoreColor;
                half4 _GlowColor;
                half4 _ChevronColor;
                float _WidthStart;
                float _WidthEnd;
                float _GlowScale;
                float _CoreWidth;
                float _GlowFalloff;
                float _HeadLength;
                float _HeadWidth;
                float _ChevronSpacing;
                float _ChevronDuty;
                float _ChevronSlant;
                float _FlowSpeed;
                float _ChevronIntensity;
                float _ShimmerAmount;
                float _StartFade;
                float _Length;
                float _Reveal;
                float _Pulse;
                float _PulseWiden;
                float _PulseBrighten;
                float _Intensity;
                float _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                // x: t along the curve (1 on the head), y: signed distance across in canvas units,
                // z: half width of the solid body (ribbon) or of the head base, w: 1 on the head, 0 on the ribbon.
                float4 shape : TEXCOORD0;
                // Distance along: arc length on the ribbon, distance from the head base on the head.
                float along : TEXCOORD1;
            };

            float2 SafeNormalize2(float2 v)
            {
                return v * rsqrt(max(dot(v, v), 1e-8));
            }

            float2 BezierPoint(float t, float2 p0, float2 p1, float2 p2, float2 p3)
            {
                float s = 1.0 - t;
                return s * s * s * p0 + 3.0 * s * s * t * p1 + 3.0 * s * t * t * p2 + t * t * t * p3;
            }

            float2 BezierTangent(float t, float2 p0, float2 p1, float2 p2, float2 p3)
            {
                float s = 1.0 - t;
                return 3.0 * s * s * (p1 - p0) + 6.0 * s * t * (p2 - p1) + 3.0 * t * t * (p3 - p2);
            }

            // Gaussian glow that also fades to exactly zero at its radius, so it never shows the quad's edge.
            half Glow(float distance, float radius)
            {
                float g = max(distance, 0.0) / radius;
                return exp(-g * g * _GlowFalloff) * (1.0 - smoothstep(0.65, 1.0, g));
            }

            // Isosceles triangle distance (Inigo Quilez): tip at the origin, base at y = q.y, half base width q.x.
            float TriangleDistance(float2 p, float2 q)
            {
                p.x = abs(p.x);
                float2 a = p - q * saturate(dot(p, q) / dot(q, q));
                float2 b = p - q * float2(saturate(p.x / q.x), 1.0);
                float s = -sign(q.y);
                float2 d = min(float2(dot(a, a), s * (p.x * q.y - p.y * q.x)),
                               float2(dot(b, b), s * (p.y - q.y)));
                return -sqrt(d.x) * sign(d.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 p0 = _P01.xy;
                float2 p1 = _P01.zw;
                float2 p2 = _P23.xy;
                float2 p3 = _P23.zw;
                float pulseScale = 1.0 + _Pulse * _PulseWiden;
                float2 position;

                if (input.uv0.x < HEAD_CODE_THRESHOLD)
                {
                    float t = saturate(input.uv0.x);
                    float2 tangent = SafeNormalize2(BezierTangent(t, p0, p1, p2, p3));
                    float2 normal = float2(-tangent.y, tangent.x);
                    float halfWidth = 0.5 * lerp(_WidthStart, _WidthEnd, t) * pulseScale;
                    float across = input.uv0.y * halfWidth * _GlowScale;
                    position = BezierPoint(t, p0, p1, p2, p3) + normal * across;
                    output.shape = float4(t, across, halfWidth, 0.0);
                    output.along = t * _Length;
                }
                else
                {
                    float2 tangent = SafeNormalize2(BezierTangent(1.0, p0, p1, p2, p3));
                    float2 normal = float2(-tangent.y, tangent.x);
                    float halfWidth = 0.5 * _HeadWidth * pulseScale;
                    float along = (input.uv0.x - HEAD_CODE_OFFSET) * _HeadLength;
                    float across = input.uv0.y * halfWidth * _GlowScale;
                    position = p3 + tangent * along + normal * across;
                    output.shape = float4(1.0, across, halfWidth, 1.0);
                    output.along = along;
                }

                output.positionCS = TransformObjectToHClip(float3(position, input.positionOS.z));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Screen-space derivatives are taken before the branch so control flow stays uniform for them.
                float signedAcross = input.shape.y;
                float acrossAA = max(fwidth(signedAcross), 1e-3);
                float arcPhase = (input.along + abs(signedAcross) * _ChevronSlant) / max(_ChevronSpacing, 1e-3) - _Time.y * _FlowSpeed;
                float phaseAA = max(fwidth(arcPhase), 1e-4);

                float t = input.shape.x;
                float across = abs(signedAcross);
                float halfWidth = max(input.shape.z, 1e-3);
                float glowRadius = max(halfWidth * (_GlowScale - 1.0), 1e-3);
                float headGlowRadius = max(min(glowRadius, _HeadLength * HEAD_MARGIN), 1e-3);

                half3 colour;
                half coverage;

                if (input.shape.w < 0.5)
                {
                    float bodyDistance = across - halfWidth;
                    half fill = 1.0 - smoothstep(-acrossAA, acrossAA, bodyDistance);

                    float coreRadius = max(halfWidth * _CoreWidth, 1e-3);
                    half core = exp(-(across * across) / (coreRadius * coreRadius));

                    half glow = Glow(bodyDistance, glowRadius) * (1.0 - fill);

                    float cycle = frac(arcPhase);
                    half chevron = smoothstep(0.0, phaseAA * 1.5, cycle) * (1.0 - smoothstep(_ChevronDuty - phaseAA * 1.5, _ChevronDuty, cycle));
                    chevron *= fill * smoothstep(0.0, 0.25, t);

                    half shimmer = 1.0 + _ShimmerAmount * sin(input.along * 0.045 - _Time.y * 5.0) * sin(input.along * 0.011 + _Time.y * 1.7);

                    half3 body = lerp(_ColorStart.rgb, _ColorEnd.rgb, t) * lerp(_ColorStart.a, _ColorEnd.a, t);
                    colour = body * fill * shimmer
                        + _CoreColor.rgb * (core * fill)
                        + _ChevronColor.rgb * (chevron * _ChevronIntensity)
                        + _GlowColor.rgb * (glow * _GlowColor.a);
                    coverage = fill;

                    float revealEdge = _Reveal * 1.03;
                    half fade = smoothstep(0.0, max(_StartFade, 1e-3), t) * (1.0 - smoothstep(revealEdge - 0.03, revealEdge, t));
                    colour *= fade;
                    coverage *= fade;
                }
                else
                {
                    float2 q = float2(halfWidth, max(_HeadLength, 1e-3));
                    float2 p = float2(across, _HeadLength - input.along);
                    float headDistance = TriangleDistance(p, q);
                    half fill = 1.0 - smoothstep(-acrossAA, acrossAA, headDistance);

                    half inner = smoothstep(0.0, halfWidth * 0.55, -headDistance);

                    half glow = Glow(headDistance, headGlowRadius) * (1.0 - fill);

                    colour = _ColorEnd.rgb * (fill * _ColorEnd.a)
                        + _CoreColor.rgb * (inner * fill * 0.8)
                        + _GlowColor.rgb * (glow * _GlowColor.a);
                    coverage = fill;

                    half fade = saturate((_Reveal - 0.92) / 0.08);
                    colour *= fade;
                    coverage *= fade;
                }

                half brightness = _Intensity * (1.0 + _Pulse * _PulseBrighten);
                return half4(colour * brightness * _Alpha, coverage * _Alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
