// Slash for combat hits: a curved blade of light swept across the struck fighter, with a hot core, a tail that fades
// behind the sweeping head and ends that taper to points, evaluated per pixel with no textures. Drawn on the shared
// VFX quad (unit square centred on its pivot). The vertex shader turns the quad to face the camera, rolls it so the
// sweep follows the blow's direction on screen (_Direction, world space; w is 0 when there is none, and the seed picks
// the angle instead), adds a small per-spawn jitter, scales it by the object's uniform scale and pushes it toward the
// camera, in front of the impact burst. Fades softly where it meets scene geometry (scene depth texture).
// Progress (0 to 1) is driven from C# on game time, so a slash holds still during hit-stop: the head sweeps over the
// first Sweep Portion of it, then the whole blade fades. Premultiplied blending with zero alpha, so it adds light.
// No keywords: exactly one variant. Every value comes from SlashArcLook at spawn; the material asset only carries the
// shader. SRP Batcher compatible: all properties in UnityPerMaterial.
Shader "Journey to Lanka/VFX/Slash Arc"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (3, 1.3, 0.4, 1)
        [HDR] _CoreColor ("Core Color", Color) = (5, 4.5, 3.8, 1)
        _Intensity ("Intensity", Float) = 1
        _Progress ("Progress", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0
        _Direction ("Direction", Vector) = (1, 0, 0, 0)

        _Width ("Width", Range(0.005, 0.4)) = 0.09
        _CoreShare ("Core Share", Range(0.05, 1)) = 0.35
        _Bow ("Bow", Range(0.05, 0.9)) = 0.35
        _SweepPortion ("Sweep Portion", Range(0.05, 1)) = 0.45
        _TailLength ("Tail Length", Range(0.1, 2)) = 0.9
        _FadePower ("Fade Power", Float) = 1.5
        _AngleJitter ("Angle Jitter (radians)", Float) = 0.44

        _CameraPush ("Camera Push", Float) = 0.5
        _SoftFade ("Soft Fade Distance", Float) = 0.25
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
            Name "SlashArc"

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

            // Half the chord of the arc, in quad radii: the blade runs from -ARC_HALF_CHORD to ARC_HALF_CHORD along x.
            #define ARC_HALF_CHORD 0.85

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                float _Intensity;
                float _Progress;
                float _Seed;
                float4 _Direction;
                float _Width;
                float _CoreShare;
                float _Bow;
                float _SweepPortion;
                float _TailLength;
                float _FadePower;
                float _AngleJitter;
                float _CameraPush;
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
                // Quad position in [-1, 1] on both axes; +x is the direction the slash sweeps.
                float2 local : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float scale = length(float3(objectToWorld[0][0], objectToWorld[1][0], objectToWorld[2][0]));

                float3 cameraRight = UNITY_MATRIX_V[0].xyz;
                float3 cameraUp = UNITY_MATRIX_V[1].xyz;

                // The blow's direction on screen, or a seeded angle when there is none, plus a seeded jitter.
                float screenX = dot(_Direction.xyz, cameraRight);
                float screenY = dot(_Direction.xyz, cameraUp);
                bool hasDirection = _Direction.w > 0.5 && (abs(screenX) + abs(screenY)) > 1e-4;
                float angle = hasDirection ? atan2(screenY, screenX) : _Seed * TWO_PI;
                angle += (_Seed * 2.0 - 1.0) * _AngleJitter;

                float sinRoll;
                float cosRoll;
                sincos(angle, sinRoll, cosRoll);
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
                float t = saturate(_Progress);

                // Circle through (-ARC_HALF_CHORD, 0), (ARC_HALF_CHORD, 0) and (0, _Bow), so the blade bows upward.
                float bow = max(_Bow, 0.05);
                float radius = (ARC_HALF_CHORD * ARC_HALF_CHORD + bow * bow) / (2.0 * bow);
                float2 center = float2(0.0, bow - radius);
                float arcDistance = length(p - center) - radius;

                // Position along the blade, -1 at the start and 1 at the end.
                float along = clamp(p.x / ARC_HALF_CHORD, -1.5, 1.5);

                float sweep = saturate(t / max(_SweepPortion, 1e-3));
                float sweepEase = 1.0 - (1.0 - sweep) * (1.0 - sweep) * (1.0 - sweep);
                float head = lerp(-1.15, 1.15, sweepEase);
                float behind = head - along;
                float trail = saturate(1.0 - behind / max(_TailLength, 1e-3)) * smoothstep(-0.04, 0.03, behind);

                float endTaper = sin(PI * saturate(along * 0.5 + 0.5));
                float width = max(_Width * endTaper * (0.45 + 0.55 * trail), 1e-4);
                float body = exp(-(arcDistance * arcDistance) / (width * width));
                float coreWidth = max(width * _CoreShare, 1e-4);
                float core = exp(-(arcDistance * arcDistance) / (coreWidth * coreWidth));

                float afterSweep = saturate((t - _SweepPortion) / max(1.0 - _SweepPortion, 1e-3));
                float fade = PositivePow(1.0 - afterSweep, _FadePower);

                float2 edgeDistance = abs(p);
                float edge = 1.0 - smoothstep(0.85, 1.0, max(edgeDistance.x, edgeDistance.y));

                float3 color = (_Color.rgb * body + _CoreColor.rgb * core) * (trail * fade * edge * _Intensity);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float soft = saturate((sceneEyeDepth - input.eyeDepth) / max(_SoftFade, 1e-4));

                return half4(color * soft, 0.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
