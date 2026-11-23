Shader "Custom/ObjectHidingMask"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)

        // маска видимости
        _VisibilityTex ("VisibilityTex", 2D) = "white" {}
        _MaskWorldCenter ("MaskWorldCenter", Vector) = (0,0,0,0)
        _MaskWorldSize ("MaskWorldSize (XZ)", Vector) = (10,0,10,0) // X=sizeX, Z=sizeZ

        _AlphaIfHidden("AlphaWhenHidden", Range(0,1)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_VisibilityTex); SAMPLER(sampler_VisibilityTex);

            float4 _BaseColor;
            float3 _MaskWorldCenter;
            float3 _MaskWorldSize;
            float _AlphaIfHidden;

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
                // sample base color
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                // map worldPos.xz to mask UV (0..1)
                float2 rel = (i.worldPos.xz - _MaskWorldCenter.xz) / _MaskWorldSize.xz; 
                float2 maskUV = rel * 0.5 + 0.5;

                // clamp (outside mask consider hidden)
                bool outside = any(maskUV < 0) || any(maskUV > 1);

                // read mask (if outside -> treat as 0)
                float vis = 0;
                if (!outside)
                {
                    vis = SAMPLE_TEXTURE2D(_VisibilityTex, sampler_VisibilityTex, maskUV).r;
                }

                // final alpha: if visible keep base alpha, else reduce to _AlphaIfHidden
                baseCol.a *= lerp(1.0, _AlphaIfHidden, vis);

                return baseCol;
            }
            ENDHLSL
        }
    }
}
