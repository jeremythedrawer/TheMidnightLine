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
        _Flip ("Flip", Float) = 0
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

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                float2  uv              : TEXCOORD0;
                float3  positionWS      : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            StructuredBuffer<AtlasSprite> _AtlasSprites;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            float2 _EntityDepthRange;

            CBUFFER_START(UnityPerMaterial)
                uint _AtlasIndex;
                half4 _Color;
                float _DepthGreyScale;
                float _ZPos;
                float _Alpha;
                float _Flip;
            CBUFFER_END


            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                AtlasSprite atlasSprite = _AtlasSprites[_AtlasIndex];
                o.positionCS = TransformObjectToHClip(v.positionOS);
                float2 centeredUV = v.uv - float2(0.5, 0);
                float2 aspect = float2(atlasSprite.uvSize.x / atlasSprite.uvSize.y, 1);
                centeredUV /= aspect;
                centeredUV.x = _Flip > 0.5 ? centeredUV.x : -centeredUV.x;
                o.uv = centeredUV * atlasSprite.uvSize + atlasSprite.uvPosition + atlasSprite.pivot * atlasSprite.uvSize;

                return o;
            }


            half4 frag (Varyings i) : SV_Target
            {
                AtlasSprite atlasSprite = _AtlasSprites[_AtlasIndex];
                float2 maxUVPos = atlasSprite.uvPosition + atlasSprite.uvSize;
                float spriteBound = i.uv.x > atlasSprite.uvPosition.x && i.uv.y > atlasSprite.uvPosition.y & i.uv.x < maxUVPos.x & i.uv.y < maxUVPos.y;
                half4 sampledMainTex =  SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                float greyScale = ((_ZPos - _EntityDepthRange.x) / (_EntityDepthRange.y)) * _DepthGreyScale;
                half3 col = sampledMainTex.rgb + _Color + greyScale;
                return half4(col, (sampledMainTex.a * spriteBound) * _Alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
