Shader "Custom/Advanced Outline Fill" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.015
    }
    SubShader
    {
        Pass
        {
            Name "OUTLINE"
            Tags
            {
                "Queue" = "Geometry+1"
                "RenderType" = "Opaque"
            }
            Cull Front
            ZWrite On
            ZTest LEqual
            CGPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            float _OutlineWidth;
            float4 _OutlineColor;

            v2f vertOutline(appdata v)
            {
                v2f o;

                float3 normal = normalize(v.normal);

                // === ‘» — (без искажени€ направлени€) ===
                // Ќаправление "от центра объекта к вершине" гладкое по своей природе
                // (не зависит от разрыва нормалей на hard-edge гран€х),
                // поэтому смешиваем его с насто€щей нормалью Ч это гасит "шипы"
                // на резких углах, но не ломает направление смещени€.
                float3 posDir = normalize(v.vertex.xyz + float3(0.0001, 0.0001, 0.0001));
                float3 blendedDir = normalize(lerp(normal, posDir, 0.5));

                float3 position = v.vertex.xyz + blendedDir * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(float4(position, 1.0));
                return o;
            }
            fixed4 fragOutline(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
        // =========================================================
        // ќ—Ќќ¬Ќјя ћќƒ≈Ћ№
        // =========================================================
        Pass
        {
            Name "MAIN"
            Tags
            {
                "Queue" = "Geometry"
                "RenderType" = "Opaque"
            }
            Cull Back
            ZWrite On
            ZTest LEqual
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}