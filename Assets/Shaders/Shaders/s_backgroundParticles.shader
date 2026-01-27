Shader "Custom/s_backgroundParticles"
{
    Properties
    {
        _Atlas("Texture Atlas", 2D) = "white"
        _BackgroundMask("Background Mask", int) = 0
        _LODLevel("LOD Level", int) = 0
        _SpriteCount("Sprite Count", int) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/BackgroundParticles.hlsl"
            #include "Assets/Shaders/HLSL/Random.hlsl"

            StructuredBuffer<BackgroundParticleOutput> _BGParticleOutputs;


            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_Atlas);
                SAMPLER(sampler_Atlas);
                StructuredBuffer<float2> _UVPositions;
                StructuredBuffer<float2> _UVSizes;
                int _BackgroundMask;
                int _LODLevel;
                int _SpriteCount;
            CBUFFER_END

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float visible : TEXCOORD1;
            };


            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings OUT;

                uint particleID = vertexID / 4;
                BackgroundParticleOutput p = _BGParticleOutputs[particleID];

                if ((p.backgroundMask & _BackgroundMask) == 0 || p.lodLevel != _LODLevel)
                {
                    OUT.positionHCS = float4(0, 0, 0, 1);
                    OUT.uv = 0;
                    OUT.visible = 0;
                    return OUT;
                }
                uint cornerID = vertexID % 4;
                float2 quadOffsets[4] = 
                {
                    float2(0, 0),
                    float2(0, 1),
                    float2(1, 1),
                    float2(1, 0)
                };

                float particleSize = 10 * p.parallaxFactor;
                float3 offset = float3(quadOffsets[cornerID] * particleSize, 0);

                int randMod = p.randID % _SpriteCount;
                float2 uvPos = _UVPositions[randMod];
                float2 uvSize = _UVSizes[randMod];

                OUT.positionHCS = TransformWorldToHClip(p.position + offset);
                OUT.uv = quadOffsets[cornerID] * uvSize + uvPos;
                OUT.visible = 1;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (IN.visible == 0) discard;

                half4 color = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, IN.uv);
                if (color.a <= 0.5) discard;
                return color;
            }
            ENDHLSL
        }
    }
}
