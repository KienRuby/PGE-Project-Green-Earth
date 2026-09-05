Shader "Custom/UI/UIDissolve_TMP"
{
    Properties
    {
        [HDR]_FaceColor     ("Face Color", Color) = (1,1,1,1)
        _FaceDilate         ("Face Dilate", Range(-1,1)) = 0

        [HDR]_OutlineColor  ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth       ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness    ("Outline Softness", Range(0,1)) = 0

        [HDR]_UnderlayColor ("Border Color", Color) = (0,0,0,.5)
        _UnderlayOffsetX    ("Border OffsetX", Range(-1,1)) = 0
        _UnderlayOffsetY    ("Border OffsetY", Range(-1,1)) = 0
        _UnderlayDilate     ("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness   ("Border Softness", Range(0,1)) = 0

        _WeightNormal       ("Weight Normal", float) = 0
        _WeightBold         ("Weight Bold", float) = .5

        _ShaderFlags        ("Flags", float) = 0
        _ScaleRatioA        ("Scale RatioA", float) = 1
        _ScaleRatioB        ("Scale RatioB", float) = 1
        _ScaleRatioC        ("Scale RatioC", float) = 1

        _MainTex            ("Font Atlas", 2D) = "white" {}
        _TextureWidth       ("Texture Width", float) = 512
        _TextureHeight      ("Texture Height", float) = 512
        _GradientScale      ("Gradient Scale", float) = 5
        _ScaleX             ("Scale X", float) = 1
        _ScaleY             ("Scale Y", float) = 1
        _PerspectiveFilter  ("Perspective Correction", Range(0, 1)) = 0.875
        _Sharpness          ("Sharpness", Range(-1,1)) = 0

        _VertexOffsetX      ("Vertex OffsetX", float) = 0
        _VertexOffsetY      ("Vertex OffsetY", float) = 0

        _ClipRect           ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
        _MaskSoftnessX      ("Mask SoftnessX", float) = 0
        _MaskSoftnessY      ("Mask SoftnessY", float) = 0

        _StencilComp        ("Stencil Comparison", Float) = 8
        _Stencil            ("Stencil ID", Float) = 0
        _StencilOp          ("Stencil Operation", Float) = 0
        _StencilWriteMask   ("Stencil Write Mask", Float) = 255
        _StencilReadMask    ("Stencil Read Mask", Float) = 255

        _CullMode           ("Cull Mode", Float) = 0
        _ColorMask          ("Color Mask", Float) = 15

        [Header(Dissolve and Stardust Sandification)]
        _NoiseTex           ("Dissolve Noise Texture", 2D) = "white" {}
        _DissolveAmount     ("Dissolve Progress", Range(0, 1)) = 0.0
        _DisintegrationWidth ("Disintegration Width (Sand Zone)", Range(0.05, 0.5)) = 0.22
        _GrainSize          ("Grain / Stardust Size (Pixels)", Range(0.5, 4.0)) = 1.8
        _DriftAmount        ("Drift / Dispersion Amount", Range(0.0, 3.0)) = 0.85
        _SparkleIntensity   ("Stardust Sparkle Glint", Range(0.0, 5.0)) = 2.2

        [Header(Color and Glow Mode)]
        [Toggle] _UseUIColor ("Use UI Color (Adaptive)", Float) = 1.0
        [HDR] _EdgeColor    ("Outer Glow Corona (HDR)", Color) = (0.3, 0.9, 1.0, 1.0)
        [HDR] _InnerEdgeColor ("Inner Core Flash (HDR)", Color) = (1.8, 1.8, 1.8, 1.0)
        _EdgeIntensity      ("Glow Boost Intensity", Range(1, 10)) = 2.5

        [Header(Noise and Sweep)]
        _NoiseScale         ("Noise Frequency Scale", Float) = 2.5
        _NoiseSpeed         ("Noise Pan Speed", Float) = 0.0
        _NoiseOffset        ("Noise Offset (Random Seed)", Vector) = (0, 0, 0, 0)
        [Toggle] _UseScreenSpace ("Use Screen-Space", Float) = 1.0
        _PanelRect          ("Panel Rect Bounds", Vector) = (-1000, -1000, 1000, 1000)

        _DissolveDirection  ("Direction Mode (0..6)", Float) = 1.0
        _DirectionInfluence ("Direction Influence vs Noise", Range(0, 1)) = 0.75
        _DissolveSoftness   ("Dissolve Softness", Range(0.001, 0.2)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        ZWrite Off
        Lighting Off
        Fog { Mode Off }
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
        CGPROGRAM
            #pragma vertex VertShader
            #pragma fragment PixShader
            #pragma shader_feature __ OUTLINE_ON
            #pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc"

            struct vertex_t
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex       : POSITION;
                float3 normal       : NORMAL;
                fixed4 color        : COLOR;
                float2 texcoord0    : TEXCOORD0;
                float2 texcoord1    : TEXCOORD1;
            };

            struct pixel_t
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                float4 vertex       : SV_POSITION;
                fixed4 faceColor    : COLOR;
                fixed4 outlineColor : COLOR1;
                float4 texcoord0    : TEXCOORD0;
                half4 param         : TEXCOORD1;
                half4 mask          : TEXCOORD2;
                float4 screenPos    : TEXCOORD3;
                float4 worldPos     : TEXCOORD4;
                #if (UNDERLAY_ON | UNDERLAY_INNER)
                float4 texcoord1    : TEXCOORD5;
                half2 underlayParam : TEXCOORD6;
                #endif
            };

            sampler2D _NoiseTex;
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

            pixel_t VertShader(vertex_t input)
            {
                pixel_t output;

                UNITY_INITIALIZE_OUTPUT(pixel_t, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float bold = step(input.texcoord1.y, 0);

                float4 vert = input.vertex;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;
                float4 vPosition = UnityObjectToClipPos(vert);

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float scale = rsqrt(dot(pixelSize, pixelSize));
                scale *= abs(input.texcoord1.y) * _GradientScale * (_Sharpness + 1);
                if (UNITY_MATRIX_P[3][3] == 0) scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(input.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

                float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
                weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

                float layerScale = scale;

                scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
                float bias = (0.5 - weight) * scale - 0.5;
                float outline = _OutlineWidth * _ScaleRatioA * 0.5 * scale;

                float opacity = input.color.a;
                #if (UNDERLAY_ON | UNDERLAY_INNER)
                opacity = 1.0;
                #endif

                fixed4 faceColor = fixed4(input.color.rgb, opacity) * _FaceColor;
                faceColor.rgb *= faceColor.a;

                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= opacity;
                outlineColor.rgb *= outlineColor.a;
                outlineColor = lerp(faceColor, outlineColor, sqrt(min(1.0, (outline * 2))));

                #if (UNDERLAY_ON | UNDERLAY_INNER)
                layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
                float layerBias = (.5 - weight) * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);

                float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
                float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
                float2 layerOffset = float2(x, y);
                #endif

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

                output.vertex = vPosition;
                output.faceColor = faceColor;
                output.outlineColor = outlineColor;
                output.texcoord0 = float4(input.texcoord0.x, input.texcoord0.y, maskUV.x, maskUV.y);
                output.param = half4(scale, bias - outline, bias + outline, bias);
                output.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_MaskSoftnessX, _MaskSoftnessY) + pixelSize.xy));
                output.screenPos = ComputeScreenPos(vPosition);
                output.worldPos = vert;

                #if (UNDERLAY_ON || UNDERLAY_INNER)
                output.texcoord1 = float4(input.texcoord0 + layerOffset, input.color.a, 0);
                output.underlayParam = half2(layerScale, layerBias);
                #endif

                return output;
            }

            fixed4 PixShader(pixel_t input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half d = tex2D(_MainTex, input.texcoord0.xy).a * input.param.x;
                half4 c = input.faceColor * saturate(d - input.param.w);

                #ifdef OUTLINE_ON
                c = lerp(input.outlineColor, input.faceColor, saturate(d - input.param.z));
                c *= saturate(d - input.param.y);
                #endif

                #if UNDERLAY_ON
                d = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
                c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate(d - input.underlayParam.y) * (1 - c.a);
                #endif

                #if UNDERLAY_INNER
                half sd = saturate(d - input.param.z);
                d = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
                c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * (1 - saturate(d - input.underlayParam.y)) * sd * (1 - c.a);
                #endif

                #if UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                c *= m.x * m.y;
                #endif

                #if (UNDERLAY_ON | UNDERLAY_INNER)
                c *= input.texcoord1.z;
                #endif

                #if UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif

                if (c.a <= 0.001)
                {
                    discard;
                }

                if (_DissolveAmount <= 0.0001)
                {
                    return c;
                }

                if (_DissolveAmount >= 0.9999)
                {
                    discard;
                }

                // --- Stardust Disintegration Calculation (Matching UIDissolve.shader & Video) ---
                float2 sPos = input.screenPos.xy / max(input.screenPos.w, 0.0001);
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
                    coord01 = saturate((input.worldPos.xy - _PanelRect.xy) / panelSize);
                    noiseUV = coord01 * _NoiseScale;
                }

                noiseUV += _NoiseOffset.xy + float2(_Time.y * _NoiseSpeed, _Time.y * _NoiseSpeed * 0.7);

                half4 noiseSampleA = tex2D(_NoiseTex, noiseUV);
                half4 noiseSampleB = tex2D(_NoiseTex, noiseUV * 2.87 + float2(0.41, 0.73));
                half organicNoise = lerp(noiseSampleA.r * 0.7 + noiseSampleA.a * 0.3, noiseSampleB.g * 0.6 + noiseSampleB.b * 0.4, 0.3);

                int dirMode = (int)(_DissolveDirection + 0.1);
                float sweepCoord = 0.5;

                if (dirMode == 1) // Left -> Right (Video match: 80% left-to-right, 20% top-to-bottom)
                {
                    sweepCoord = coord01.x * 0.82 + (1.0 - coord01.y) * 0.18;
                }
                else if (dirMode == 2)
                {
                    sweepCoord = (1.0 - coord01.x) * 0.82 + (1.0 - coord01.y) * 0.18;
                }
                else if (dirMode == 3)
                {
                    sweepCoord = (1.0 - coord01.y);
                }
                else if (dirMode == 4)
                {
                    sweepCoord = coord01.y;
                }
                else if (dirMode == 5)
                {
                    float2 centered = (coord01 - float2(0.5, 0.5)) * 2.0;
                    sweepCoord = saturate(length(centered));
                }
                else if (dirMode == 6)
                {
                    float2 centered = (coord01 - float2(0.5, 0.5)) * 2.0;
                    sweepCoord = 1.0 - saturate(length(centered));
                }
                else
                {
                    sweepCoord = organicNoise;
                }

                float sweepFront;
                if (dirMode == 0)
                {
                    sweepFront = organicNoise;
                }
                else
                {
                    sweepFront = lerp(organicNoise, sweepCoord * 0.75 + organicNoise * 0.25, _DirectionInfluence);
                }

                float bandWidth = max(_DisintegrationWidth, 0.05);
                float sweepProgress = _DissolveAmount * (1.0 + bandWidth * 1.6) - (bandWidth * 0.3);
                float dist = sweepFront - sweepProgress;

                // Ahead of wave: solid text
                if (dist > bandWidth)
                {
                    return c;
                }

                // Behind wave: dissolved
                if (dist <= 0.0)
                {
                    discard;
                }

                // In the stardust wave:
                float t = 1.0 - (dist / bandWidth);

                float2 pixelCoord = floor((sPos * _ScreenParams.xy) / max(_GrainSize, 0.5));
                float grainHash = frac(sin(dot(pixelCoord, float2(12.9898, 78.233))) * 43758.5453);
                float grainHash2 = frac(sin(dot(pixelCoord + float2(37.12, 89.41), float2(269.5, 183.3))) * 23421.631);

                float survivalThreshold = pow(saturate(1.0 - t), 1.55);
                if (grainHash > survivalThreshold)
                {
                    discard;
                }

                // Color of text dust inherits text color (white, yellow, etc.) with sparkle
                float twinkle = sin(_Time.y * 28.0 + grainHash * 6.2831) * 0.5 + 0.5;
                float sparkleGlint = pow(grainHash2, 5.0) * _SparkleIntensity * (0.8 + twinkle * 0.6);
                float lum = dot(c.rgb, half3(0.299, 0.587, 0.114));

                if (_UseUIColor > 0.5)
                {
                    c.rgb = c.rgb * (1.0 + sparkleGlint * 2.8) + (sparkleGlint * 0.4 * lum);
                    float edgeFlash = smoothstep(0.0, 0.10, t) * smoothstep(0.25, 0.10, t);
                    c.rgb += c.rgb * edgeFlash * 0.65;
                }
                else
                {
                    half3 glowTint = lerp(_EdgeColor.rgb, _InnerEdgeColor.rgb, pow(t, 2.0));
                    c.rgb = lerp(c.rgb, glowTint * _EdgeIntensity, t);
                }

                float alphaFade = saturate(1.0 - t * 0.85);
                c *= alphaFade;

                return c;
            }
        ENDCG
        }
    }

    CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}
