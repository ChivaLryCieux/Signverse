Shader "Custom/FloatingSimple"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Amplitude ("幅度", Float) = 0.3
        _Speed ("速度", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Amplitude;
            float _Speed;

            v2f vert (appdata v)
            {
                v2f o;
                
                // 计算相位（使用物体自身位置让不同物体错落）
                float phase = v.vertex.x * 0.5 + v.vertex.z * 0.3;
                
                // 时间计算
                float t = _Time.y * _Speed + phase;
                
                // 使用 sin 的三次方实现缓动（最高点和最低点速度慢）
                float sinValue = sin(t);
                float easeValue = sinValue * sinValue * sinValue;
                
                // 计算偏移
                float offsetY = easeValue * _Amplitude;
                
                // 应用偏移到顶点
                float4 newVertex = v.vertex;
                newVertex.y += offsetY;
                
                o.vertex = UnityObjectToClipPos(newVertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}