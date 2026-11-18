// Playable-card glow: a soft halo just outside a card's rounded edge, drawn on a quad behind the card that is larger
// than it by the glow spread on every side. The halo is a Gaussian falloff from the card's rounded rectangle, measured
// in screen pixels from the uv derivatives so it stays even at any size, scale or fan angle, and it fades to zero
// before the quad's edge so the quad never shows. It breathes with a slow pulse whose phase follows the card's
// position, so a hand of glowing cards ripples instead of blinking in step. Additive (premultiplied, zero alpha).
// Vertex alpha is each card's glow strength, so every card shares one material and fades its own glow without a
// material change. No keywords: one variant. Values come from CombatHudStyleSO through CardFrameMaterials.
Shader "Journey to Lanka/UI/Card Glow"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}

        _GlowColor ("Glow", Color) = (1, 0.78, 0.35, 1)
        _Strength ("Strength", Range(0, 3)) = 0.9
        _Inset ("Card Inset (of quad)", Vector) = (0.08, 0.06, 0, 0)
        _CornerRadius ("Corner Radius (of card width)", Range(0, 0.25)) = 0.07
        _Falloff ("Falloff", Range(0.5, 8)) = 2.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.35
        _PulseSpeed ("Pulse Speed", Float) = 0.8
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
            Name "CardGlow"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _GlowColor;
                half _Strength;
                float4 _Inset;
                float _CornerRadius;
                float _Falloff;
                half _PulseAmount;
                float _PulseSpeed;
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
                float phase : TEXCOORD1;
            };

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
                output.phase = input.positionOS.x * 0.004;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 gradientU = float2(ddx(uv.x), ddy(uv.x));
                float2 gradientV = float2(ddx(uv.y), ddy(uv.y));
                float2 size = float2(rcp(max(length(gradientU), 1e-6)), rcp(max(length(gradientV), 1e-6)));

                float2 inset = saturate(_Inset.xy);
                float2 cardHalf = max((0.5 - inset) * size, 1.0);
                float spread = max(inset.x * size.x, 1.0);
                float radius = min(_CornerRadius * cardHalf.x * 2.0, min(cardHalf.x, cardHalf.y));
                float2 p = (uv - 0.5) * size;
                float outside = max(RoundedBoxDistance(p, cardHalf, radius), 0.0) / spread;

                half glow = exp(-outside * outside * _Falloff) * (1.0 - smoothstep(0.75, 1.0, outside));
                half pulse = 1.0 - _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 6.2831853 + input.phase));
                half intensity = glow * pulse * _Strength * input.color.a;
                return half4(_GlowColor.rgb * intensity, 0.0);
            }
            ENDHLSL
        }
    }
}
