Shader "Custom/s_fadeBlack"
{
    Properties
    {
        _Alpha("Alpha", Range(0,1)) = 0
        [NoScaleOffset] _NoiseTexture("Noise Texture", 2D) = "white"
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
            
            CBUFFER_START(UnityPerMaterial)
                float _Alpha;
            CBUFFER_END

            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);


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

            float3 _BlackColor;
            float4 _CameraSizeAndPos;

            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 objPos = v.positionOS.xyz;
                objPos.xy *= _CameraSizeAndPos.xy;
                o.positionHCS = TransformObjectToHClip(objPos);
                o.uv = v.uv;
                
                return o;
            }
            
            half4 frag(Varyings i) : SV_Target
            {
                float mask = BayerX8(_Alpha, i.positionHCS.y);
                clip(mask - 0.001);

                half3 col = mask * _BlackColor;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
