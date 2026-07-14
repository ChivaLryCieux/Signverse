Shader "GlowWithShakeAndOutline" {
    Properties {
        _MainTex ("Texture", 2D) = "white" { }
        
        //流光参数
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _GlowTex ("GlowTexture", 2D) = "white" { }
        _GlowIntensityMax ("Glow Intensity Max", Range(0, 10)) = 2.4
        _GlowScrollSpeedX ("Glow Scroll Speed X", Range(-3, 3)) = 0.3   // ★X方向速度
        _GlowScrollSpeedY ("Glow Scroll Speed Y", Range(-3, 3)) = 0     // ★Y方向速度

        //流光扭曲参数
        _DistortTex ("DistortionTex", 2D) = "white" { }
        _DistortAmount ("DistortionAmount", Range(0, 2)) = 2
        _DistortTexXSpeed ("DistortTexXSpeed", Range(-50, 50)) = 0
        _DistortTexYSpeed ("DistortTexYSpeed", Range(-50, 50)) = -5

        //描边参数
        _OutlineColor ("OutlineColor", Color) = (1, 1, 1, 1)
        _OutlineAlpha ("OutlineAlpha", Range(0, 1)) = 1
        _OutlinePixelWidth ("OutlinePixelWidth", Int) = 1
        _OutlineDistortTex ("OutlineDistortionTex", 2D) = "white" { }
        _OutlineDistortAmountMax ("OutlineDistortionAmount Max", Range(0, 2)) = 1.58
        _OutlineDistortTexXSpeed ("OutlineDistortTexXSpeed", Range(-50, 50)) = 5
        _OutlineDistortTexYSpeed ("OutlineDistortTexYSpeed", Range(-50, 50)) = 5

        //抖动参数
        _ShakeUvSpeedMax ("Shake Speed Max", Range(0, 20)) = 11.3
        _ShakeUvX ("X Multiplier", Range(0, 5)) = 1.5
        _ShakeUvY ("Y Multiplier", Range(0, 5)) = 1

        //渐变进度控制
        _Progress ("Transition Progress", Range(0, 1)) = 0
    }
    SubShader {
        Tags { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass {
            Name "GlowWithShakeAndOutline"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            TEXTURE2D(_GlowTex);
            SAMPLER(sampler_GlowTex);

            TEXTURE2D(_DistortTex);
            SAMPLER(sampler_DistortTex);
            float4 _DistortTex_ST;

            TEXTURE2D(_OutlineDistortTex);
            SAMPLER(sampler_OutlineDistortTex);
            float4 _OutlineDistortTex_ST;

            half4 _GlowColor;
            float _GlowIntensityMax;
            float _GlowScrollSpeedX;   // ★X方向滚动速度
            float _GlowScrollSpeedY;   // ★Y方向滚动速度

            float _DistortAmount;
            float _DistortTexXSpeed, _DistortTexYSpeed;

            half4 _OutlineColor;
            float _OutlineAlpha;
            int _OutlinePixelWidth;
            float _OutlineDistortTexXSpeed, _OutlineDistortTexYSpeed;
            float _OutlineDistortAmountMax;

            half _ShakeUvSpeedMax;
            half _ShakeUvX, _ShakeUvY;

            float _Progress;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 uvOutDistTex : TEXCOORD1;
                float2 uvOutOutlineDistTex : TEXCOORD2;
                half4 color : COLOR;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.uvOutDistTex = v.uv * _DistortTex_ST.xy + _DistortTex_ST.zw;
                o.uvOutOutlineDistTex = v.uv * _OutlineDistortTex_ST.xy + _OutlineDistortTex_ST.zw;
                o.color = v.color;
                return o;
            }

            half4 frag(v2f i) : SV_Target {
                float progress = saturate(_Progress);
                
                float currentGlowIntensity = _GlowIntensityMax * progress;
                half currentShakeSpeed = _ShakeUvSpeedMax * progress;
                float currentOutlineDistortAmount = _OutlineDistortAmountMax * progress;

                //===== 抖动效果 =====
                half xShake = sin(_Time.y * currentShakeSpeed * 50) * _ShakeUvX;
                half yShake = cos(_Time.y * currentShakeSpeed * 50) * _ShakeUvY;
                float2 shakeUv = i.uv + half2(xShake * 0.01, yShake * 0.01);

                //===== 流光扭曲效果 =====
                float2 distortUV = i.uvOutDistTex + float2(_Time.y * _DistortTexXSpeed, _Time.y * _DistortTexYSpeed);
                float outDistortAmnt = (SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortUV).r - 0.5) * 0.2 * _DistortAmount;
                
                float2 destUv = shakeUv;
                destUv.x += outDistortAmnt;
                destUv.y += outDistortAmnt;
                float4 noiseCol = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, destUv);

                //===== ★★★ 流光滚动：XY方向独立控制 ★★★ =====
                // 同时应用X和Y方向的速度，组合出任意方向
                float2 glowUV = shakeUv + float2(_Time.y * _GlowScrollSpeedX, _Time.y * _GlowScrollSpeedY);
                
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shakeUv);
                half4 emission = SAMPLE_TEXTURE2D(_GlowTex, sampler_GlowTex, glowUV);
                emission.rgb *= emission.a * col.a * currentGlowIntensity * _GlowColor;
                col.rgb += emission.rgb * noiseCol;

                //===== 描边效果 =====
                float originalAlpha = col.a;

                float2 outlineOffset = float2(_OutlinePixelWidth * _MainTex_TexelSize.x, _OutlinePixelWidth * _MainTex_TexelSize.y);

                float2 outlineDistortUV = i.uvOutOutlineDistTex + float2(_Time.y * _OutlineDistortTexXSpeed, _Time.y * _OutlineDistortTexYSpeed);
                float outlineDistortAmnt = (SAMPLE_TEXTURE2D(_OutlineDistortTex, sampler_OutlineDistortTex, outlineDistortUV).r - 0.5) * 0.2 * currentOutlineDistortAmount;
                outlineOffset.x += outlineDistortAmnt;
                outlineOffset.y += outlineDistortAmnt;

                float spriteLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shakeUv + float2(outlineOffset.x, 0)).a;
                float spriteRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shakeUv - float2(outlineOffset.x, 0)).a;
                float spriteBottom = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shakeUv + float2(0, outlineOffset.y)).a;
                float spriteTop = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shakeUv - float2(0, outlineOffset.y)).a;
                float spriteTopLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shakeUv + float2(outlineOffset.x, outlineOffset.y)).a;
                float spriteTopRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shakeUv + float2(-outlineOffset.x, outlineOffset.y)).a;
                float spriteBotLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shakeUv + float2(outlineOffset.x, -outlineOffset.y)).a;
                float spriteBotRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shakeUv + float2(-outlineOffset.x, -outlineOffset.y)).a;
                float result = spriteLeft + spriteRight + spriteBottom + spriteTop + spriteTopLeft + spriteTopRight + spriteBotLeft + spriteBotRight;

                result = step(0.05, saturate(result));
                result *= (1 - originalAlpha) * _OutlineAlpha;

                half4 outline = _OutlineColor;
                col = lerp(col, outline, result);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}