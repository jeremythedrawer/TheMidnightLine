Shader "Unlit/s_character"
{
    Properties
    {
         [NoScaleOffset]_AtlasTexture ("Atlas Texture", 2D) = "white" {}

        _Color ("Color", Color) = (1,1,1,1)
        _AtlasIndex("Atlas Index", Float) = 0
        _DepthGreyScale ("Depth Grey Scale", Range(0, 1)) = 0.5
        _Alpha ("Alpha", Range(0,1)) = 1
        _ZPos ("ZPos", Float) = 0
        _Flip ("Flip", Range(0,1)) = 0
        _PPU ("Pixels Per Unit", Float) = 32

    }
    SubShader
    {

        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/AtlasSprites.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);
            float2 _EntityDepthRange;
            float4 _AtlasTexture_TexelSize;

            
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                uint _AtlasIndex;
                float _DepthGreyScale;
                float _ZPos;
                float _Alpha;
                int _Flip;
                float _PPU;
            CBUFFER_END


            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.positionOS;
                o.uv.x *= _Flip;
               // o.uv *= float2(1 / atlasSprite.aspect, 1) * (_PPU / (atlasSprite.uvSize.y * _AtlasTexture_TexelSize.w)) * atlasSprite.uvSize;
                //o.uv += (atlasSprite.pivot * atlasSprite.uvSize + atlasSprite.uvPosition);
                return o;
            }


            half4 frag (Varyings i) : SV_Target
            {
                float greyScale = ((_ZPos - _EntityDepthRange.x) / (_EntityDepthRange.y)) * _DepthGreyScale;
                half4 sampledMainTex =  SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                half3 col = sampledMainTex.rgb + _Color;
                return half4(col, (sampledMainTex.a) * _Alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
