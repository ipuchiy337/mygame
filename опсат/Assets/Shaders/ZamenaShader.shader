Shader "Custom/ZamenaShader_NoPlayer"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)

        _HidingDistance("Hide Distance", Range(0,10)) = 3

        _Enable("Enable Fog", Float) = 0   // 0 – выкл, 1 – вкл
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap;
            float4 _BaseColor;

            float _HidingDistance;
            float _Enable;

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float4 col = tex2D(_BaseMap, i.uv) * _BaseColor;

                // Если эффект выключен, объект полностью видимый
                if (_Enable < 0.5)
                    return col;

                // Просто глобальная прозрачность (никакого игрока)
                col.a *= saturate(_HidingDistance / 10.0);

                return col;
            }
            ENDHLSL
        }
    }
}
