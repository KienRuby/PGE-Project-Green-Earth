Shader "PGE/UI/Chipset Red Shimmer"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Holographic Rainbow Sheen)]
        _HoloIntensity ("Holo Intensity", Range(0, 3)) = 1.45
        _HoloSpeed ("Holo Flow Speed", Float) = 0.26
        _HoloScale ("Holo Frequency", Float) = 0.95
        _HoloWaveFreq ("Holo Wave Frequency", Float) = 4.6
        _HoloWaveSpeed ("Holo Wave Speed", Float) = 1.1
        _HoloWaveAmp ("Holo Wave Amplitude", Range(0, 0.1)) = 0.022
        _HoloSheenIntensity ("Holo Sheen Boost", Range(0, 4)) = 1.4
        
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
            Name "ChipsetRedHolographicShimmer"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
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
            float4 _ClipRect;
            float4 _SpriteUVRect;

            float _HoloIntensity;
            float _HoloSpeed;
            float _HoloScale;
            float _HoloWaveFreq;
            float _HoloWaveSpeed;
            float _HoloWaveAmp;
            float _HoloSheenIntensity;
            // Được cập nhật bằng Time.unscaledTime từ UI để shader vẫn chạy
            // khi popup level-up tạm dừng gameplay bằng Time.timeScale = 0.
            float _ChipsetUnscaledTime;

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

            // 7 sắc cầu vồng đầy đủ với độ chuyển tiếp siêu mềm mại
            fixed3 Sample7ColorRainbow(float t)
            {
                fixed3 a = fixed3(0.50, 0.50, 0.50);
                fixed3 b = fixed3(0.50, 0.50, 0.50);
                fixed3 c = fixed3(1.00, 1.00, 1.00);
                fixed3 d = fixed3(0.00, 0.333, 0.667);
                return saturate(a + b * cos(6.2831853 * (c * t + d)));
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 localUv = (input.texcoord - _SpriteUVRect.xy) / _SpriteUVRect.zw;

                // 1. Sample texture gốc tại UV chuẩn xác (bảo toàn 100% độ nét của viền)
                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;

                float uiClip = 1.0;
                #ifdef UNITY_UI_CLIP_RECT
                uiClip = UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                color.a *= uiClip;
                #endif

                // Nhận diện vùng bề mặt thẻ màu đỏ (lòng thẻ chipset)
                float redMask = saturate((color.r - max(color.g, color.b)) * 3.8) * color.a;

                // 2. Chuyển đổi tọa độ quét nghiêng 45 độ êm ái (Diagonal Smooth Flow)
                float2 centeredUv = localUv - 0.5;
                float diagCoord = (centeredUv.x * 0.7071 - centeredUv.y * 0.7071);
                float orthCoord = (centeredUv.x * 0.7071 + centeredUv.y * 0.7071);

                // 3. Hiệu ứng sóng lượn tơ lụa siêu mềm mại (Silky Gentle Undulation)
                float waveMotion = sin(orthCoord * _HoloWaveFreq - _ChipsetUnscaledTime * _HoloWaveSpeed) * _HoloWaveAmp
                                 + cos(localUv.x * 8.0 + _ChipsetUnscaledTime * 1.1) * (_HoloWaveAmp * 0.35);

                // 4. Tọa độ dòng chảy Hologram 7 sắc cầu vồng trôi êm đềm
                float holoT = (diagCoord * _HoloScale + waveMotion) - (_ChipsetUnscaledTime * _HoloSpeed);
                fixed3 rainbow7 = Sample7ColorRainbow(holoT);

                // 5. Vệt ánh kim khuếch tán dịu nhẹ (Soft Prismatic Sheen)
                float sheenBand = pow(saturate(0.5 + 0.5 * sin(holoT * 6.2831853)), 2.6);
                fixed3 rainbowSheen = rainbow7 * (0.28 + sheenBand * _HoloSheenIntensity * 0.75);

                // 6. HÒA TRỘN MỀM MẠI: ĐỎ CHỦ ĐẠO SÂU LẮNG (70%) + CẦU VỒNG 7 SẮC ÓNG Ả (30%)
                fixed3 dominantRedBase = fixed3(0.92, 0.06, 0.12) * max(color.r, 0.90);
                fixed3 blendedHolo = dominantRedBase * 0.70 + rainbowSheen * (color.r * 0.65 + 0.20) * _HoloIntensity;
                color.rgb = lerp(color.rgb, blendedHolo, redMask);

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

