Shader "Custom/OutlineEdgeDetection"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _DepthThreshold ("Depth Threshold", Range(0.0001, 1)) = 0.05
        _NormalThreshold ("Normal Threshold", Range(0.0001, 1)) = 0.4
        _OutlineThickness ("Outline Thickness (px)", Range(0.5, 5)) = 1.0
        _MaxDistance01 ("Max Distance (Linear01, 0-1)", Range(0.01, 1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "OutlinePass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float _DepthThreshold;
            float _NormalThreshold;
            float _OutlineThickness;
            float _MaxDistance01;

            float SampleDepth(float2 uv)
            {
                return Linear01Depth(SampleSceneDepth(uv), _ZBufferParams);
            }

            float3 SampleNormal(float2 uv)
            {
                return SampleSceneNormals(uv);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                half4 baseColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                // Центральный пиксель — если это небо/слишком далеко, сразу выходим.
                // Буфер глубины на большой дистанции почти не имеет точности,
                // и попытка сравнивать соседние значения там даёт "полосатый" шум.
                float centerDepth01 = SampleDepth(uv);
                if (centerDepth01 >= _MaxDistance01)
                {
                    return baseColor;
                }

                // Точный размер пикселя экрана, без искажений
                float2 texel = _ScreenParams.zw - 1.0;
                float2 offset = texel * _OutlineThickness;

                float2 uvR = uv + float2(offset.x, 0);
                float2 uvL = uv - float2(offset.x, 0);
                float2 uvU = uv + float2(0, offset.y);
                float2 uvD = uv - float2(0, offset.y);

                // --- Depth edge ---
                float dR = SampleDepth(uvR);
                float dL = SampleDepth(uvL);
                float dU = SampleDepth(uvU);
                float dD = SampleDepth(uvD);
                float depthDiff = abs(dR - dL) + abs(dU - dD);
                float depthEdge = step(_DepthThreshold, depthDiff);

                // --- Normal edge ---
                float3 nR = SampleNormal(uvR);
                float3 nL = SampleNormal(uvL);
                float3 nU = SampleNormal(uvU);
                float3 nD = SampleNormal(uvD);
                float normalDiff = distance(nR, nL) + distance(nU, nD);
                float normalEdge = step(_NormalThreshold, normalDiff);

                float edge = saturate(depthEdge + normalEdge);

                return lerp(baseColor, _OutlineColor, edge);
            }
            ENDHLSL
        }
    }
}