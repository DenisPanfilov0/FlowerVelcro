Shader "Hidden/CanvasBrush"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BrushColor ("Brush Color", Color) = (1,1,1,1)
        _BrushPos ("Brush Pos", Vector) = (0,0,0,0)
        _BrushSize ("Brush Size", Float) = 0.05
        _BrushHardness ("Hardness", Float) = 0.8
        _IsErase ("Is Erase", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _BrushColor;
            float4 _BrushPos; // x=uv.x y=uv.y
            float _BrushSize;
            float _BrushHardness;
            float _IsErase;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
{
    float2 uv = i.uv;
    fixed4 prev = tex2D(_MainTex, uv);

    // --- Исправление овала ---
    float aspect = _ScreenParams.y / _ScreenParams.x; // высота / ширина
    float2 scaledUV = uv;
    scaledUV.x *= aspect;                  // масштабируем по X
    float2 brushUV = _BrushPos.xy;
    brushUV.x *= aspect;

    float d = distance(scaledUV, brushUV); // теперь круг будет настоящий

    // compute normalized brush radius (in UV)
    float radius = _BrushSize * 0.5;
    float edge = radius * (1.0 - _BrushHardness);
    float alpha = 0.0;
    if (d <= radius)
    {
        if (edge > 0.0001)
            alpha = smoothstep(radius, radius - edge, d); // soft edge
        else
            alpha = 1.0;
    }

    if (_IsErase > 0.5)
    {
        fixed4 outCol = lerp(prev, fixed4(0,0,0,0), alpha);
        return outCol;
    }
    else
    {
        fixed4 brush = _BrushColor;
        fixed4 outCol = lerp(prev, brush, alpha * brush.a);
        return outCol;
    }
}

            ENDCG
        }
    }
    FallBack Off
}
