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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/BackgroundParticles.hlsl"
            #include "Assets/Shaders/HLSL/Random.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            StructuredBuffer<BackgroundParticleOutput> _LODParticles;
            StructuredBuffer<float4> _UVSizeAndPos;

            TEXTURE2D(_Atlas);
            SAMPLER(sampler_Atlas);

            CBUFFER_START(UnityPerMaterial)
                int _BackgroundMask;
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
                Varyings o;

                uint particleID = vertexID * 0.25;
                BackgroundParticleOutput p = _LODParticles[particleID];

                // if ((p.backgroundMask & _BackgroundMask) == 0)
                // {
                //     o.positionHCS = float4(0, 0, 0, 1);
                //     o.uv = 0;
                //     o.visible = 0;
                //     return o;
                // }
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

                float4 uvSizeAndPos = _UVSizeAndPos[randMod];

                o.positionHCS = TransformWorldToHClip(p.position + offset);
                o.uv = quadOffsets[cornerID] * uvSizeAndPos.xy + uvSizeAndPos.zw;
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //return half4(0,0,0,1);
                // if (IN.visible == 0) discard;

                half4 color = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, IN.uv);
                if (color.a <= 0.5) discard;
                return color;
            }
            ENDHLSL
        }
    }
}
