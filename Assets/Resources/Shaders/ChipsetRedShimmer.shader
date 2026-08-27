Shader "PGE/UI/Chipset Red Shimmer"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShimmerColor ("Shimmer Color", Color) = (1,0.86,0.55,1)
        _WaveTravelDuration ("Wave Travel Duration", Float) = 2.8
        _WaveGap ("Time Between Waves", Float) = 1.0
        _ShimmerWidth ("Shimmer Width", Range(0.02, 0.5)) = 0.07
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 3)) = 1.55
        _SolarGlowColor ("Solar Glow Color", Color) = (1,0.16,0.02,1)
        _SolarSparkColor ("Solar Spark Color", Color) = (1,0.82,0.2,1)
        _SolarGlowIntensity ("Solar Glow Intensity", Range(0, 2)) = 0.95
        _SolarPulseSpeed ("Solar Pulse Speed", Float) = 0.75
        _StarColor ("Vietnam Star Color", Color) = (1,0.82,0.035,1)
        _StarIntensity ("Star Intensity", Range(0, 2)) = 1.15
        _StarSize ("Star Size", Range(0.05, 0.45)) = 0.23
        _ClothWaveStrength ("Cloth Wave Strength", Range(0, 0.08)) = 0.026
        
        [Header(Radiating Outer Aura)]
        _OuterAuraColor ("Outer Aura Color", Color) = (1, 0.22, 0.02, 1)
        _OuterAuraRayColor ("Outer Aura Ray Color", Color) = (1, 0.85, 0.3, 1)
        _OuterAuraIntensity ("Outer Aura Intensity", Range(0, 4)) = 1.8
        _OuterAuraPulseSpeed ("Outer Aura Pulse Speed", Float) = 2.2
        
        [HideInInspector] _SpriteUVRect ("Sprite UV Rect", Vector) = (0,0,1,1)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "ChipsetRedShimmer"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            fixed4 _ShimmerColor;
            fixed4 _SolarGlowColor;
            fixed4 _SolarSparkColor;
            fixed4 _StarColor;
            fixed4 _OuterAuraColor;
            fixed4 _OuterAuraRayColor;
            float4 _ClipRect;
            float4 _SpriteUVRect;
            float _WaveTravelDuration;
            float _WaveGap;
            float _ShimmerWidth;
            float _ShimmerIntensity;
            float _SolarGlowIntensity;
            float _SolarPulseSpeed;
            float _StarIntensity;
            float _StarSize;
            float _ClothWaveStrength;
            float _OuterAuraIntensity;
            float _OuterAuraPulseSpeed;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            float FivePointStarMask(float2 uv, float starScale)
            {
                float2 p = uv - 0.5;
                p.x *= 0.78;

                float angle = atan2(p.y, p.x) - 1.5708;
                float sector = 0.6283185;
                float segment = abs(frac((angle + sector) / (sector * 2.0))
                    * sector * 2.0 - sector);
                float boundary = lerp(starScale, starScale * 0.43, saturate(segment / sector));
                float edgeSoftness = 0.007;
                return 1.0 - smoothstep(
                    boundary - edgeSoftness,
                    boundary + edgeSoftness,
                    length(p));
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 localUv = (input.texcoord - _SpriteUVRect.xy) / _SpriteUVRect.zw;
                float flagTime = _Time.y * 0.55;

                // Sample texture directly at exact undistorted UV so the frame shape,
                // borders, edges and tassels remain 100% solid, crisp and never deformed.
                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;

                float uiClip = 1.0;
                #ifdef UNITY_UI_CLIP_RECT
                uiClip = UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                color.a *= uiClip;
                #endif

                float redMask = saturate((color.r - max(color.g, color.b)) * 4.0) * color.a;

                // Internal waving cloth coordinates applied purely to the lighting, shimmer, and star inside
                float broadFold = sin(localUv.y * 9.0 - flagTime * 1.35);
                float fineFold = sin(localUv.y * 19.0 + localUv.x * 4.5 - flagTime * 2.1);
                float2 wavingUv = localUv;
                wavingUv.x += broadFold * _ClothWaveStrength
                    + fineFold * _ClothWaveStrength * 0.34;
                wavingUv.y += sin(localUv.x * 7.0 + flagTime * 1.15)
                    * _ClothWaveStrength * 0.42;

                // Undulating cloth light and shadow only modulates the red interior surface
                float clothLight = 0.92 + broadFold * 0.08 + fineFold * 0.03;
                color.rgb *= lerp(1.0, clothLight, redMask);

                // Golden shimmer sweep with ripples across the red metal
                float travelDuration = max(0.1, _WaveTravelDuration);
                float cycleDuration = travelDuration + max(0.0, _WaveGap);
                float cycleIndex = floor(_Time.y / cycleDuration);
                float cycleTime = frac(_Time.y / cycleDuration) * cycleDuration;
                float travelProgress = saturate(cycleTime / travelDuration);
                float reverseDirection = frac(cycleIndex * 0.5) * 2.0;
                float forwardCentre = lerp(-0.25, 1.25, travelProgress);
                float backwardCentre = lerp(1.25, -0.25, travelProgress);
                float sweepCentre = lerp(forwardCentre, backwardCentre, reverseDirection);
                float waveTime = _Time.y * 0.18;
                float curvedX = wavingUv.x
                    + sin(wavingUv.y * 13.0 + waveTime * 3.2) * 0.055
                    + sin(wavingUv.y * 27.0 - waveTime * 1.7) * 0.018;
                float bandDistance = abs(curvedX - sweepCentre);
                float waveCore = 1.0 - smoothstep(_ShimmerWidth * 0.22, _ShimmerWidth, bandDistance);
                float waveHalo = 1.0 - smoothstep(_ShimmerWidth, _ShimmerWidth * 2.2, bandDistance);

                // Two interfering ripples keep the red metal gently alive between sweeps
                float rippleA = sin(wavingUv.x * 22.0 + wavingUv.y * 12.0 - waveTime * 5.0);
                float rippleB = sin(wavingUv.x * 13.0 - wavingUv.y * 19.0 + waveTime * 3.4);
                float waterRipple = pow(saturate(0.5 + 0.25 * (rippleA + rippleB)), 5.0);

                float shimmer = waveCore + waveHalo * 0.34 + waterRipple * 0.16;
                float glow = redMask * shimmer * _ShimmerIntensity;
                color.rgb += _ShimmerColor.rgb * glow;

                // Solar boiling embers and pulse on red surface
                float solarTime = _Time.y * _SolarPulseSpeed;
                float solarPulse = 0.76 + sin(solarTime) * 0.24;
                float solarCellA = sin(wavingUv.x * 47.0
                    + sin(wavingUv.y * 19.0 + solarTime * 1.4) * 2.2
                    + solarTime * 1.8);
                float solarCellB = sin(wavingUv.y * 41.0
                    - wavingUv.x * 16.0
                    - solarTime * 1.25);
                float solarGranules = pow(saturate(0.5 + 0.25 * (solarCellA + solarCellB)), 6.0);

                float solarBase = redMask * solarPulse * _SolarGlowIntensity * 0.38;
                float solarSpark = redMask * solarGranules * _SolarGlowIntensity * 1.25;
                color.rgb += _SolarGlowColor.rgb * solarBase;
                color.rgb += _SolarSparkColor.rgb * solarSpark;

                // Five-point star placed inside the red background (confined by redMask)
                float starMask = FivePointStarMask(wavingUv, _StarSize) * uiClip * redMask;
                float starSheen = 0.88 + 0.22 * sin(
                    wavingUv.x * 18.0 + wavingUv.y * 8.0 - solarTime * 2.0);
                float3 illuminatedStar = _StarColor.rgb * starSheen * _StarIntensity;
                color.rgb = lerp(color.rgb, illuminatedStar, saturate(starMask * 0.96));
                color.a = max(color.a, starMask * _StarColor.a * input.color.a);

                // --- RADIATING OUTWARD AURA & RAYS EFFECT ---
                float2 centerVec = localUv - 0.5;
                float2 absCenter = abs(centerVec);
                
                // Outer perimeter distance calculation for card rectangle
                float boxDistX = max(0.0, absCenter.x - 0.32) / 0.18;
                float boxDistY = max(0.0, absCenter.y - 0.34) / 0.16;
                float edgeDist = length(float2(boxDistX, boxDistY));

                // Radiating ray flares sweeping outward from card
                float rayAngle = atan2(centerVec.y, centerVec.x);
                float rayPatternA = sin(rayAngle * 10.0 + _Time.y * 2.0) * 0.5 + 0.5;
                float rayPatternB = sin(rayAngle * 18.0 - _Time.y * 2.8) * 0.5 + 0.5;
                float combinedRays = pow(rayPatternA * 0.65 + rayPatternB * 0.35, 2.5);

                // Rhythmic aura pulse
                float auraPulse = 0.80 + 0.20 * sin(_Time.y * _OuterAuraPulseSpeed);
                float auraSoftGlow = exp(-edgeDist * 2.8) * auraPulse * _OuterAuraIntensity;
                float auraRayIntensity = auraSoftGlow * (0.35 + combinedRays * 0.85);

                // Blend radiant aura color between hot red-orange and bright golden rays
                fixed3 auraRgb = lerp(_OuterAuraColor.rgb, _OuterAuraRayColor.rgb, combinedRays);
                
                // Radiating glow onto both the edge rim of the card and outward into surrounding space
                color.rgb += auraRgb * (auraRayIntensity * 0.85 + redMask * auraSoftGlow * 0.4);
                color.a = max(color.a, saturate(auraSoftGlow * 0.7) * uiClip * _OuterAuraColor.a * input.color.a);

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
