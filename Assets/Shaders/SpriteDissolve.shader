Shader "Custom/2D/SpriteDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _NoiseTex ("Dissolve Noise Texture", 2D) = "white" {}
        
        // --- Erosion and Progression ---
        _DissolveAmount ("Erosion Progress", Range(0, 1)) = 0.0
        _DissolveDirectionMode ("Erosion Direction (0=BottomToTop, 1=TopToBottom, 2=CenterOutward, 3=LeftToRight, 4=UniformNoise)", Float) = 2.0
        _NoiseScale ("Simplex Noise Scale", Float) = 3.0
        _EdgeWidth ("Erosion Edge Width", Range(0.001, 0.5)) = 0.12

        // --- Ultra Dazzling HDR Glowing Emission ---
        [HDR] _EdgeColor ("Molten Edge Corona (HDR)", Color) = (5.5, 2.8, 0.4, 1.0)
        [HDR] _InnerEdgeColor ("Searing White-Hot Core (HDR)", Color) = (10.0, 8.0, 4.5, 1.0)
        _EdgeIntensity ("Emission & Bloom Intensity", Range(0, 20)) = 2.8
        _SupernovaFlash ("Initial Supernova Shockwave Intensity", Range(0, 5)) = 2.5

        // --- 360 Degree Particle Burst and Glittering Flares ---
        _ParticleShapeMode ("Particle Shape (0=4Star Flare, 1=Hexagon Pixel, 2=Ash Flake, 3=Diamond Spark, 4=Circle Stardust)", Float) = 0.0
        _ParticleGridSize ("Particle Density / Grid Count", Float) = 60.0
        _DisperseSpeed ("360 Radial Burst Speed", Float) = 1.8
        _RadialBurstSpread ("Radial Outward Spread", Float) = 1.4
        _UpwardDrift ("Anti-Gravity Upward Drift", Float) = 0.5
        _SwirlStrength ("Turbulent Swirl Vortex", Float) = 1.0
        _DisperseChaos ("Chaos & Turbulence", Float) = 1.3
        _ParticleShrink ("Size Over Lifetime Decay", Range(0, 1)) = 0.82
        _Gravity ("Downward Gravity Pull", Float) = 0.02
        _StarSparkleSpeed ("Glitter Twinkle Speed", Float) = 45.0
        _PrismaticShimmer ("Iridescent Prismatic Shimmer", Range(0, 1)) = 0.5
        _HaloGlowIntensity ("Star Aura Halo Glow", Range(0, 2)) = 0.85

        // --- Per Renderer Sub-Sprite Mapping ---
        [PerRendererData] _SpriteUVRect ("Sprite UV Rect in Atlas", Vector) = (0, 0, 1, 1)

        // --- Sprite Defaults ---
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
            #pragma vertex SpriteCustomVert
            #pragma fragment SpriteUltraGlitterDissolveFrag
            #pragma target 3.0
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            float _DissolveAmount;
            float _DissolveDirectionMode;
            float _NoiseScale;
            float _EdgeWidth;

            half4 _EdgeColor;
            half4 _InnerEdgeColor;
            float _EdgeIntensity;
            float _SupernovaFlash;

            float _ParticleShapeMode;
            float _ParticleGridSize;
            float _DisperseSpeed;
            float _RadialBurstSpread;
            float _UpwardDrift;
            float _SwirlStrength;
            float _DisperseChaos;
            float _ParticleShrink;
            float _Gravity;
            float _StarSparkleSpeed;
            float _PrismaticShimmer;
            float _HaloGlowIntensity;

            float4 _SpriteUVRect;

            struct appdata_custom
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_custom
            {
                float4 vertex       : SV_POSITION;
                fixed4 color        : COLOR;
                float2 texcoord     : TEXCOORD0;
                float2 localExpand  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // =========================================================================
            // HASH & SIMPLEX FBM PROCEDURAL NOISE
            // =========================================================================
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(443.897, 441.423, 437.195));
                p3 += dot(p3, p3.yzx + 19.19);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            float rawNoise2D(float2 uv)
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

            float fbmSimplexNoise(float2 uv)
            {
                float n = 0.0;
                float amp = 0.55;
                float2 p = uv * max(0.1, _NoiseScale);

                n += rawNoise2D(p) * amp; p = p * 2.13 + float2(1.7, 9.2); amp *= 0.5;
                n += rawNoise2D(p) * amp; p = p * 2.07 + float2(8.3, 2.8); amp *= 0.5;
                n += rawNoise2D(p) * amp;

                return n / 0.86625;
            }

            float calculateDirectionalBias(float2 normUV)
            {
                int mode = (int)(_DissolveDirectionMode + 0.5);

                if (mode == 0) // Bottom-to-Top
                {
                    return normUV.y;
                }
                else if (mode == 1) // Top-to-Bottom
                {
                    return 1.0 - normUV.y;
                }
                else if (mode == 2) // Center-Outward 360 Radial Burst
                {
                    float2 centered = normUV - float2(0.5, 0.5);
                    return saturate(length(centered) * 1.4142);
                }
                else if (mode == 3) // Left-to-Right
                {
                    return normUV.x;
                }
                else // Uniform Simplex Noise
                {
                    return 0.5;
                }
            }

            float getErosionValue(float2 normUV)
            {
                float dirBias = calculateDirectionalBias(normUV);
                float noise = fbmSimplexNoise(normUV);

                if ((int)(_DissolveDirectionMode + 0.5) == 4)
                {
                    return noise;
                }

                return saturate(dirBias * 0.6 + noise * 0.4);
            }

            // =========================================================================
            // PARTICLE SHAPE EVALUATOR WITH STAR DIFFRACTION SPIKES & AURA HALO
            // =========================================================================
            float evaluateParticleSDF(float2 p, float shapeMode, float size, out float auraHalo)
            {
                int shape = (int)(shapeMode + 0.5);
                float distCenter = length(p);
                auraHalo = exp(-distCenter * 6.5) * _HaloGlowIntensity;

                if (shape == 0) // Sharp 4-Pointed Star Flare with Cross Beams
                {
                    float2 q = abs(p);
                    float starAstroid = sqrt(q.x) + sqrt(q.y);
                    float starBound = sqrt(max(0.001, size * 0.42));
                    float baseStar = starAstroid / max(0.001, starBound);

                    float spikeX = q.x * 0.08 + q.y * 4.5;
                    float spikeY = q.y * 0.08 + q.x * 4.5;
                    float crossSpikes = min(spikeX, spikeY) / max(0.001, size * 0.95);

                    return min(baseStar, crossSpikes);
                }
                else if (shape == 1) // Hexagonal Digital Pixel
                {
                    float2 q = abs(p);
                    float hex = max(q.x * 0.866025 + q.y * 0.5, q.y) * 2.0;
                    return hex / max(0.001, size);
                }
                else if (shape == 2) // Ash / Ember Flake
                {
                    float wobble = (rawNoise2D(p * 9.0) - 0.5) * 0.45;
                    return (distCenter * 2.0 + wobble) / max(0.001, size);
                }
                else if (shape == 3) // Diamond Gem Spark
                {
                    float2 q = abs(p);
                    return ((q.x + q.y) * 1.4142) / max(0.001, size);
                }
                else // Stardust Circle Droplet
                {
                    return (distCenter * 2.0) / max(0.001, size);
                }
            }

            // =========================================================================
            // CUSTOM VERTEX SHADER WITH BOUNDLESS QUAD EXPANSION
            // =========================================================================
            v2f_custom SpriteCustomVert(appdata_custom IN)
            {
                v2f_custom OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float progress = saturate(_DissolveAmount);
                float expand = 1.0 + progress * _RadialBurstSpread * 1.5;

                float4 expandedVertex = IN.vertex;
                expandedVertex.xy *= expand;

                OUT.vertex = UnityObjectToClipPos(expandedVertex);
                OUT.texcoord = IN.texcoord;

                float2 uvMin = _SpriteUVRect.xy;
                float2 uvSize = max(float2(0.0001, 0.0001), _SpriteUVRect.zw);
                float2 normBase = (IN.texcoord - uvMin) / uvSize;
                OUT.localExpand = (normBase - 0.5) * expand + 0.5;

                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            // =========================================================================
            // ULTRA-GLITTER FRAGMENT SHADER (1 SECOND 360 BURST)
            // =========================================================================
            fixed4 SpriteUltraGlitterDissolveFrag(v2f_custom IN) : SV_Target
            {
                float progress = saturate(_DissolveAmount);
                float2 uvMin = _SpriteUVRect.xy;
                float2 uvSize = max(float2(0.0001, 0.0001), _SpriteUVRect.zw);

                // --- 1. NORMAL UNERODED SPRITE ---
                if (progress <= 0.0001)
                {
                    half4 baseCol = SampleSpriteTexture(IN.texcoord) * IN.color;
                    if (baseCol.a <= 0.001) discard;
                    baseCol.rgb *= baseCol.a;
                    return baseCol;
                }

                float gridSize = max(12.0, _ParticleGridSize);
                float2 normUV = IN.localExpand;
                float2 centerVec = normUV - float2(0.5, 0.5);
                float distFromSpriteCenter = length(centerVec);

                // --- 2. MULTI-SCALE SIMPLEX EROSION & SUPERNOVA FLASH WAVE ---
                float erosionVal = getErosionValue(saturate(normUV));
                float distFromFront = erosionVal - progress;

                bool isInsideOriginalRect = (normUV.x >= 0.0 && normUV.x <= 1.0 && normUV.y >= 0.0 && normUV.y <= 1.0);

                if (distFromFront >= 0.0 && isInsideOriginalRect)
                {
                    float2 sampleUV = uvMin + saturate(normUV) * uvSize;
                    half4 cBody = SampleSpriteTexture(sampleUV) * IN.color;
                    if (cBody.a <= 0.001) discard;

                    if (_SupernovaFlash > 0.0 && progress < 0.35)
                    {
                        float shockRadius = progress * 2.5;
                        float shockDist = abs(distFromSpriteCenter - shockRadius);
                        float shockWave = exp(-shockDist * 14.0) * (1.0 - progress / 0.35) * _SupernovaFlash;
                        cBody.rgb += _InnerEdgeColor.rgb * shockWave * _EdgeIntensity;
                    }

                    if (distFromFront < _EdgeWidth)
                    {
                        float edgeFactor = 1.0 - saturate(distFromFront / max(0.001, _EdgeWidth));
                        float coreFactor = pow(edgeFactor, 3.2);
                        float glowIntensity = _EdgeIntensity * 1.4;

                        cBody.rgb = lerp(cBody.rgb, _EdgeColor.rgb * glowIntensity, edgeFactor);
                        cBody.rgb += _InnerEdgeColor.rgb * coreFactor * (glowIntensity * 1.8);
                    }

                    cBody.rgb *= cBody.a;
                    return cBody;
                }

                // --- 3. 360-DEGREE DAZZLING STAR EXPLOSION & SWIRL PHYSICS ---
                float2 cellId = floor(normUV * gridSize);
                float2 cellLocal = frac(normUV * gridSize) - 0.5;

                float2 rand2 = hash22(cellId);
                float randSpeed = hash21(cellId + 79.31);
                float randTwinkle = hash21(cellId + 17.59);
                float randHue = hash21(cellId + 93.41);

                float cellErosion = getErosionValue(saturate((cellId + 0.5) / gridSize));
                float particleLife = saturate((progress - cellErosion + 0.06) / 0.94);

                if (particleLife <= 0.0)
                {
                    discard;
                }

                float2 cellCenter = (cellId + 0.5) / gridSize;
                float2 radialDir = cellCenter - float2(0.5, 0.5);
                float rLen = length(radialDir);
                radialDir = rLen > 0.001 ? radialDir / rLen : float2(0.0, 1.0);

                float2 tangentDir = float2(-radialDir.y, radialDir.x) * _SwirlStrength * sin(particleLife * 3.5 + rand2.y * 6.28);
                float2 chaosNoise = (rand2 - 0.5) * 2.0 * _DisperseChaos;

                float2 velocity = (radialDir * _RadialBurstSpread + tangentDir + chaosNoise) * (_DisperseSpeed * (0.75 + 0.85 * randSpeed));
                velocity.y += _UpwardDrift * 1.25 - _Gravity * 0.3;

                float2 origNormCenter = cellCenter - velocity * particleLife;

                if (origNormCenter.x < 0.0 || origNormCenter.x > 1.0 || origNormCenter.y < 0.0 || origNormCenter.y > 1.0)
                {
                    discard;
                }

                float2 origUV = uvMin + origNormCenter * uvSize;
                half4 cStar = SampleSpriteTexture(origUV) * IN.color;
                if (cStar.a <= 0.01) discard;

                float spinAngle = (rand2.x - 0.5) * particleLife * 25.0;
                float s = sin(spinAngle);
                float c_ang = cos(spinAngle);
                float2 rotLocal = float2(
                    cellLocal.x * c_ang - cellLocal.y * s,
                    cellLocal.x * s + cellLocal.y * c_ang
                );

                float sizePeak = 1.0 + 0.35 * sin(particleLife * 3.14159);
                float currentSize = max(0.04, (1.0 - particleLife * _ParticleShrink) * sizePeak);

                float auraHalo = 0.0;
                float sdfRatio = evaluateParticleSDF(rotLocal, _ParticleShapeMode, currentSize, auraHalo);

                if (sdfRatio > 1.0 && auraHalo < 0.05)
                {
                    discard;
                }

                // =====================================================================
                // HIỆU ỨNG LUNG LINH: GLITTER TWINKLE & PRISMATIC IRIDESCENCE
                // =====================================================================
                float twinkleA = sin(particleLife * _StarSparkleSpeed + randTwinkle * 62.83);
                float twinkleB = cos(particleLife * (_StarSparkleSpeed * 1.45) + rand2.x * 31.41);
                float twinkle = 1.0 + 0.95 * max(twinkleA, twinkleB);

                float3 rainbowSpectrum = float3(
                    sin(particleLife * 7.0 + randHue * 6.28 + 0.0),
                    sin(particleLife * 7.0 + randHue * 6.28 + 2.09),
                    sin(particleLife * 7.0 + randHue * 6.28 + 4.18)
                ) * 0.5 + 0.5;

                float3 starCoronaColor = lerp(_EdgeColor.rgb, _EdgeColor.rgb * rainbowSpectrum * 1.6 + _InnerEdgeColor.rgb * 0.5, _PrismaticShimmer);

                float starEdge = 1.0 - saturate(sdfRatio);
                float starCore = pow(starEdge, 3.5);
                float finalEmissionIntensity = _EdgeIntensity * (1.3 + (1.0 - particleLife) * 1.1) * twinkle;

                cStar.rgb = lerp(cStar.rgb, starCoronaColor * finalEmissionIntensity, max(starEdge, particleLife * 0.65));
                cStar.rgb += _InnerEdgeColor.rgb * starCore * (finalEmissionIntensity * 2.0);
                cStar.rgb += starCoronaColor * auraHalo * (finalEmissionIntensity * 0.6);

                float shapeAlpha = smoothstep(1.0, 0.15, sdfRatio) + auraHalo * 0.7;
                float lifetimeFade = (1.0 - pow(particleLife, 1.8));
                cStar.a *= saturate(shapeAlpha * lifetimeFade);

                cStar.rgb *= cStar.a;
                return cStar;
            }
        ENDCG
        }
    }

    Fallback "Sprites/Default"
}
