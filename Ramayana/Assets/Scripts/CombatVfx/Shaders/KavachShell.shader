// Kavach shield hull: a fighter's body mesh drawn again, every vertex pushed outward by Thickness (world units,
// grown by Swell on a Kavach gain) so the hull stands just off the body: a slightly larger cube for a cube, a glowing
// skin that follows the silhouette and animation for a character. The push follows a smooth normal baked into UV3 in
// tangent space by KavachShellMeshBaker, rebuilt here from the vertex's (skinned) normal and tangent, so hard edges
// never tear apart and the push bends with the body; a mesh without the bake falls back to its own normal.
// Front faces only, so the near side sits in front of the body and the far side never shows through it.
// The look is a force field: a fresnel rim from the smooth normal (rounded even on a cube), a honeycomb mapped
// triplanar in the mesh's own space scaled to world units (so it sticks to the body as it moves), bright toward the
// edges and faint in the middle, a faint fill, a shimmer rising through it on game time (_Time, so it slows with
// hit-stop), fading softly where it cuts into the ground (scene depth texture). Keep Soft Fade below Thickness so
// the body just behind the hull never fades it.
// Driven effects: Strength and Flash scale the whole hull; an absorb ripple is a ring that runs out from
// _RippleOrigin (world point on the side facing the attacker) by Ripple Reach, Ripple Band wide, lighting the
// honeycomb it crosses; Break dissolves the hull cell by cell with glowing edges. Premultiplied blending with zero
// alpha, so it adds light; where layers of one character overlap they add up (accepted for now).
// No keywords: exactly one variant. Static values come from KavachVfxProfileSO.ApplyStatic, driven ones from
// KavachShell. SRP Batcher compatible.
Shader "Journey to Lanka/VFX/Kavach Shell"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.25, 0.95, 1.5, 1)
        _Fill ("Fill", Range(0, 0.5)) = 0.06
        [HDR] _RimColor ("Rim Color", Color) = (0.6, 1.9, 2.5, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _Thickness ("Thickness", Range(0.005, 0.5)) = 0.07
        _PatternDensity ("Honeycomb Cells Per Unit", Range(0.25, 12)) = 4
        _PatternWidth ("Honeycomb Line Width", Range(0.005, 0.2)) = 0.04
        _PatternStrength ("Honeycomb Strength", Range(0, 2)) = 0.6
        _ShimmerSpeed ("Shimmer Speed", Float) = 0.6
        _SoftFade ("Soft Fade Distance", Float) = 0.03

        [HDR] _RippleColor ("Ripple Color", Color) = (2, 4, 5, 1)
        _RippleBand ("Ripple Band", Range(0.05, 2)) = 0.3
        _RippleReach ("Ripple Reach", Range(0.2, 6)) = 2.2
        [HDR] _BreakColor ("Break Color", Color) = (2.5, 4.5, 5, 1)
        _ShardDensity ("Shard Cells Per Unit", Range(0.5, 12)) = 4

        _Strength ("Strength", Range(0, 1)) = 1
        _Flash ("Flash", Float) = 0
        _Swell ("Swell", Float) = 0
        _RippleOrigin ("Ripple Origin", Vector) = (0, 0, 0, 0)
        _RippleProgress ("Ripple Progress", Range(0, 1)) = 1
        _RippleStrength ("Ripple Strength", Float) = 0
        _Break ("Break", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "KavachShell"

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Fill;
                half4 _RimColor;
                float _RimPower;
                float _Thickness;
                float _PatternDensity;
                float _PatternWidth;
                float _PatternStrength;
                float _ShimmerSpeed;
                float _SoftFade;
                half4 _RippleColor;
                float _RippleBand;
                float _RippleReach;
                half4 _BreakColor;
                float _ShardDensity;
                float _Strength;
                float _Flash;
                float _Swell;
                float4 _RippleOrigin;
                float _RippleProgress;
                float _RippleStrength;
                float _Break;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                // Smooth normal in the vertex's tangent frame, baked by KavachShellMeshBaker. Zero when not baked.
                float3 smoothTS : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 smoothNormalWS : TEXCOORD0;
                float3 smoothNormalOS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                // Mesh-space position scaled to world units, so cells keep one size and stick to the body.
                float3 patternPosition : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                float eyeDepth : TEXCOORD5;
            };

            float KavachHash31(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // Distance from p to the nearest edge of a hexagonal grid with cells one unit across: 0 on an edge, 0.5
            // at a cell's centre.
            float HexEdgeDistance(float2 p)
            {
                const float2 spacing = float2(1.0, 1.7320508);
                float4 centres = floor(float4(p, p - float2(0.5, 1.0)) / spacing.xyxy) + 0.5;
                float4 local = float4(p - centres.xy * spacing, p - (centres.zw + 0.5) * spacing);
                float2 nearest = dot(local.xy, local.xy) < dot(local.zw, local.zw) ? local.xy : local.zw;
                nearest = abs(nearest);
                return 0.5 - max(dot(nearest, spacing * 0.5), nearest.x);
            }

            // Honeycomb lines projected on the three mesh planes and blended by the normal, so the pattern wraps the
            // body without seams or stretching.
            float Honeycomb(float3 position, float3 normal)
            {
                float3 weights = PositivePow(abs(normal), 4.0);
                weights /= max(weights.x + weights.y + weights.z, 1e-4);
                float edgeDistance = HexEdgeDistance(position.zy) * weights.x
                    + HexEdgeDistance(position.xz) * weights.y
                    + HexEdgeDistance(position.xy) * weights.z;
                return 1.0 - smoothstep(0.0, max(_PatternWidth, 1e-4), edgeDistance);
            }

            float3 SmoothNormalOS(Attributes input)
            {
                float3 normal = normalize(input.normalOS);
                float3 tangent = input.tangentOS.xyz - normal * dot(normal, input.tangentOS.xyz);
                if (dot(input.smoothTS, input.smoothTS) < 0.25 || dot(tangent, tangent) < 1e-8)
                {
                    return normal;
                }

                tangent = normalize(tangent);
                float3 bitangent = cross(normal, tangent) * (input.tangentOS.w < 0.0 ? -1.0 : 1.0);
                return normalize(tangent * input.smoothTS.x + bitangent * input.smoothTS.y + normal * input.smoothTS.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 smoothOS = SmoothNormalOS(input);
                float3 smoothWS = normalize(TransformObjectToWorldNormal(smoothOS));
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += smoothWS * (_Thickness * (1.0 + _Swell));

                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float3 objectScale = float3(
                    length(float3(objectToWorld[0][0], objectToWorld[1][0], objectToWorld[2][0])),
                    length(float3(objectToWorld[0][1], objectToWorld[1][1], objectToWorld[2][1])),
                    length(float3(objectToWorld[0][2], objectToWorld[1][2], objectToWorld[2][2])));

                output.positionCS = TransformWorldToHClip(positionWS);
                output.smoothNormalWS = smoothWS;
                output.smoothNormalOS = smoothOS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                output.patternPosition = input.positionOS.xyz * objectScale;
                output.positionWS = positionWS;
                output.eyeDepth = -TransformWorldToView(positionWS).z;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.smoothNormalWS);
                float3 viewDir = normalize(input.viewDirWS);
                float time = _Time.y * _ShimmerSpeed;

                float facing = saturate(dot(normal, viewDir));
                float rim = PositivePow(1.0 - facing, _RimPower);
                // 0 at the middle of the hull as seen, 1 at its silhouette; the honeycomb and fill follow it.
                float edgeWeight = PositivePow(1.0 - facing, 1.5);

                float honeycomb = Honeycomb(input.patternPosition * _PatternDensity, normalize(input.smoothNormalOS));
                float shimmer = 0.55 + 0.45 * sin(input.patternPosition.y * 4.0 - time * 4.0);

                // Absorb ripple: a ring running out across the hull from the origin, over a glow at the origin.
                float fromOrigin = distance(input.positionWS, _RippleOrigin.xyz);
                float band = max(_RippleBand, 1e-3);
                float ringOffset = (fromOrigin - _RippleProgress * _RippleReach) / band;
                float remain = 1.0 - _RippleProgress;
                float originGlow = exp(-(fromOrigin * fromOrigin) / (band * band) * 2.0);
                float ripple = (exp(-ringOffset * ringOffset) * remain + originGlow * remain * remain) * _RippleStrength * _RippleOrigin.w;

                float patternLight = honeycomb * _PatternStrength * (0.15 + 0.85 * edgeWeight) * shimmer;
                float3 shell = _Color.rgb * (_Fill * (0.2 + 0.8 * edgeWeight) + patternLight) + _RimColor.rgb * rim;
                shell *= _Strength * (1.0 + _Flash);

                float3 color = shell + _RippleColor.rgb * ripple * (0.35 + 0.65 * honeycomb);

                // Break: cells drop out in random order; cells about to go glow at their edges.
                float cellRandom = KavachHash31(floor(input.patternPosition * _ShardDensity) + 17.0);
                float alive = step(_Break, cellRandom);
                float breaking = step(1e-4, _Break);
                float edgeGlow = (1.0 - saturate((cellRandom - _Break) / 0.15)) * breaking * alive;
                color = color * alive + _BreakColor.rgb * edgeGlow * (1.0 - _Break);

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
