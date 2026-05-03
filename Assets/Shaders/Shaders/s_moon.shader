Shader "Custom/s_moon"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"
            #pragma vertex vert
            #pragma fragment frag

            float _DayNight;

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

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 objPos = v.positionOS.xyz;
                float2 scale;
                scale.x = length(unity_ObjectToWorld._m00_m10_m20);
                scale.y = length(unity_ObjectToWorld._m01_m11_m21);

                float worldCamTop = unity_OrthoParams.y + _WorldSpaceCameraPos.y;

                float3 worldPos = TransformObjectToWorld(objPos);
                worldPos.y = lerp(worldPos.y - scale.y, worldPos.y + (worldCamTop * 0.75), _DayNight);
                
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.worldPos = worldPos;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float circle = 1 - length(p);
                float cutCircle = length(p - float2(0.5, 0));


                float fade = i.uv.y * 0.75 + 0.25;

                float fullMoon = ceil(circle) * floor(cutCircle);
                float mask = fullMoon * fade;
                mask = BayerX8(mask, i.positionHCS.y + (_Time.y * 5));

                clip(mask - 0.001);
                return half4(mask.xxx, 1);
            }
            ENDHLSL
        }
    }
}
