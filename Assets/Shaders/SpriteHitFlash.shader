Shader "Custom/2D/SpriteHitFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,0,0,1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0.0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex CustomSpriteVert
            #pragma fragment CustomSpriteHitFlashFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _FlashColor)
                UNITY_DEFINE_INSTANCED_PROP(fixed, _FlashAmount)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata_flash
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_flash
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_flash CustomSpriteVert(appdata_flash IN)
            {
                v2f_flash OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
                OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 CustomSpriteHitFlashFrag(v2f_flash IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                fixed4 c = SampleSpriteTexture(IN.texcoord);
                
                fixed4 flashCol = UNITY_ACCESS_INSTANCED_PROP(Props, _FlashColor);
                fixed flashAmt = UNITY_ACCESS_INSTANCED_PROP(Props, _FlashAmount);

                // Nếu không có instancing buffer hợp lệ hoặc flashCol.a == 0, fallback về đỏ mặc định
                if (flashCol.a <= 0.001f)
                {
                    flashCol = fixed4(1, 0, 0, 1);
                }

                // Khi FlashAmount = 0: Trả về ảnh sprite gốc nhân vertex color (bình thường 100%)
                // Khi FlashAmount > 0: Phủ màu ĐỎ TƯƠI RỰC RỠ lên toàn bộ sprite theo silhouette alpha
                fixed4 baseColor = c * IN.color;
                baseColor.rgb *= baseColor.a;

                fixed3 flashRgb = flashCol.rgb * c.a * IN.color.a;
                fixed3 finalRgb = lerp(baseColor.rgb, flashRgb, saturate(flashAmt));

                return fixed4(finalRgb, baseColor.a);
            }
        ENDCG
        }
    }
    Fallback "Sprites/Default"
}
