Shader "Custom/s_zoneParticles"
{
    Properties
    {
        _Atlas("Texture Atlas", 2D) = "white"
        _SpriteCount("Sprite Count", int) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/AtlasParticles.hlsl"
            #include "Assets/Shaders/HLSL/Random.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            StructuredBuffer<ZoneOutput> _Particles;

            TEXTURE2D(_Atlas);
            SAMPLER(sampler_Atlas);

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                int particleID : TEXCOORD1;
            };


            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings o;

                uint particleID = vertexID / 4;
                ZoneOutput p = _Particles[particleID];


                float2 pivot = p.worldPivotAndSize.xy;
                float2 size = p.worldPivotAndSize.zw;
                float2 scale = p.scale.xy;

                uint cornerID = vertexID % 4;
                float2 quadOffsets[4] = 
                {
                    float2(0, 0),
                    float2(0, 1),
                    float2(1, 1),
                    float2(1, 0)
                };

                float2 objPos = quadOffsets[cornerID];


                objPos *= size * scale;
                objPos += pivot;

                float3 worldPos = float3(p.position.xy + objPos, p.position.z);

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = quadOffsets[cornerID];
                o.particleID = particleID;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                ZoneOutput p = _Particles[i.particleID];

                float2 scale  = p.scale.xy;
                float2 uvSize = p.uvSizeAndPos.xy;
                float2 uvPos = p.uvSizeAndPos.zw;

                i.uv *= scale;
                i.uv = frac(i.uv);
                i.uv *= uvSize;
                i.uv += uvPos;
                
                //return half4(i.uv, 0, 1);
                half4 color = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, i.uv);
                if (color.a <= 0.5) discard;
                return color;
            }
            ENDHLSL
        }
    }
}
