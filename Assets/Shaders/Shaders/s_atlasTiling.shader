Shader "Custom/s_atlasTiling"
{
    Properties
    {
        [NoScaleOffset] _AtlasTexture("Texture Atlas", 2D) = "white"
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag


            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);
            float4 _AtlasTexture_TexelSize;
            float4 _AtlasTexture_ST;
            float _MetersTravelled;
            
            UNITY_INSTANCING_BUFFER_START(AtlasProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _UVSizeAndPos)
                UNITY_DEFINE_INSTANCED_PROP(float2, _WidthHeight)
                UNITY_DEFINE_INSTANCED_PROP(float2, _Flip)
                UNITY_DEFINE_INSTANCED_PROP(float, _Aspect)
            UNITY_INSTANCING_BUFFER_END(AtlasProps)

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);

                float4 uvSizeAndPos = UNITY_ACCESS_INSTANCED_PROP(AtlasProps, _UVSizeAndPos);
                float2 widthHeight = UNITY_ACCESS_INSTANCED_PROP(AtlasProps, _WidthHeight);

                float2 scrollUV = v.positionOS.xy + float2(_MetersTravelled, 0);

                o.uv = scrollUV;
                o.uv *= uvSizeAndPos.xy;
                o.uv += uvSizeAndPos.zw;

                return o;
            }


            half4 frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                return half4(i.uv, 0, 1);
                return color;
            }
            ENDHLSL
        }
    }
}
