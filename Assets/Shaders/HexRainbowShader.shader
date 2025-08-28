Shader "UI/SquareFadeShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FadeColor ("Fade Color", Color) = (0,0,0,1) // Black
        _TransitionProgress ("Transition Progress", Range(0, 1)) = 0
        _NoiseScale1 ("Noise Scale 1", Range(1, 50)) = 5
        _NoiseScale2 ("Noise Scale 2", Range(1, 50)) = 10
        _BlendWidth ("Blend Width", Range(0, 0.2)) = 0.05
        _BlurRadius ("Blur Radius", Range(0, 0.1)) = 0.02
        _MistIntensity ("Mist Intensity", Range(0, 1)) = 0.3
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
            fixed4 _FadeColor;
            float _TransitionProgress;
            float _NoiseScale1;
            float _NoiseScale2;
            float _BlendWidth;
            float _BlurRadius;
            float _MistIntensity;

            float2 randomGradient(float2 i)
            {
                float2 p = float2(
                    dot(i, float2(127.1, 311.7)),
                    dot(i, float2(269.5, 183.3))
                );
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float perlin(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);

                float dot00 = dot(randomGradient(i + float2(0.0, 0.0)), f - float2(0.0, 0.0));
                float dot10 = dot(randomGradient(i + float2(1.0, 0.0)), f - float2(1.0, 0.0));
                float dot01 = dot(randomGradient(i + float2(0.0, 1.0)), f - float2(0.0, 1.0));
                float dot11 = dot(randomGradient(i + float2(1.0, 1.0)), f - float2(1.0, 1.0));

                float mixX0 = lerp(dot00, dot10, u.x);
                float mixX1 = lerp(dot01, dot11, u.x);
                return lerp(mixX0, mixX1, u.y);
            }

            float ridgedFBM(float2 uv)
            {
                float val = 0.0;
                float amp = 0.6;

                val += amp * (1.0 - abs(perlin(uv * _NoiseScale1)));
                amp *= 0.5;
                val += amp * (1.0 - abs(perlin(uv * _NoiseScale2)));

                val /= 0.9;
                return val;
            }

            float3 blur(sampler2D tex, float2 uv, float radius)
            {
                float3 col = float3(0,0,0);
                float total = 0.0;
                for (float x = -radius; x <= radius; x += 0.01)
                {
                    for (float y = -radius; y <= radius; y += 0.01)
                    {
                        float weight = 1.0 - (abs(x) + abs(y)) / (2.0 * radius);
                        col += tex2D(tex, uv + float2(x, y)).rgb * weight;
                        total += weight;
                    }
                }
                return col / total;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                float2 uv = IN.texcoord;

                float ridge = ridgedFBM(uv);
                float threshold = _TransitionProgress;
                float mask = smoothstep(threshold - _BlendWidth, threshold, ridge); // Ensure full coverage

                float3 blurredFade = blur(_MainTex, uv, _BlurRadius);
                float3 finalFade = lerp(_FadeColor.rgb, blurredFade, _MistIntensity);

                c.rgb = lerp(c.rgb, finalFade, mask);
                c.a *= 1.0 - mask; // Fully fade original where mask is 1
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}