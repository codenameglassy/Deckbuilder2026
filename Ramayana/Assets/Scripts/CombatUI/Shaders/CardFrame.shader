// Card frame for hand, zoom, flight, pile viewer and reward cards: a rounded card with an accent border shaded like a
// bevel, a thin gold trim inset from it, a title band in the accent colour across the top, a soft accent glow along
// the inner edge, and a body gradient lightly tinted by the accent. The accent is the card type's colour.
// Drawn on the frame Image's quad. Every shape is a distance field measured in screen pixels, which the fragment
// shader derives from the uv derivatives, so corners, borders and trim stay crisp and in proportion at any card size,
// scale or fan rotation, and no texture or mesh data is needed. Sizes are fractions of the card width (the band is a
// fraction of its height). Premultiplied alpha; vertex colour carries the Button tint and CanvasGroup alpha.
// One keyword pair (UNITY_UI_CLIP_RECT) so cards clip inside the pile viewer's RectMask2D; nothing else varies.
// Values are pushed from CombatHudStyleSO and the card type's CardTypeVisualsSO into one runtime material per type
// (CardFrameMaterials); the material asset only carries the shader. All material properties sit in UnityPerMaterial.
Shader "Journey to Lanka/UI/Card Frame"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}

        _Accent ("Accent", Color) = (0.86, 0.33, 0.18, 1)
        _BodyTop ("Body Top", Color) = (0.25, 0.2, 0.15, 1)
        _BodyBottom ("Body Bottom", Color) = (0.12, 0.095, 0.075, 1)
        _AccentTint ("Accent Tint", Range(0, 1)) = 0.14
        _CornerRadius ("Corner Radius (of width)", Range(0, 0.25)) = 0.07
        _BorderWidth ("Border Width (of width)", Range(0, 0.1)) = 0.03
        _TrimColor ("Trim", Color) = (0.95, 0.78, 0.42, 0.85)
        _TrimWidth ("Trim Width (of width)", Range(0, 0.03)) = 0.006
        _TrimInset ("Trim Inset (of width)", Range(0, 0.1)) = 0.016
        _BandHeight ("Band Height (of height)", Range(0, 0.6)) = 0.25
        _BandStrength ("Band Strength", Range(0, 1)) = 0.9
        _GlowWidth ("Glow Width (of width)", Range(0.001, 0.25)) = 0.06
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.4
        _Sheen ("Sheen", Range(0, 1)) = 0.08

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
            Name "CardFrame"

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
                half4 _Accent;
                half4 _BodyTop;
                half4 _BodyBottom;
                half _AccentTint;
                float _CornerRadius;
                float _BorderWidth;
                half4 _TrimColor;
                float _TrimWidth;
                float _TrimInset;
                float _BandHeight;
                half _BandStrength;
                float _GlowWidth;
                half _GlowStrength;
                half _Sheen;
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

                float border = max(_BorderWidth * width, 0.0);
                float depth = -(edgeDistance + border);
                half inside = saturate(depth + 0.5);
                half height = uv.y;

                half3 body = lerp(_BodyBottom.rgb, _BodyTop.rgb, height);
                body = lerp(body, _Accent.rgb * 0.5, _AccentTint);

                float bandStart = 1.0 - _BandHeight;
                float bandPixels = (uv.y - bandStart) * size.y;
                half inBand = saturate(bandPixels + 0.5);
                half bandGradient = saturate((uv.y - bandStart) / max(_BandHeight, 1e-4));
                half3 band = _Accent.rgb * lerp(0.62, 1.0, bandGradient);
                half3 colour = lerp(body, band, inBand * _BandStrength);

                half glow = exp(-max(depth, 0.0) / max(_GlowWidth * width, 1e-3)) * _GlowStrength;
                colour += _Accent.rgb * (glow * inside * 0.6);
                colour += _Sheen * smoothstep(0.55, 1.0, height) * (1.0 - inBand * 0.5);

                float trimWidth = max(_TrimWidth * width, 0.75);
                float trimInset = _TrimInset * width;
                half trim = saturate(trimWidth * 0.5 + 0.5 - abs(depth - trimInset)) * inside;
                half bandLine = saturate(trimWidth * 0.5 + 0.5 - abs(bandPixels)) * saturate(depth - trimInset + 0.5);
                colour = lerp(colour, _TrimColor.rgb, max(trim, bandLine) * _TrimColor.a);

                half innerEdge = saturate(1.0 - abs(edgeDistance + border));
                colour *= 1.0 - 0.35 * innerEdge;

                half borderBlend = saturate(-edgeDistance / max(border, 1e-3));
                half3 borderColour = _Accent.rgb * lerp(1.15, 0.7, borderBlend) * lerp(0.8, 1.1, height);
                colour = lerp(borderColour, colour, inside);

                half alpha = outer * input.color.a;

                #if defined(UNITY_UI_CLIP_RECT)
                    float2 clipInside = step(_ClipRect.xy, input.canvasPosition) * step(input.canvasPosition, _ClipRect.zw);
                    alpha *= clipInside.x * clipInside.y;
                #endif

                colour *= input.color.rgb;
                return half4(colour * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
