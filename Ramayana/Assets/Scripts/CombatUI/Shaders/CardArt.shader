// Rounded UI piece for cards: the art window, the type ribbon and the cost orb. It draws the Image's texture (white
// when it has no sprite) times the vertex colour, clipped to a rounded rectangle, darkened toward its edges by a soft
// vignette and lit slightly from the top. With no sprite and the card type's colour as the vertex colour it is the art
// placeholder; with a painting and white it frames the painting. Corner Radius is a fraction of the shorter side, so
// 0.5 makes a pill (the ribbon) or a circle (a square orb). The shape comes from the second uv channel, which
// CardArtMeshEffect fills with the position inside the rect, and is measured in screen pixels from its derivatives,
// so edges stay crisp at any size, scale or fan rotation. Premultiplied alpha; vertex colour carries the tint and
// CanvasGroup alpha. One keyword pair (UNITY_UI_CLIP_RECT) so cards clip inside the pile viewer's RectMask2D.
// Values are pushed from CombatHudStyleSO into runtime copies (CardFrameMaterials); all properties sit in
// UnityPerMaterial, the texture outside it.
Shader "Journey to Lanka/UI/Card Art"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}

        _CornerRadius ("Corner Radius (of shorter side)", Range(0, 0.5)) = 0.1
        _Vignette ("Vignette", Range(0, 1)) = 0.5
        _VignetteWidth ("Vignette Width (of shorter side)", Range(0.01, 1)) = 0.35
        _TopLight ("Top Light", Range(0, 0.5)) = 0.12

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
            Name "CardArt"

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
                float _CornerRadius;
                half _Vignette;
                float _VignetteWidth;
                half _TopLight;
                float4 _ClipRect;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 shape : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 shape : TEXCOORD1;
                float2 canvasPosition : TEXCOORD2;
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
                output.shape = input.shape;
                output.canvasPosition = input.positionOS.xy;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 shape = input.shape;

                // Size in screen pixels from how fast the shape uv changes per pixel; exact for any scale or rotation.
                float2 gradientU = float2(ddx(shape.x), ddy(shape.x));
                float2 gradientV = float2(ddx(shape.y), ddy(shape.y));
                float2 size = float2(rcp(max(length(gradientU), 1e-6)), rcp(max(length(gradientV), 1e-6)));

                // Inset by a pixel so the anti-aliased edge stays inside the quad.
                float2 halfSize = max(size * 0.5 - 1.0, 1.0);
                float shorter = min(size.x, size.y);
                float radius = min(_CornerRadius * shorter, min(halfSize.x, halfSize.y));
                float edgeDistance = RoundedBoxDistance((shape - 0.5) * size, halfSize, radius);
                half coverage = saturate(0.5 - edgeDistance);

                half edge = exp(-max(-edgeDistance, 0.0) / max(_VignetteWidth * shorter, 1e-3));
                half light = 1.0 + _TopLight * (shape.y * 2.0 - 1.0);

                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 colour = texel.rgb * input.color.rgb * (1.0 - _Vignette * edge) * light;
                half alpha = texel.a * input.color.a * coverage;

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
