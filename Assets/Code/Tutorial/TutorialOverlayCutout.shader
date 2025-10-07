Shader "UI/TutorialOverlayCutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Overlay Color", Color) = (0,0,0,0.75)
        _HoleTex ("Hole Texture", 2D) = "white" {}
        _HolePos ("Hole Position (UV)", Vector) = (0,0,0,0)
        _HoleSize ("Hole Size (UV)", Vector) = (1,1,0,0)
        _HolePivot ("Hole Pivot (UV)", Vector) = (0.5,0.5,0,0)
        _Smoothness ("Smoothness", Float) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _HoleTex;
            float4 _Color;
            float4 _HolePos;
            float4 _HoleSize;
            float4 _HolePivot;
            float _Smoothness;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // Вычислить UV для hole texture
                float2 holeUV = (i.uv - _HolePos.xy) / _HoleSize.xy + _HolePivot.xy;
                
                // Проверить, внутри ли UV области дырки
                if (holeUV.x >= 0 && holeUV.x <= 1 && holeUV.y >= 0 && holeUV.y <= 1)
                {
                    float holeAlpha = tex2D(_HoleTex, holeUV).a;
                    // Плавный переход: уменьшаем alpha тени пропорционально alpha hole
                    float edge = smoothstep(0, _Smoothness, 1 - holeAlpha);
                    col.a *= edge;
                }

                return col;
            }
            ENDCG
        }
    }
}