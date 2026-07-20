Shader "Custom/s_moon"
{
    Properties
    {
        _XOffset("X Offset", Range(-4, 4)) = 0.5
        _YOffset("Y Offset", Range(-4, 4)) = 0
        _CrescentSize("Crescent Size", Range(0, 2)) = 1
        _Fade("Fade", Range(-1,1)) = 0.25
        _FadeSpeed("Fade Speed", Range(0, 20)) = 5
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

            CBUFFER_START(UnityPerMaterial)
                float _XOffset;
                float _YOffset;
                float _CrescentSize;
                float _Fade;
                float _FadeSpeed;
            CBUFFER_END
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
                float cutCircle = length(p / _CrescentSize - float2(_XOffset, _YOffset));

                float fade = i.uv.y * (1 - _Fade) + _Fade;

                float fullMoon = ceil(circle) * floor(cutCircle);
                float mask = fullMoon * fade;
                mask = BayerX8(mask, i.positionHCS.y + (_Time.y * _FadeSpeed));

                clip(mask - 0.001);
                return half4(mask.xxx, 1);
            }
            ENDHLSL
        }
    }
}
