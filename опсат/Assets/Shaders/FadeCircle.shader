Shader "Custom/URP_FadeCircle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FadeRadius ("Fade Radius", Range(0.5, 8)) = 12
        _MinAlpha ("Min Alpha", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags{
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float _FadeRadius;
            float _MinAlpha;

            float3 _FadeCenter;   // позиция игрока передана глобально

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float dist = distance(IN.worldPos.xz, _FadeCenter.xz);

                float fade = saturate(dist / _FadeRadius);   // 0 в центре → 1 на краю
                float alpha = lerp(1.0, _MinAlpha, fade);   // теперь центр = 1, край = _MinAlpha
 

                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                tex *= _Color;

                return float4(tex.rgb, tex.a * alpha);
            }

            ENDHLSL
        }
    }
}
