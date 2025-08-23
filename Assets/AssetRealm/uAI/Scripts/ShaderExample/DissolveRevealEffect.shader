Shader "Custom/DissolveRevealEffect"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _SecondaryTex ("Secondary Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0, 0.1)) = 0.01
        _EdgeColor ("Edge Color", Color) = (1, 0.5, 0, 1)
        _NoiseScale ("Noise Scale", Range(0.1, 50)) = 10
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SecondaryTex);
            SAMPLER(sampler_SecondaryTex);
            
            float _DissolveAmount;
            float _EdgeWidth;
            float4 _EdgeColor;
            float _NoiseScale;
            float _NoiseSpeed;
            float4 _MainTex_ST;

            // Simplex noise function
            float2 random2(float2 st)
            {
                st = float2(dot(st, float2(127.1, 311.7)),
                            dot(st, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
            }

            float simplex2D(float2 p)
            {
                const float K1 = 0.366025404; // (sqrt(3)-1)/2
                const float K2 = 0.211324865; // (3-sqrt(3))/6
                
                float2 i = floor(p + (p.x + p.y) * K1);
                float2 a = p - i + (i.x + i.y) * K2;
                float m = step(a.y, a.x); 
                float2 o = float2(m, 1.0 - m);
                float2 b = a - o + K2;
                float2 c = a - 1.0 + 2.0 * K2;
                
                float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
                float3 n = h * h * h * h * float3(dot(a, random2(i)), 
                                                 dot(b, random2(i + o)), 
                                                 dot(c, random2(i + 1.0)));
                return dot(n, float3(70.0, 70.0, 70.0));
            }

            // Fractal Brownian Motion
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < 5; i++)
                {
                    value += amplitude * simplex2D(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value * 0.5 + 0.5; // Normalize to 0-1 range
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Generate noise
                float2 noiseUV = input.uv * _NoiseScale + float2(_Time.y * _NoiseSpeed, 0);
                float noise = fbm(noiseUV);
                
                // Sample textures
                float4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 secondaryColor = SAMPLE_TEXTURE2D(_SecondaryTex, sampler_SecondaryTex, input.uv);
                
                // Calculate dissolve edge
                float dissolveThreshold = _DissolveAmount;
                float edgeDistance = noise - dissolveThreshold;
                
                // Apply edge color
                float edgeLerp = 1 - saturate(edgeDistance / _EdgeWidth);
                float isEdge = edgeLerp * step(0, edgeDistance);
                
                // Blend between main texture, edge color, and secondary texture
                float4 finalColor;
                if (noise < dissolveThreshold)
                {
                    finalColor = secondaryColor;
                }
                else
                {
                    finalColor = lerp(mainColor, _EdgeColor, isEdge);
                }
                
                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}