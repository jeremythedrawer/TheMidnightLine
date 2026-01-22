Shader "Custom/s_backgroundParticles"
{
    Properties
    {
        _Atlas("Texture Atlas", 2D) = "white"
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        AlphaToMask On
        AlphaTest Greater 0.5
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/BackgroundParticles.hlsl"
            #include "Assets/Shaders/HLSL/Random.hlsl"

            StructuredBuffer<BackgroundAttributes> _BGParticles;

            TEXTURE2D(_Atlas);
            SAMPLER(sampler_Atlas);
            StructuredBuffer<float2> _UVPositions;
            StructuredBuffer<float2> _UVSizes;
            int _SpriteCount;
            int _BackgroundTypeMask;

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings OUT;

                uint particleID = vertexID / 4;
                BackgroundAttributes p = _BGParticles[particleID];

                uint cornerID = vertexID % 4;
                float2 quadOffsets[4] = 
                {
                    float2(0, 0),
                    float2(0,  1),
                    float2(1,  1),
                    float2(1, 0)
                };

                float2 uvOffsets[4] = 
                {
                    float2(0,0),
                    float2(0,1),
                    float2(1,1),
                    float2(1,0)
                };

                float particleSize = 10.0;
                float3 offset = float3(quadOffsets[cornerID] * particleSize, 0);
                OUT.positionHCS = TransformWorldToHClip(p.position + offset);

                uint randID = HashIntToInt(particleID);
                randID = randID % _SpriteCount;
                float2 uvPos = _UVPositions[randID];
                float2 uvSize = _UVSizes[randID];

                OUT.uv = uvOffsets[cornerID] * uvSize + uvPos;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, IN.uv);
                return color;
            }
            ENDHLSL
        }
    }
}
