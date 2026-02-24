Shader "Custom/s_exteriorWalls"
{
    Properties
    {
        [NoScaleOffset] _AtlasTexture("Texture Atlas", 2D) = "white"
        [NoScaleOffset] _EmissionTexture("Emission Atlas", 2D) = "black"
        _UVSizeAndPos ("UV Size And Pos", Vector) = (0,0,0,0)
        _WidthHeightFlip ("Width Height And Flip", Vector) = (0,0,0,0)
        _Alpha("Alpha", float) = 0.0
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
            TEXTURE2D(_EmissionTexture);
            SAMPLER(sampler_AtlasTexture);
            SAMPLER(sampler_EmissionTexture);
            float4 _AtlasTexture_ST;

            CBUFFER_START(UnityPerMaterial)
                float4 _UVSizeAndPos;
                float4 _WidthHeightFlip;
                float  _Alpha;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                i.uv *= _WidthHeightFlip.xy;
                i.uv = frac(i.uv);
                i.uv = (i.uv - 0.5) * _WidthHeightFlip.zw + 0.5;
                i.uv *= _UVSizeAndPos.xy;
                i.uv += _UVSizeAndPos.zw;
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);

                half emission = SAMPLE_TEXTURE2D(_EmissionTexture, sampler_EmissionTexture, i.uv).r;
                emission *= 2;

                half3 finalColor = color.rgb * emission;
                finalColor = max(color.rgb, finalColor);
                float finalAlpha = _Alpha * color.a;
                clip(finalAlpha - 0.001);
                return half4 (finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
