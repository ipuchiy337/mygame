Shader "Custom/ObjectHidingURP"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)

        _HidingDistance("Hide Distance", Range(0,10)) = 3
        _PlayerWorldPos("Player Pos", Vector) = (0,0,0,0)

        _Enable("Enable Fog", Float) = 0   // 0 Ц выкл, 1 Ц вкл
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

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
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _BaseMap;
            float4 _BaseColor;

            float _HidingDistance;
            float3 _PlayerWorldPos;
            float _Enable; // управление включением эффекта

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float dist = distance(i.worldPos, _PlayerWorldPos);

                // прозрачность (1 Ч видно, 0 Ч скрыто)
                float alpha = saturate(dist / _HidingDistance);

                // если _Enable = 0 ? эффект выключен ? alpha = 1
                alpha = lerp(1, alpha, _Enable);

                float4 col = tex2D(_BaseMap, i.uv) * _BaseColor;
                col.a *= alpha;

                return col;
            }
            ENDHLSL
        }
    }
}
