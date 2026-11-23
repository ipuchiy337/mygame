Shader "Grass/TrampleBlob"
{
    Properties
    {
        _Strength ("Strength", Range(0,2)) = 1
        _Softness ("Softness (power)", Range(0.5, 6)) = 2.5
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "TrampleBlobPass"
            Blend One One          // Additive: пятна складываются, а не перекрывают друг друга
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float _Softness;
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // расстояние от центра квада (UV 0..1 -> центр в 0.5,0.5)
                float2 centered = IN.uv - 0.5;
                float dist = length(centered) * 2.0; // 0 в центре, 1 на краю

                float mask = 1.0 - saturate(dist);
                mask = pow(mask, _Softness);   // мягкий спад к краям

                float value = mask * _Strength;

                return float4(_Color.rgb * value, value);
            }
            ENDHLSL
        }
    }
}