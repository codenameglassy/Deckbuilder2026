// One Prana orb on the combat HUD: a dark socket with a gold rim, filled by a glowing saffron core whose energy swirls
// slowly, with a small highlight and a soft outer glow. _Fill drains or fills the core (it shrinks and fades as it
// empties), _Flash sends a bright ring outward when an orb is spent or refilled, _Preview gives a pulsing white rim
// and a dimmed core to the orbs a hovered card would spend, and _Deficit a pulsing red rim to the orbs it lacks.
// Drawn on one Image quad per orb. Shapes are distance fields with pixel-accurate anti-aliasing from the uv
// derivatives, so the orb is crisp at any size and needs no texture. Radii are fractions of the quad's half size.
// No keywords: one variant. Premultiplied alpha, where the glow, rims and flash are additive. Vertex alpha carries
// CanvasGroup fades. Each orb owns a runtime copy of the material (PranaOrb) with its own state values; the shared
// look comes from PranaOrbsStyleSO. All material properties sit in UnityPerMaterial.
Shader "Journey to Lanka/UI/Prana Orb"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}

        _CoreColor ("Core", Color) = (1, 0.96, 0.8, 1)
        _GlowColor ("Glow", Color) = (1, 0.62, 0.18, 1)
        _SocketColor ("Socket", Color) = (0.06, 0.045, 0.03, 0.9)
        _RimColor ("Rim", Color) = (0.95, 0.78, 0.42, 0.75)
        _PreviewColor ("Preview Rim", Color) = (1, 1, 1, 1)
        _DeficitColor ("Deficit Rim", Color) = (1, 0.25, 0.2, 1)
        _OrbRadius ("Orb Radius", Range(0.2, 0.95)) = 0.62
        _RimWidth ("Rim Width", Range(0, 0.2)) = 0.05
        _GlowWidth ("Glow Width", Range(0.01, 0.5)) = 0.22
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.55
        _SwirlAmount ("Swirl Amount", Range(0, 1)) = 0.2
        _SwirlSpeed ("Swirl Speed", Float) = 1
        _PulseSpeed ("Pulse Speed", Float) = 1.6

        [HideInInspector] _Fill ("Fill", Range(0, 1)) = 1
        [HideInInspector] _Flash ("Flash", Range(0, 1)) = 0
        [HideInInspector] _Preview ("Preview", Range(0, 1)) = 0
        [HideInInspector] _Deficit ("Deficit", Range(0, 1)) = 0
        [HideInInspector] _Seed ("Seed", Float) = 0
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
            Name "PranaOrb"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _CoreColor;
                half4 _GlowColor;
                half4 _SocketColor;
                half4 _RimColor;
                half4 _PreviewColor;
                half4 _DeficitColor;
                float _OrbRadius;
                float _RimWidth;
                float _GlowWidth;
                half _GlowStrength;
                half _SwirlAmount;
                float _SwirlSpeed;
                float _PulseSpeed;
                half _Fill;
                half _Flash;
                half _Preview;
                half _Deficit;
                float _Seed;
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
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                // Width of one pixel in these units, for anti-aliasing at any size.
                float pixel = max(length(float2(ddx(p.x), ddy(p.x))), 1e-5);
                float radiusFromCentre = length(p);
                float angle = atan2(p.y, p.x);
                float orbRadius = _OrbRadius;
                float time = _Time.y;
                half pulse = 0.5 + 0.5 * sin(time * _PulseSpeed * 6.2831853 + _Seed * 3.0);

                half socket = saturate((orbRadius - radiusFromCentre) / pixel + 0.5);
                half shade = 0.6 + 0.4 * smoothstep(0.0, orbRadius, radiusFromCentre);
                half3 colour = _SocketColor.rgb * shade * (socket * _SocketColor.a);
                half alpha = socket * _SocketColor.a;

                half fill = saturate(_Fill);
                float coreRadius = orbRadius * 0.9 * (0.55 + 0.45 * fill);
                half core = saturate((coreRadius - radiusFromCentre) / pixel + 0.5) * fill;
                half falloff = smoothstep(0.0, coreRadius, radiusFromCentre);
                half3 coreColour = lerp(_CoreColor.rgb, _GlowColor.rgb, falloff);
                float swirlPhase = time * _SwirlSpeed;
                half swirl = 1.0 + _SwirlAmount
                    * sin(3.0 * angle + 6.0 * radiusFromCentre / orbRadius - swirlPhase * 2.0 + _Seed * 6.2831853)
                    * sin(2.0 * angle - 4.0 * radiusFromCentre / orbRadius + swirlPhase * 1.4);
                coreColour *= swirl;
                float2 highlightOffset = p - float2(-0.25, 0.3) * orbRadius;
                coreColour += exp(-dot(highlightOffset, highlightOffset) / 0.02) * 0.6;
                coreColour *= 1.0 - 0.35 * _Preview * pulse;
                colour = lerp(colour, coreColour, core);
                alpha = lerp(alpha, 1.0, core);

                half rim = saturate((_RimWidth * 0.5 - abs(radiusFromCentre - orbRadius)) / pixel + 0.5);
                colour = colour * (1.0 - rim * 0.8) + _RimColor.rgb * (_RimColor.a * rim);
                alpha = max(alpha, rim * 0.8);

                float outside = max(radiusFromCentre - orbRadius, 0.0) / _GlowWidth;
                half glow = exp(-outside * outside) * step(orbRadius * 0.9, radiusFromCentre) * fill * _GlowStrength;
                // Fade the glow to zero before the quad edge so the quad never shows.
                glow *= 1.0 - smoothstep(0.85, 1.0, radiusFromCentre);
                colour += _GlowColor.rgb * glow;

                float rimDistance = (radiusFromCentre - orbRadius) / 0.06;
                half rimGlow = exp(-rimDistance * rimDistance) * (1.0 - smoothstep(0.85, 1.0, radiusFromCentre));
                colour += _PreviewColor.rgb * (rimGlow * _Preview * (0.5 + 0.5 * pulse));
                colour += _DeficitColor.rgb * (rimGlow * _Deficit * (0.6 + 0.4 * pulse) * 1.4);

                float flashRadius = orbRadius * (1.0 + (1.0 - _Flash) * 0.7);
                float flashDistance = (radiusFromCentre - flashRadius) / 0.07;
                half flash = exp(-flashDistance * flashDistance) * _Flash * (1.0 - smoothstep(0.85, 1.0, radiusFromCentre));
                colour += half3(1.0, 0.95, 0.8) * flash;

                half fade = input.color.a;
                return half4(colour * fade, alpha * fade);
            }
            ENDHLSL
        }
    }
}
