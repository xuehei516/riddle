Shader "Custom/2D/LightBeam2D"
{
    Properties
    {
        _MainTex ("Sprite Texture (Unity默认白图)", 2D) = "white" {}
        [HDR] _Color ("光照颜色 (支持HDR提高发光)", Color) = (1.0, 0.95, 0.7, 0.8)
        
        [Header(Edge Softness)]
        _SideSoftness ("两侧羽化程度", Range(0.01, 1.0)) = 0.6
        _HeadSoftness ("光源起点羽化", Range(0.0, 0.5)) = 0.05
        _TailSoftness ("光束末端衰减", Range(0.0, 1.0)) = 0.4

        [Header(Tyndall Light Streaks)]
        _StreakSpeed ("光丝流动速度", Float) = 1.5
        _StreakDensity ("光丝密集度", Float) = 8.0
        _StreakStrength ("光丝动态对比度", Range(0.0, 0.5)) = 0.15
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
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        // ★ 核心：使用半透明加色混合（Additive Blend），光线重叠时会变亮，自然穿透背景
        Blend SrcAlpha One

        Pass
        {
            Name "LightBeamPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _SideSoftness;
                float _HeadSoftness;
                float _TailSoftness;
                float _StreakSpeed;
                float _StreakDensity;
                float _StreakStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. 上下两侧羽化（中心最亮，向两边淡出成柔光）
                float distFromCenter = abs(input.uv.y - 0.5) * 2.0; // 0 是中心，1 是边缘
                float sideFade = smoothstep(1.0, 1.0 - _SideSoftness, distFromCenter);

                // 2. 光线行进方向衰减（UV.x: 0是起点光源，1是终点）
                float headFade = smoothstep(0.0, _HeadSoftness, input.uv.x);
                float tailFade = smoothstep(1.0, 1.0 - _TailSoftness, input.uv.x);
                float lengthFade = headFade * tailFade;

                // 3. 丁达尔光线流丝（正弦波合成的多重流动光缕）
                float wave1 = sin(input.uv.y * _StreakDensity + _Time.y * _StreakSpeed);
                float wave2 = sin(input.uv.y * (_StreakDensity * 1.7) - _Time.y * (_StreakSpeed * 1.3) + input.uv.x * 2.0);
                float streaks = (wave1 + wave2) * 0.25 + 0.5; // 0 ~ 1 范围
                
                // 将光丝叠加进光照强度
                float streakIntensity = 1.0 - (streaks * _StreakStrength);

                // 4. 最终光强合成
                float totalIntensity = sideFade * lengthFade * streakIntensity;

                half4 finalColor = input.color * totalIntensity;
                return finalColor;
            }
            ENDHLSL
        }
    }
}