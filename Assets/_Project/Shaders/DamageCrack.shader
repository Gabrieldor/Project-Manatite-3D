Shader "Manatite/DamageCrack"
{
    Properties
    {
        _Damage("Damage", Range(0,1)) = 0
        _CrackScale("Crack Scale", Float) = 10
        _CrackWidth("Max Crack Width", Range(0,0.1)) = 0.03
        _CrackColor("Crack Color", Color) = (0.03,0.05,0.06,1)
        _DistortStrength("Distortion Strength", Range(0,0.05)) = 0.015
        _EdgeDarken("Edge Darken Strength", Range(0,2)) = 1.0
        _VignetteColor("Vignette Color", Color) = (0,0,0,1)
        _AberrationStrength("Chromatic Aberration Strength", Range(0,2)) = 0.6
        _FrontSoftness("Spread Front Softness", Range(0.02,0.6)) = 0.18
        _FrontJitter("Spread Front Jitter", Range(0,0.5)) = 0.12
        _CenterScaleMultiplier("Center Crack Scale Multiplier (<1 = bigger cells at center)", Range(0.1,1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "DamageCrackPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            SAMPLER(sampler_BlitTexture);

            float _Damage;
            float _CrackScale;
            float _CrackWidth;
            half4 _CrackColor;
            float _DistortStrength;
            float _EdgeDarken;
            half4 _VignetteColor;
            float _AberrationStrength;
            float _FrontSoftness;
            float _FrontJitter;
            float _CenterScaleMultiplier;

            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            void Voronoi(float2 uv, out float f1, out float f2, out float2 cellId)
            {
                float2 cell = floor(uv);
                float2 local = frac(uv);

                f1 = 8.0;
                f2 = 8.0;
                cellId = cell;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 pt = Hash2(cell + neighbor);
                        float2 diff = neighbor + pt - local;
                        float dist = length(diff);

                        if (dist < f1)
                        {
                            f2 = f1;
                            f1 = dist;
                            cellId = cell + neighbor;
                        }
                        else if (dist < f2)
                        {
                            f2 = dist;
                        }
                    }
                }
            }

            float CrackField(float2 uv)
            {
                float f1, f2;
                float2 cellId;
                Voronoi(uv, f1, f2, cellId);
                return f2 - f1;
            }

            float Hash1(float2 p)
            {
                return frac(sin(dot(p, float2(41.3, 289.1))) * 43758.5453123);
            }

            struct CrackSample
            {
                float edge;
                float2 cellId;
                float2 distortDir;
            };

            // Samples one crack layer at a fixed, non-varying scale. Two of these
            // (coarse + fine) get cross-faded by screen position below, which avoids the
            // shearing you get from continuously warping a single layer's UV scale.
            CrackSample SampleCrackLayer(float2 baseUV, float scale)
            {
                float2 layerUV = baseUV * scale;

                CrackSample s;
                float f1, f2;
                Voronoi(layerUV, f1, f2, s.cellId);
                s.edge = f2 - f1;

                float texel = 0.001 * scale;
                float edgeDx = CrackField(layerUV + float2(texel, 0)) - CrackField(layerUV - float2(texel, 0));
                float edgeDy = CrackField(layerUV + float2(0, texel)) - CrackField(layerUV - float2(0, texel));
                s.distortDir = normalize(float2(edgeDx, edgeDy) + 1e-5);
                return s;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 baseUV = float2((uv.x - 0.5) * aspect + 0.5, uv.y);

                // Cracks start at the screen corners and spread toward the center as
                // damage rises: each cell reveals once the expanding front reaches it,
                // with a per-cell jitter so the front looks like a jagged crack spread
                // rather than a perfect circle.
                float2 centered = uv - 0.5;
                centered.x *= aspect;
                float spreadRadial = length(centered) / length(float2(0.5 * aspect, 0.5));

                // Two independent, undistorted crack layers — coarse (big cells) and fine
                // (small cells) — cross-faded by screen position. Cross-fading two fixed
                // patterns keeps each one geometrically clean, unlike warping a single
                // pattern's UV scale per-pixel, which shears the cells.
                float coarseScale = _CrackScale * _CenterScaleMultiplier;
                CrackSample fine = SampleCrackLayer(baseUV, _CrackScale);
                CrackSample coarse = SampleCrackLayer(baseUV, coarseScale);

                // A narrow, steep selector rather than a linear blend: fine is fully
                // suppressed (not just dimmed) below the seam and coarse fully suppressed
                // above it, so each zone shows ONE clean pattern instead of the fine
                // layer's dense grid bleeding through everywhere at reduced opacity.
                float layerBlend = smoothstep(0.35, 0.65, spreadRadial);

                float cellJitter = (Hash1(fine.cellId) - 0.5) * _FrontJitter;
                float frontRadius = saturate(1.0 - _Damage) + cellJitter;
                float damageMask = smoothstep(frontRadius - _FrontSoftness, frontRadius + _FrontSoftness, spreadRadial);

                float crackWidthFine = _CrackWidth * _Damage;
                float crackWidthCoarse = crackWidthFine * _CenterScaleMultiplier;

                float crackMaskFine = (1.0 - smoothstep(0.0, crackWidthFine, fine.edge)) * damageMask;
                float crackMaskCoarse = (1.0 - smoothstep(0.0, crackWidthCoarse, coarse.edge)) * damageMask;
                float crackMask = lerp(crackMaskCoarse, crackMaskFine, layerBlend);
                float crackWidth = lerp(crackWidthCoarse, crackWidthFine, layerBlend);

                float2 distortDir = normalize(lerp(coarse.distortDir, fine.distortDir, layerBlend) + 1e-5);

                float2 distortedUV = uv + distortDir * crackMask * _DistortStrength;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, distortedUV);

                half4 color = lerp(sceneColor, _CrackColor, saturate(crackMask * 1.5));

                // Chromatic aberration is painted as a tint rather than re-sampling the
                // scene at an offset, so it reads consistently over a flat skybox instead
                // of only showing up where nearby scene contrast happens to reveal it.
                float haloMaskFine = (1.0 - smoothstep(crackWidthFine, crackWidthFine * 4.0, fine.edge)) * damageMask;
                float haloMaskCoarse = (1.0 - smoothstep(crackWidthCoarse, crackWidthCoarse * 4.0, coarse.edge)) * damageMask;
                float haloMask = lerp(haloMaskCoarse, haloMaskFine, layerBlend);
                float fringeMask = saturate(haloMask - crackMask);

                float side = dot(distortDir, float2(0.7071, 0.7071));
                half3 warmFringe = half3(1.0, 0.2, 0.1);
                half3 coolFringe = half3(0.1, 0.55, 1.0);
                half3 fringeTint = lerp(coolFringe, warmFringe, saturate(side * 0.5 + 0.5));

                color.rgb += fringeTint * fringeMask * _AberrationStrength;

                float radial = length(uv - 0.5) * 1.4;
                float darken = smoothstep(0.25, 1.0, radial) * _Damage * _EdgeDarken;
                color.rgb = lerp(color.rgb, _VignetteColor.rgb, saturate(darken));

                return color;
            }
            ENDHLSL
        }
    }
}
