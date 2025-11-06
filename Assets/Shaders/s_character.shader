Shader "Unlit/s_character"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Color", Color) = (1,1,1,1)
        _ZPos ("ZPos", float) = 1
        _EntityDepthRange ("Entity Depth Range", Vector) = (-9, -5, 0, 0)
        _DepthGreyScale ("Depth Grey Scale", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Cull Off

        Pass
        {
            AlphaToMask On
            AlphaTest Greater 0.5
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                float4  color           : COLOR;
                float2  uv              : TEXCOORD0;
                float3  positionWS      : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _ZPos;
                float2 _EntityDepthRange;
                half _DepthGreyScale;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.positionWS = TransformObjectToWorld(v.positionOS);

                o.uv = v.uv;

                o.color = v.color * _Color * unity_SpriteColor;
                return o;
            }


            half4 frag (Varyings i) : SV_Target
            {
                half4 sampledMainTex =  SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float greyScale = ((_ZPos - _EntityDepthRange.x) / (_EntityDepthRange.y - _EntityDepthRange.x)) * _DepthGreyScale;

                half3 col = sampledMainTex.rgb + _Color + greyScale;


                return half4(col, sampledMainTex.a);
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
