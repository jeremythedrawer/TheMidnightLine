Shader "Custom/s_atlasStandard"
{
    Properties
    {
        _AtlasTexture("Texture Atlas", 2D) = "white"
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
            #include "Assets/Shaders/HLSL/AtlasSprites.hlsl"
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
            float4 _AtlasTexture_ST;

            UNITY_INSTANCING_BUFFER_START(AtlasProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _UVSizeAndPos)
            UNITY_INSTANCING_BUFFER_END(AtlasProps)

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.positionOS;
                float4 uvSizeAndPos = UNITY_ACCESS_INSTANCED_PROP(AtlasProps, _UVSizeAndPos);
                o.uv *= uvSizeAndPos.xy;
                o.uv += uvSizeAndPos.zw;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                return color;
            }
            ENDHLSL
        }
    }
}
