Shader "Custom/s_atlasScroll"
{
    Properties
    {
        [NoScaleOffset] _AtlasTexture("Texture Atlas", 2D) = "white"
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
            #include "Assets/Shaders/HLSL/AtlasSprites.hlsl"
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #define PIXELS_PER_UNIT 180

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
            float4 _AtlasTexture_TexelSize;

            float _MetersTravelled;

            UNITY_INSTANCING_BUFFER_START(AtlasProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _UVSizeAndPos)
                UNITY_DEFINE_INSTANCED_PROP(float4, _WidthHeightFlip)
            UNITY_INSTANCING_BUFFER_END(AtlasProps)


            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 uvSizeAndPos = UNITY_ACCESS_INSTANCED_PROP(AtlasProps, _UVSizeAndPos);
                float4 widthHeightFlip  = UNITY_ACCESS_INSTANCED_PROP(AtlasProps, _WidthHeightFlip);

                i.uv *= widthHeightFlip.xy;
                float spritePixelSize = _AtlasTexture_TexelSize.z * uvSizeAndPos.x;

                i.uv.x += _MetersTravelled / (spritePixelSize / PIXELS_PER_UNIT);
                i.uv = frac(i.uv);
                i.uv = (i.uv - 0.5) * widthHeightFlip.zw + 0.5;
                i.uv *= uvSizeAndPos.xy;
                i.uv += uvSizeAndPos.zw;
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);

                half3 finalColor = color.rgb;

                clip(color.a - 0.001);
                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}
