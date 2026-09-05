Shader "Custom/UI/UIDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Dissolve and Stardust Sandification)]
        _NoiseTex ("Dissolve Noise Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Progress", Range(0, 1)) = 0.0
        _DisintegrationWidth ("Disintegration Width (Sand Zone)", Range(0.05, 0.5)) = 0.22
        _GrainSize ("Grain / Stardust Size (Pixels)", Range(0.5, 4.0)) = 1.8
        _DriftAmount ("Drift / Dispersion Amount", Range(0.0, 3.0)) = 0.85
        _SparkleIntensity ("Stardust Sparkle Glint", Range(0.0, 5.0)) = 2.2

        [Header(Color and Glow Mode)]
        [Toggle] _UseUIColor ("Use UI Color (Adaptive)", Float) = 1.0
        [HDR] _EdgeColor ("Outer Glow Corona (HDR)", Color) = (0.3, 0.9, 1.0, 1.0)
        [HDR] _InnerEdgeColor ("Inner Core Flash (HDR)", Color) = (1.8, 1.8, 1.8, 1.0)
        _EdgeIntensity ("Glow Boost Intensity", Range(1, 10)) = 2.5

        [Header(Noise and Sweep)]
        _NoiseScale ("Noise Frequency Scale", Float) = 2.5
        _NoiseSpeed ("Noise Pan Speed", Float) = 0.0
        _NoiseOffset ("Noise Offset (Random Seed)", Vector) = (0, 0, 0, 0)
        [Toggle] _UseScreenSpace ("Use Screen-Space (Unified Across Panel)", Float) = 1.0
        _PanelRect ("Panel Rect Bounds", Vector) = (-1000, -1000, 1000, 1000)

        // 0=Random, 1=LeftToRight (Video Match), 2=RightToLeft, 3=TopToBottom, 4=BottomToTop, 5=CenterToOutside, 6=OutsideToCenter
        _DissolveDirection ("Direction Mode (0..6)", Float) = 1.0
        _DirectionInfluence ("Direction Influence vs Noise", Range(0, 1)) = 0.75
        _DissolveSoftness ("Dissolve Softness", Range(0.001, 0.2)) = 0.02

        [Header(Unity UI Stencil and Masking)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            Name "UIDissolveDefault"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex         : SV_POSITION;
                fixed4 color          : COLOR;
                float2 texcoord       : TEXCOORD0;
                float4 worldPosition  : TEXCOORD1;
                float4 screenPos      : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            float _DissolveAmount;
            float _DisintegrationWidth;
            float _GrainSize;
            float _DriftAmount;
            float _SparkleIntensity;

            float _UseUIColor;
            half4 _EdgeColor;
            half4 _InnerEdgeColor;
            float _EdgeIntensity;

            float _NoiseScale;
            float _NoiseSpeed;
            float4 _NoiseOffset;
            float _UseScreenSpace;
            float4 _PanelRect;

            float _DissolveDirection;
            float _DirectionInfluence;
            float _DissolveSoftness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                OUT.screenPos = ComputeScreenPos(OUT.vertex);

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Sample original UI Sprite / Texture
                half4 baseTexColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                baseTexColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(baseTexColor.a - 0.001);
                #endif

                // If fully transparent, discard early
                if (baseTexColor.a <= 0.001)
                {
                    discard;
                }

                // If dissolve amount is 0, render completely untainted original UI
                if (_DissolveAmount <= 0.0001)
                {
                    return baseTexColor;
                }

                // If dissolve amount is 1, completely vanish
                if (_DissolveAmount >= 0.9999)
                {
                    discard;
                }

                // 2. Coordinate calculation (Screen-Space for unified whole-popup continuity)
                float2 sPos = IN.screenPos.xy / max(IN.screenPos.w, 0.0001);
                float2 coord01;
                float2 noiseUV;

                if (_UseScreenSpace > 0.5)
                {
                    coord01 = sPos;
                    float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                    noiseUV = float2(sPos.x * aspect, sPos.y) * _NoiseScale;
                }
                else
                {
                    float2 panelSize = max(_PanelRect.zw - _PanelRect.xy, float2(0.001, 0.001));
                    coord01 = saturate((IN.worldPosition.xy - _PanelRect.xy) / panelSize);
                    noiseUV = coord01 * _NoiseScale;
                }

                noiseUV += _NoiseOffset.xy + float2(_Time.y * _NoiseSpeed, _Time.y * _NoiseSpeed * 0.7);

                // 3. Multi-octave organic wave noise
                half4 noiseSampleA = tex2D(_NoiseTex, noiseUV);
                half4 noiseSampleB = tex2D(_NoiseTex, noiseUV * 2.87 + float2(0.41, 0.73));
                half organicNoise = lerp(noiseSampleA.r * 0.7 + noiseSampleA.a * 0.3, noiseSampleB.g * 0.6 + noiseSampleB.b * 0.4, 0.3);

                // 4. Directional Sweep: Matches Shader.mp4 (starts top-left, sweeps across to bottom-right)
                int dirMode = (int)(_DissolveDirection + 0.1);
                float sweepCoord = 0.5;

                if (dirMode == 1) // Left -> Right (Video match: 80% left-to-right, 20% top-to-bottom)
                {
                    sweepCoord = coord01.x * 0.82 + (1.0 - coord01.y) * 0.18;
                }
                else if (dirMode == 2) // Right -> Left
                {
                    sweepCoord = (1.0 - coord01.x) * 0.82 + (1.0 - coord01.y) * 0.18;
                }
                else if (dirMode == 3) // Top -> Bottom
                {
                    sweepCoord = (1.0 - coord01.y);
                }
                else if (dirMode == 4) // Bottom -> Top
                {
                    sweepCoord = coord01.y;
                }
                else if (dirMode == 5) // Center -> Outside
                {
                    float2 centered = (coord01 - float2(0.5, 0.5)) * 2.0;
                    sweepCoord = saturate(length(centered));
                }
                else if (dirMode == 6) // Outside -> Center
                {
                    float2 centered = (coord01 - float2(0.5, 0.5)) * 2.0;
                    sweepCoord = 1.0 - saturate(length(centered));
                }
                else // 0 = Random
                {
                    sweepCoord = organicNoise;
                }

                // Blend sweep with organic wave
                float sweepFront;
                if (dirMode == 0)
                {
                    sweepFront = organicNoise;
                }
                else
                {
                    sweepFront = lerp(organicNoise, sweepCoord * 0.75 + organicNoise * 0.25, _DirectionInfluence);
                }

                // 5. Sweep Progression Range
                float bandWidth = max(_DisintegrationWidth, 0.05);
                float sweepProgress = _DissolveAmount * (1.0 + bandWidth * 1.6) - (bandWidth * 0.3);

                // Distance from cutting edge
                float dist = sweepFront - sweepProgress;

                // A. Ahead of wave: 100% Solid original UI
                if (dist > bandWidth)
                {
                    return baseTexColor;
                }

                // B. Behind wave: 100% Dissolved
                if (dist <= 0.0)
                {
                    discard;
                }

                // C. IN THE DISINTEGRATION / STARDUST WAVE (0.0 <= dist <= bandWidth)
                // t goes from 0.0 (just starting to break apart) to 1.0 (about to vanish)
                float t = 1.0 - (dist / bandWidth);

                // 6. Micro-Grain / Stardust Lattice (Exact match to Telegram / Video patch)
                float2 pixelCoord = floor((sPos * _ScreenParams.xy) / max(_GrainSize, 0.5));

                // High-frequency pseudo-random hashes per grain
                float grainHash = frac(sin(dot(pixelCoord, float2(12.9898, 78.233))) * 43758.5453);
                float grainHash2 = frac(sin(dot(pixelCoord + float2(37.12, 89.41), float2(269.5, 183.3))) * 23421.631);
                float grainHash3 = frac(sin(dot(pixelCoord + float2(113.7, 47.9), float2(419.2, 371.9))) * 54321.123);

                // Grain survival curve: drops from 1.0 down to 0.0
                // As t advances, more and more grains disintegrate into empty space
                float survivalThreshold = pow(saturate(1.0 - t), 1.55);

                if (grainHash > survivalThreshold)
                {
                    discard; // Grain has disintegrated into thin air!
                }

                // 7. Particle Drift / Updraft Displacement (Floating into the air)
                float driftT = pow(t, 1.4) * _DriftAmount;
                float2 driftUV = float2(
                    (grainHash2 - 0.45) * 0.025,        // Random horizontal air turbulence
                    (grainHash3 * 0.65 + 0.35) * 0.045   // Upward air drift
                ) * driftT;

                float2 sampledUV = IN.texcoord - driftUV;
                half4 sourceColor = (tex2D(_MainTex, sampledUV) + _TextureSampleAdd) * IN.color;
                if (sourceColor.a < 0.01)
                {
                    sourceColor = baseTexColor;
                }

                // 8. Color of Disintegration: "MÀU THÌ TÙY VÀO MÀU CỦA UI MÀ TAN BIẾN CÓ MÀU NHƯ VẬY"
                half3 finalRGB = sourceColor.rgb;
                float lum = dot(finalRGB, half3(0.299, 0.587, 0.114));

                // Stardust Sparkle & Twinkle
                float twinkle = sin(_Time.y * 28.0 + grainHash * 6.2831) * 0.5 + 0.5;
                float sparkleGlint = pow(grainHash2, 5.0) * _SparkleIntensity * (0.8 + twinkle * 0.6);

                if (_UseUIColor > 0.5)
                {
                    // 99% MATCH TO VIDEO:
                    // Each grain keeps its original UI color (text=white, button=pink/purple, coin=yellow, bg=dark teal)
                    // plus an emission sparkle boost to look like glittering stardust!
                    finalRGB = finalRGB * (1.0 + sparkleGlint * 2.8) + (sparkleGlint * 0.4 * lum);

                    // At the leading fracture boundary (t < 0.18), add subtle energy activation
                    float edgeFlash = smoothstep(0.0, 0.10, t) * smoothstep(0.25, 0.10, t);
                    finalRGB += finalRGB * edgeFlash * 0.65;
                }
                else
                {
                    // Fallback: Blend with custom edge color
                    half3 glowTint = lerp(_EdgeColor.rgb, _InnerEdgeColor.rgb, pow(t, 2.0));
                    finalRGB = lerp(finalRGB, glowTint * _EdgeIntensity, t);
                }

                // 9. Alpha Decay as particles fade into the air
                float alphaFade = saturate(1.0 - t * 0.85);
                return half4(finalRGB, sourceColor.a * alphaFade);
            }
        ENDCG
        }
    }

    FallBack "UI/Default"
}
