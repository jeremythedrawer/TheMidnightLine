Shader "Unlit/s_bgAtlas"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _AtlasSize ("Atlas Size", vector) = (1,1,0,0)
        _Index ("Index", float) = 0
        _Scale("Scale", float) = 1
    }
    SubShader
    {
        Tags {"Queue" = "Geometry" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        LOD 100
        HLSLINCLUDE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            #include "Assets/Shaders/HLSLIncludes/TextureAtlas.hlsl"

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)                
                vector _AtlasSize;
                int _Index;
                float _Scale;
            CBUFFER_END


            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                half2 lightingUV    : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"


        ENDHLSL

        Pass
        {
            Tags {"Lightmode" = "Universal2D" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
                
                #pragma vertex vert
                #pragma fragment frag

                Varyings vert (Attributes v)
                {
                    Varyings o = (Varyings)0;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );
                    o.positionCS = TransformObjectToHClip(v.positionOS);
                    o.positionWS = TransformObjectToWorld(v.positionOS);

                    o.uv = v.uv;
                    o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);

                    return o;
                }


                half4 frag (Varyings i) : SV_TARGET
                {
                    float2 atlasUV = TextureAtlasUV(i.uv, _AtlasSize, _Index, _Scale);
                    //return half4(atlasUV, 0, 1);
                    const half4 sampledMainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, atlasUV);
                    const half3 mainTex = i.color + sampledMainTex.rgb;
                    const half mask = sampledMainTex.a;

                    SurfaceData2D surfaceData;
                    InputData2D inputData;

                    InitializeSurfaceData(mainTex.rgb, sampledMainTex.a, mask, surfaceData);
                    InitializeInputData(atlasUV, i.lightingUV, inputData);

                    return CombinedShapeLightShared(surfaceData, inputData);
                }
                
            ENDHLSL
        }
    }
}
