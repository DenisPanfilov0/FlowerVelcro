Shader "Custom/RainbowFlowerShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SpeedX ("Horizontal Wave Speed", Range(0, 5)) = 1
        _SpeedY ("Vertical Wave Speed", Range(0, 5)) = 0.5
        _Intensity ("Color Intensity", Range(0, 1)) = 0.8
        _WaveFrequencyX ("Horizontal Wave Frequency", Range(0, 10)) = 3
        _WaveFrequencyY ("Vertical Wave Frequency", Range(0, 10)) = 2
        _WaveAmplitudeX ("Horizontal Wave Amplitude", Range(0, 0.5)) = 0.15
        _WaveAmplitudeY ("Vertical Wave Amplitude", Range(0, 0.5)) = 0.1
        _WaveOffsetX ("Horizontal Wave Offset", Range(-1, 1)) = 0
        _WaveOffsetY ("Vertical Wave Offset", Range(-1, 1)) = 0
        _ColorBlend ("Color Blend Factor", Range(0, 0.3)) = 0.1
        _SecondaryWaveStrength ("Secondary Wave Strength", Range(0, 0.5)) = 0.2
        _RedWidthFactor ("Red Band Width Factor", Range(0.1, 1)) = 0.5
        _NoiseScale ("Noise Scale", Range(1, 50)) = 10
        _NoiseAmplitude ("Noise Amplitude", Range(0, 0.5)) = 0.05
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 0.5
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float _AlphaSplitEnabled;
            fixed4 _Color;
            float _SpeedX;
            float _SpeedY;
            float _Intensity;
            float _WaveFrequencyX;
            float _WaveFrequencyY;
            float _WaveAmplitudeX;
            float _WaveAmplitudeY;
            float _WaveOffsetX;
            float _WaveOffsetY;
            float _ColorBlend;
            float _SecondaryWaveStrength;
            float _RedWidthFactor;
            float _NoiseScale;
            float _NoiseAmplitude;
            float _NoiseSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            // Simple hash function
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Simple 2D noise function
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i + float2(0,0));
                float b = hash(i + float2(1,0));
                float c = hash(i + float2(0,1));
                float d = hash(i + float2(1,1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                float2 uv = IN.texcoord;
                float timeX = _Time.y * _SpeedX;
                float timeY = _Time.y * _SpeedY;

                // Define seven vivid rainbow colors in order
                float3 rainbow[7];
                rainbow[0] = float3(1, 0, 0);    // Red
                rainbow[1] = float3(1, 0.5, 0);  // Orange
                rainbow[2] = float3(1, 1, 0);    // Yellow
                rainbow[3] = float3(0, 1, 0);    // Green
                rainbow[4] = float3(0, 1, 1);    // Cyan
                rainbow[5] = float3(0, 0, 1);    // Blue
                rainbow[6] = float3(0.5, 0, 0.5); // Purple

                // Add random noise to UV for shimmering effect
                float noiseValue = noise(uv * _NoiseScale + _Time.y * _NoiseSpeed) * _NoiseAmplitude;

                // Primary wave: horizontal flow with vertical oscillation and noise
                float waveX = sin((uv.x + timeX + _WaveOffsetX + noiseValue) * _WaveFrequencyX + 
                                cos((uv.y + timeY + _WaveOffsetY + noiseValue) * _WaveFrequencyY) * _WaveAmplitudeY);
                waveX = waveX * 0.5 + 0.5;

                // Secondary wave: modulates the primary wave's amplitude for 2D effect
                float waveY = cos((uv.y + timeY + _WaveOffsetY + noiseValue) * _WaveFrequencyY * 0.5) * _SecondaryWaveStrength + 1.0;
                
                // Map wave to rainbow colors with adjustable red band width
                float t = frac(waveX); // Normalized wave value [0,1]
                
                // Calculate band widths
                float totalUnits = 6.0 + _RedWidthFactor;
                float unitWidth = 1.0 / totalUnits;
                float redWidth = unitWidth * _RedWidthFactor;
                float otherWidth = unitWidth;
                
                // Cumulative starts
                float cum[7];
                cum[0] = 0.0;
                cum[1] = redWidth;
                cum[2] = cum[1] + otherWidth;
                cum[3] = cum[2] + otherWidth;
                cum[4] = cum[3] + otherWidth;
                cum[5] = cum[4] + otherWidth;
                cum[6] = cum[5] + otherWidth;
                // cum[7] would be 1.0, but not needed
                
                // Find index and local lerpT
                int index = 0;
                float lerpT = 0.0;
                for (int i = 0; i < 7; i++) {
                    if (t < cum[i+1]) {
                        index = i;
                        float bandWidth = cum[i+1] - cum[i];
                        lerpT = (t - cum[i]) / (bandWidth + _ColorBlend);
                        break;
                    }
                }

                float3 finalColor = lerp(rainbow[index], rainbow[(index + 1) % 7], lerpT * waveY);

                // Enhance contrast and apply intensity with some noise shimmer
                finalColor = pow(finalColor, 1.5); // Increase color contrast
                finalColor += (noise(uv * _NoiseScale * 2.0 + _Time.y * _NoiseSpeed * 1.5) - 0.5) * 0.1; // Add subtle shimmer
                finalColor = lerp(float3(1,1,1), finalColor, _Intensity);
                
                // Apply rainbow effect over sprite colors
                c.rgb *= finalColor; // Multiply rainbow colors with sprite colors
                c.rgb *= c.a; // Premultiply alpha
                return c;
            }
            ENDCG
        }
    }
}