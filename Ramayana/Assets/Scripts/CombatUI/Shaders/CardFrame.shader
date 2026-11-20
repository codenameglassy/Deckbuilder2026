// Card frame for hand, flight, pile viewer and reward cards: a rounded card with an accent border shaded like a bevel,
// a thin gold trim inset from it, a rounded name banner in the accent colour edged with trim, a recessed socket for the
// art window ringed with a trim rim (the art Image draws over the socket; only the rim shows around it), a soft accent
// glow along the inner edge, and a body gradient lightly tinted by the accent. Mithila-style finishing: a bevel lit
// from the top left, a second rule inside the trim, a petal-and-dot motif in each corner, and a border of a rule and a
// row of dots along the inside of the banner's top and bottom edges. The accent is the card type's colour; the trim,
// inner rule, corner motifs, banner edge and art rim take the card rarity's colour and, for rarer cards, a slow
// diagonal shimmer driven by _Time (no CPU work per frame). The banner and art rects come from CardFaceLayout as
// fractions of the card (x0, y0, x1, y1, y up). Drawn on the frame Image's quad. Every shape is a distance field
// measured in screen pixels, which the fragment shader derives from the uv derivatives, so corners, borders and trim
// stay crisp and in proportion at any card size, scale or fan rotation, and no texture or mesh data is needed. Sizes
// are fractions of the card width, except the art corner radius, which matches the Card Art shader (a fraction of the
// art window's shorter side). Premultiplied alpha; vertex colour carries the Button tint and CanvasGroup alpha. One
// keyword pair (UNITY_UI_CLIP_RECT) so cards clip inside the pile viewer's RectMask2D; nothing else varies. Values are
// pushed from CombatHudStyleSO, the card type's CardTypeVisualsSO and the rarity's CardRarityVisualsSO into one runtime
// material per type and rarity (CardFrameMaterials); the material asset only carries the shader. All material
// properties sit in UnityPerMaterial.
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
        _BannerRect ("Banner Rect (x0, y0, x1, y1 of card)", Vector) = (0.06, 0.836, 0.94, 0.957)
        _BannerRadius ("Banner Corner Radius (of width)", Range(0, 0.25)) = 0.04
        _BandStrength ("Banner Strength", Range(0, 1)) = 0.9
        _ArtRect ("Art Rect (x0, y0, x1, y1 of card)", Vector) = (0.06, 0.414, 0.94, 0.814)
        _ArtRadius ("Art Corner Radius (of shorter side)", Range(0, 0.5)) = 0.1
        _ArtRimWidth ("Art Rim Width (of width)", Range(0, 0.05)) = 0.012
        _ArtRimColor ("Art Rim", Color) = (0.95, 0.78, 0.42, 1)
        _Bevel ("Bevel", Range(0, 1)) = 0.35
        _BevelWidth ("Bevel Width (of width)", Range(0.005, 0.2)) = 0.05
        _InnerLine ("Inner Line", Range(0, 1)) = 0.6
        _InnerLineGap ("Inner Line Gap (of width)", Range(0, 0.06)) = 0.018
        _Ornament ("Corner Ornaments", Range(0, 1)) = 1
        _OrnamentSize ("Ornament Size (of width)", Range(0.02, 0.2)) = 0.075
        _BannerPattern ("Banner Pattern", Range(0, 1)) = 0.5
        _Shimmer ("Rarity Shimmer", Range(0, 1)) = 0
        _ShimmerSpeed ("Shimmer Sweeps Per Second", Range(0, 2)) = 0.35
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
                float4 _BannerRect;
                float _BannerRadius;
                half _BandStrength;
                float4 _ArtRect;
                float _ArtRadius;
                float _ArtRimWidth;
                half4 _ArtRimColor;
                half _Bevel;
                float _BevelWidth;
                half _InnerLine;
                float _InnerLineGap;
                half _Ornament;
                float _OrnamentSize;
                half _BannerPattern;
                half _Shimmer;
                float _ShimmerSpeed;
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

            // Signed distance to a card-relative rect (x0, y0, x1, y1 as fractions of the card), in pixels.
            float RectDistance(float2 p, float4 rect, float2 size, float radiusPixels)
            {
                float2 centre = ((rect.xy + rect.zw) * 0.5 - 0.5) * size;
                float2 halfSize = max((rect.zw - rect.xy) * 0.5 * size, 0.0);
                return RoundedBoxDistance(p - centre, halfSize, min(radiusPixels, min(halfSize.x, halfSize.y)));
            }

            // Signed distance to an ellipse-like petal lying along +u from the origin, length and half width in pixels.
            // An exact ellipse distance is costly; this scaled-circle form is close enough for a few-pixel motif.
            float PetalDistance(float2 uv, float petalLength, float halfWidth)
            {
                float halfLength = petalLength * 0.5;
                float2 q = float2((uv.x - halfLength) / max(halfLength, 1e-3), uv.y / max(halfWidth, 1e-3));
                return (length(q) - 1.0) * halfWidth;
            }

            // Mithila-style corner motif in one corner, in pixels: a central lotus petal along the diagonal, two side
            // petals splayed from it and a dot beyond its tip. d is the position measured inward from the corner.
            float OrnamentDistance(float2 d, float size)
            {
                float2 axis = float2(d.x + d.y, d.y - d.x) * 0.70710678;
                float centre = PetalDistance(axis, size, size * 0.2);

                const float c = 0.76604444;
                const float s = 0.64278761;
                float2 left = float2(axis.x * c - axis.y * s, axis.x * s + axis.y * c);
                float2 right = float2(axis.x * c + axis.y * s, -axis.x * s + axis.y * c);
                float side = min(PetalDistance(left, size * 0.62, size * 0.14),
                    PetalDistance(right, size * 0.62, size * 0.14));

                float tipDot = length(axis - float2(size * 1.22, 0.0)) - size * 0.09;
                return min(min(centre, side), tipDot);
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

                float bannerDistance = RectDistance(p, _BannerRect, size, _BannerRadius * width);
                half inBand = saturate(0.5 - bannerDistance);
                half bandGradient = saturate((uv.y - _BannerRect.y) / max(_BannerRect.w - _BannerRect.y, 1e-4));
                half3 band = _Accent.rgb * lerp(0.62, 1.0, bandGradient);
                half3 colour = lerp(body, band, inBand * _BandStrength);

                // Mithila border inside the banner along its top and bottom edges: a thin rule and a row of dots.
                float2 bannerCentre = ((_BannerRect.xy + _BannerRect.zw) * 0.5 - 0.5) * size;
                float bannerHalfHeight = (_BannerRect.w - _BannerRect.y) * 0.5 * size.y;
                float2 bannerLocal = p - bannerCentre;
                float fromBannerEdge = bannerHalfHeight - abs(bannerLocal.y);
                float ruleRow = 0.012 * width;
                float dotRow = 0.028 * width;
                float dotSpacing = 0.036 * width;
                float cellX = (frac(bannerLocal.x / dotSpacing + 0.5) - 0.5) * dotSpacing;
                float ruleDistance = abs(fromBannerEdge - ruleRow) - max(0.0035 * width, 0.4);
                float dotDistance = length(float2(cellX, fromBannerEdge - dotRow)) - max(0.0065 * width, 0.6);
                half pattern = saturate(0.5 - min(ruleDistance, dotDistance)) * inBand * _BannerPattern;
                colour = lerp(colour, band * 0.45, pattern);

                float2 artSize = (_ArtRect.zw - _ArtRect.xy) * size;
                float artDistance = RectDistance(p, _ArtRect, size, _ArtRadius * min(artSize.x, artSize.y));
                float rimWidth = max(_ArtRimWidth * width, 0.75);
                half socket = saturate(0.5 - artDistance);
                half shadow = exp(-max(artDistance - rimWidth, 0.0) / max(rimWidth * 3.0, 1.0)) * (1.0 - socket);
                colour *= 1.0 - 0.3 * shadow;
                colour = lerp(colour, body * 0.35, socket);

                half glow = exp(-max(depth, 0.0) / max(_GlowWidth * width, 1e-3)) * _GlowStrength;
                colour += _Accent.rgb * (glow * inside * 0.6);
                colour += _Sheen * smoothstep(0.55, 1.0, height) * (1.0 - inBand * 0.5);

                float trimWidth = max(_TrimWidth * width, 0.75);
                float trimInset = _TrimInset * width;
                half trim = saturate(trimWidth * 0.5 + 0.5 - abs(depth - trimInset)) * inside;
                half bandLine = saturate(trimWidth * 0.5 + 0.5 - abs(bannerDistance));

                // Rarity shimmer: a soft diagonal band of light that sweeps across the card and brightens only the
                // trim, banner edge and art rim. Zero strength costs one multiply.
                float sweep = frac(_Time.y * _ShimmerSpeed) * 1.6 - 0.3;
                half shine = 1.0 + _Shimmer * 1.5 * saturate(1.0 - abs((uv.x + uv.y) * 0.5 - sweep) * 8.0);
                colour = lerp(colour, _TrimColor.rgb * shine, max(trim, bandLine) * _TrimColor.a);

                // Inner frame line: a second, softer rule inside the trim for a double-ruled border, broken where the
                // banner and the art rim cross it so it never draws over them.
                float innerDepth = trimInset + _InnerLineGap * width;
                half innerLine = saturate(trimWidth * 0.5 + 0.5 - abs(depth - innerDepth)) * inside * _InnerLine;
                innerLine *= saturate(bannerDistance - 0.5) * saturate(artDistance - rimWidth - 0.5);
                colour = lerp(colour, _TrimColor.rgb * shine * 0.8, innerLine * _TrimColor.a);

                // Corner ornaments in the rarity trim colour: filled petals with a dark outline, drawn over the banner
                // ends so they read as clasps. The top-left one sits under the cost orb.
                float ornamentSize = max(_OrnamentSize * width, 1.0);
                float cornerInset = trimInset + trimWidth + radius * (1.0 - 0.70710678);
                float2 fromCorner = halfSize - abs(p) - cornerInset;
                float ornamentDistance = OrnamentDistance(fromCorner, ornamentSize);
                half ornamentFill = saturate(0.5 - ornamentDistance) * inside * _Ornament;
                half ornamentOutline = saturate(trimWidth * 0.75 + 0.5 - abs(ornamentDistance)) * inside * _Ornament;
                colour = lerp(colour, _TrimColor.rgb * shine, ornamentFill * _TrimColor.a);
                colour = lerp(colour, _TrimColor.rgb * 0.35, ornamentOutline * _TrimColor.a);

                half rim = saturate(rimWidth + 0.5 - artDistance) * saturate(artDistance + 0.5);
                half rimShade = lerp(0.75, 1.15, saturate(artDistance / max(rimWidth, 1e-3)));
                half3 rimColour = _ArtRimColor.rgb * shine * rimShade;
                colour = lerp(colour, rimColour, rim * _ArtRimColor.a);

                half innerEdge = saturate(1.0 - abs(edgeDistance + border));
                colour *= 1.0 - 0.35 * innerEdge;

                half borderBlend = saturate(-edgeDistance / max(border, 1e-3));
                half3 borderColour = _Accent.rgb * lerp(1.15, 0.7, borderBlend) * lerp(0.8, 1.1, height);
                colour = lerp(borderColour, colour, inside);

                // Bevel: light from the top left, so the top and left edges catch light and the bottom and right fall into
                // shade, fading inward from the outer edge. Each pixel takes the facing of its nearest edge.
                float2 toEdge = abs(p) - halfSize;
                float2 edgeNormal = toEdge.x > toEdge.y ? float2(sign(p.x), 0.0) : float2(0.0, sign(p.y));
                half facing = dot(edgeNormal, float2(-0.70710678, 0.70710678));
                half bevelZone = exp(min(edgeDistance, 0.0) / max(_BevelWidth * width, 1.0));
                colour *= 1.0 + _Bevel * facing * bevelZone;

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
