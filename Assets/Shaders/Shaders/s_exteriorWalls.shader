Shader "Custom/s_exteriorWalls"
{
    Properties
    {
        [NoScaleOffset] _AtlasTexture("Texture Atlas", 2D) = "white"
        _UVSizeAndPos ("UV Size And Pos", Vector) = (0,0,0,0)
        _WidthHeightFlip ("Width Height And Flip", Vector) = (0,0,0,0)
        _Alpha("Alpha", Float) = 0
        _WorldClip("World Clip", Float) = 0
    }

    SubShader
    {
        Pass
        {
            Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);
            float4 _AtlasTexture_ST;

            CBUFFER_START(UnityPerMaterial)
                float4 _UVSizeAndPos;
                float4 _WidthHeightFlip;
                float  _Alpha;
                float _WorldClip;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                worldPos.y -= (1 - _Alpha) * 3.3; // NOTE: 3.3 here is the world size of the exterior wall
                o.worldPos = worldPos;
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                i.uv *= _WidthHeightFlip.xy;
                i.uv *= _UVSizeAndPos.xy;
                i.uv += _UVSizeAndPos.zw;
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                half worldClip = step(_WorldClip, i.worldPos.y);
                float alpha = color.a * worldClip;
                clip(alpha - 0.001);
                return half4 (color.rgb,  alpha);
            }
            ENDHLSL
        }
    }
}
