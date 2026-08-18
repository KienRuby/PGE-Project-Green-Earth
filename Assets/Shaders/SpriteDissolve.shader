Shader "Custom/2D/SpriteDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _NoiseTex ("Dissolve Noise Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0.0
        _EdgeWidth ("Edge Width", Range(0.001, 0.5)) = 0.06
        [HDR] _EdgeColor ("Edge Color (HDR)", Color) = (2.5, 0.8, 0.1, 1.0)
        _EdgeIntensity ("Edge Glow Intensity", Range(0, 10)) = 1.0
        _NoiseScale ("Noise Scale (if UV tiled)", Float) = 1.0

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
            #pragma vertex SpriteVert
            #pragma fragment SpriteDissolveFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _DissolveAmount;
            float _EdgeWidth;
            fixed4 _EdgeColor;
            float _EdgeIntensity;
            float _NoiseScale;

            // Simple 2D procedural noise fallback if _NoiseTex is missing or pure white
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float proceduralNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float getNoiseValue(float2 uv)
            {
                float4 texSample = tex2D(_NoiseTex, uv * _NoiseTex_ST.xy * max(0.001, _NoiseScale) + _NoiseTex_ST.zw);
                float n = texSample.r;

                // Fallback to smooth procedural noise if default white texture is used
                if (n >= 0.999 && texSample.g >= 0.999 && texSample.b >= 0.999)
                {
                    n = proceduralNoise(uv * 14.0 * max(0.1, _NoiseScale));
                }

                return n;
            }

            fixed4 SpriteDissolveFrag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                #if defined(PIXELSNAP_ON)
                IN.vertex = UnityPixelSnap(IN.vertex);
                #endif

                // Discard fully transparent pixels
                if (c.a <= 0.001)
                {
                    discard;
                }

                if (_DissolveAmount > 0.0)
                {
                    float noiseVal = getNoiseValue(IN.texcoord);

                    // Step 1: Discard pixels that are completely dissolved
                    clip(noiseVal - _DissolveAmount);

                    // Step 2: Edge Burning / Cyber Disintegration glow effect
                    float edgeDist = noiseVal - _DissolveAmount;
                    if (edgeDist < _EdgeWidth)
                    {
                        float edgeFactor = 1.0 - saturate(edgeDist / max(_EdgeWidth, 0.0001));
                        float intensity = max(0.0, _EdgeIntensity);

                        // Lerp sprite color towards emissive edge color
                        c.rgb = lerp(c.rgb, _EdgeColor.rgb * intensity, edgeFactor);

                        // Boost HDR emission at the immediate leading burn edge
                        c.rgb += _EdgeColor.rgb * pow(edgeFactor, 2.0) * _EdgeColor.a * intensity;
                    }
                }

                // Unity Sprite Pre-multiplied Alpha
                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }

    Fallback "Sprites/Default"
}
