Shader "Unlit/s_character"
{
    Properties
    {
        [HideInInspector][NoScaleOffset] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Color", Color) = (1,1,1,1)
        _DepthGreyScale ("Depth Grey Scale", Range(0, 1)) = 0.5
        _Alpha ("Alpha", Range(0,1)) = 1
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
                float _DepthGreyScale;
                float _ZPos;
                float _Alpha;
            CBUFFER_END

            float2 _EntityDepthRange;

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color * unity_SpriteColor;
                return o;
            }


            half4 frag (Varyings i) : SV_Target
            {
                half4 sampledMainTex =  i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float greyScale = ((_ZPos - _EntityDepthRange.x) / (_EntityDepthRange.y)) * _DepthGreyScale;
                half3 col = sampledMainTex.rgb + _Color + greyScale;
                return half4(col, sampledMainTex.a * _Alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
