// Card back for the draw and discard stacks and for face-down card flights: a rounded card in the back colour with a
// darker border, a thin gold trim and a second rule inside it (the same proportions as the Card Frame, so a back and a
// face line up), a faint diagonal lattice across the field, and a Mithila-style emblem in the centre (a ring around a
// dot inside a rhombus outline) drawn in the trim colour. The body is lit slightly from the top. Every shape is a
// distance field measured in screen pixels from the uv derivatives, so it stays crisp at any size, scale or rotation and
// needs no texture or extra mesh data; sizes are fractions of the card width. Premultiplied alpha; vertex colour
// multiplies the result, so an Image's colour tints a back (the dimmed discard stack, the faint empty slot) and carries
// Button tints and CanvasGroup alpha. One keyword pair (UNITY_UI_CLIP_RECT). Values are pushed from CombatHudStyleSO
// into one runtime copy (CardFrameMaterials.GetBack); the material asset only carries the shader. All material
// properties sit in UnityPerMaterial, the texture outside it.
Shader "Journey to Lanka/UI/Card Back"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}

        _BodyColor ("Body", Color) = (0.45, 0.16, 0.1, 1)
        _Shade ("Top Light", Range(0, 1)) = 0.3
        _CornerRadius ("Corner Radius (of width)", Range(0, 0.25)) = 0.07
        _BorderWidth ("Border Width (of width)", Range(0, 0.1)) = 0.03
        _TrimColor ("Trim", Color) = (0.95, 0.78, 0.42, 0.85)
        _TrimWidth ("Trim Width (of width)", Range(0, 0.03)) = 0.006
        _TrimInset ("Trim Inset (of width)", Range(0, 0.1)) = 0.016
        _InnerLineGap ("Inner Line Gap (of width)", Range(0, 0.06)) = 0.018
        _EmblemSize ("Emblem Size (of width)", Range(0.1, 0.9)) = 0.5
        _Pattern ("Lattice", Range(0, 1)) = 0.35

        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "CardBack"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BodyColor;
                half _Shade;
                float _CornerRadius;
                float _BorderWidth;
                half4 _TrimColor;
                float _TrimWidth;
                float _TrimInset;
                float _InnerLineGap;
                float _EmblemSize;
                half _Pattern;
                float4 _ClipRect;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 canvasPosition : TEXCOORD1;
            };

            // Signed distance to a rounded box centred on the origin (negative inside), in the units of p.
            float RoundedBoxDistance(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            // Signed distance to a rhombus centred on the origin with half extents a (x) and b (y), exact along its
            // edges, which is all an outline needs.
            float RhombusDistance(float2 p, float a, float b)
            {
                return (abs(p.x) * b + abs(p.y) * a - a * b) * rsqrt(max(a * a + b * b, 1e-6));
            }

            // Coverage of a line of the given half width at distance d, anti-aliased over one pixel.
            half Line(float d, float halfWidth)
            {
                return saturate(halfWidth + 0.5 - abs(d));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                output.canvasPosition = input.positionOS.xy;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Card size in screen pixels from how fast uv changes per pixel; exact for any scale or rotation.
                float2 gradientU = float2(ddx(uv.x), ddy(uv.x));
                float2 gradientV = float2(ddx(uv.y), ddy(uv.y));
                float2 size = float2(rcp(max(length(gradientU), 1e-6)), rcp(max(length(gradientV), 1e-6)));
                float width = size.x;

                // The shape is inset by a pixel so its anti-aliased edge stays inside the quad.
                float2 halfSize = max(size * 0.5 - 1.0, 1.0);
                float radius = min(_CornerRadius * width, min(halfSize.x, halfSize.y));
                float2 p = (uv - 0.5) * size;
                float edgeDistance = RoundedBoxDistance(p, halfSize, radius);
                half outer = saturate(0.5 - edgeDistance);
                float depth = -edgeDistance;

                float border = max(_BorderWidth * width, 0.0);
                half inBody = saturate(depth - border + 0.5);
                half3 body = _BodyColor.rgb * lerp(1.0 - _Shade, 1.0 + _Shade * 0.5, uv.y);
                half3 colour = lerp(body * 0.45, body, inBody);

                // Gold trim inset from the border, and a thinner rule inside it.
                float trimHalf = max(_TrimWidth * width * 0.5, 0.5);
                float trimCentre = border + _TrimInset * width;
                half trim = Line(depth - trimCentre, trimHalf);
                float ruleHalf = max(trimHalf * 0.6, 0.4);
                float ruleCentre = trimCentre + _InnerLineGap * width;
                half rule = Line(depth - ruleCentre, ruleHalf);
                half field = saturate(depth - ruleCentre - ruleHalf + 0.5);

                // Emblem: a ring around a dot, inside a rhombus outline.
                float emblem = max(_EmblemSize * width * 0.5, 1.0);
                float strokeHalf = max(emblem * 0.045, 0.5);
                float centreDistance = length(p);
                float rhombus = RhombusDistance(p, emblem * 0.7, emblem);
                half mark = max(max(Line(centreDistance - emblem * 0.36, strokeHalf), saturate(emblem * 0.12 + 0.5 - centreDistance)),
                    Line(rhombus, strokeHalf));

                // Faint diagonal lattice across the field, kept clear of the emblem.
                float cell = max(0.11 * width, 2.0);
                float u = (p.x + p.y) * 0.70710678;
                float v = (p.x - p.y) * 0.70710678;
                float latticeHalf = max(0.004 * width, 0.4);
                half lattice = max(Line((frac(u / cell + 0.5) - 0.5) * cell, latticeHalf),
                    Line((frac(v / cell + 0.5) - 0.5) * cell, latticeHalf));
                half clear = saturate((rhombus - emblem * 0.12) / max(emblem * 0.2, 1.0));
                colour = lerp(colour, lerp(body, _TrimColor.rgb, 0.4), lattice * _Pattern * field * clear);

                half gold = saturate(trim + rule * 0.8 + mark * field) * _TrimColor.a;
                colour = lerp(colour, _TrimColor.rgb, gold);

                half alpha = outer * input.color.a;
                colour *= input.color.rgb;

                #if defined(UNITY_UI_CLIP_RECT)
                    float2 clipInside = step(_ClipRect.xy, input.canvasPosition) * step(input.canvasPosition, _ClipRect.zw);
                    alpha *= clipInside.x * clipInside.y;
                #endif

                return half4(colour * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
