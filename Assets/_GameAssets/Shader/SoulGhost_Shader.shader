Shader "Custom/2D/SoulGhost"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("幽光颜色 (支持HDR发光)", Color) = (0.2, 0.8, 1.0, 0.85)
        
        [Header(Wave Settings)]
        _WaveSpeed ("摇曳速度", Float) = 4.0
        _WaveFrequency ("摇曳频率", Float) = 5.0
        _WaveAmplitude ("摇曳幅度", Float) = 0.04

        [Header(Pulse Settings)]
        _PulseSpeed ("呼吸频率", Float) = 2.5
        _MinAlpha ("最低透明度", Range(0.2, 1.0)) = 0.45
        _GlowIntensity ("额外泛光倍率", Range(1.0, 3.0)) = 1.5
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
        Blend SrcAlpha OneMinusSrcAlpha // 经典半透明混合

        Pass
        {
            Name "SoulGhostPass"

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
                float _WaveSpeed;
                float _WaveFrequency;
                float _WaveAmplitude;
                float _PulseSpeed;
                float _MinAlpha;
                float _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // ★ 灵魂鬼火波动核心算法：
                // (1.0 - input.uv.y) 表示顶部(UV.y=1)权重为0（保持平稳好落脚），越往下摆权重越大（下摆剧烈飘动）
                float swayFactor = pow(1.0 - input.uv.y, 1.5);
                float wave = sin(_Time.y * _WaveSpeed + input.positionOS.y * _WaveFrequency) * _WaveAmplitude * swayFactor;
                
                input.positionOS.x += wave;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 贴图采样
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // 呼吸效果（0~1 平滑脉冲）
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;

                // 颜色和透明度计算
                half4 finalColor = texColor * input.color;
                
                // 呼吸时让透明度在 _MinAlpha 和 1.0 之间微颤
                finalColor.a *= lerp(_MinAlpha, 1.0, pulse);

                // 配合 URP Post-processing 的 Bloom（泛光）效果
                finalColor.rgb *= (1.0 + pulse * (_GlowIntensity - 1.0));

                return finalColor;
            }
            ENDHLSL
        }
    }
}