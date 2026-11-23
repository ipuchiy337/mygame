Shader "Custom/CeilingVisionShader"
{
    Properties
    {
        _PlayerPosition("Player Position", Vector) = (0,0,0,0)
        _ViewRadius("View Radius", Float) = 5
        _ConeAngle("Cone Angle (degrees)", Float) = 360
        _PlayerForward("Player Forward", Vector) = (0,0,1,0)
        _DarkColor("Dark Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100

        Pass
        {
            Stencil {
              Ref 1
              Comp NotEqual
            }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float3 _PlayerPosition;
            float _ViewRadius;
            float _ConeAngle;
            float3 _PlayerForward;
            float4 _DarkColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = i.worldPos - _PlayerPosition;
                float dist = length(dir);

                // Расчёт конуса
                float angleMask = 1.0;
                if (_ConeAngle < 360)
                {
                    float3 dirNorm = normalize(dir);
                    float3 forwardNorm = normalize(_PlayerForward);
                    float angle = acos(dot(forwardNorm, dirNorm)) * (180/3.14159265);
                    angleMask = smoothstep(_ConeAngle/2, _ConeAngle/2 - 5, angle); // 5° мягкий край
                }

                float radiusMask = smoothstep(_ViewRadius, _ViewRadius - 1.0, dist); // 1 юнит градиент
                float mask = radiusMask * angleMask;

                fixed4 col = lerp(_DarkColor, float4(0,0,0,0), mask);
                return col;
            }
            ENDHLSL
        }
    }
}