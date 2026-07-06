Shader "Custom/s_atlasEdgeScroll"
{
	Properties
    {
        _AtlasTexture("Texture Atlas", 2D) = "white"
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"
            #include "Assets/Shaders/HLSL/AtlasParticles.hlsl"
            #include "Assets/Shaders/HLSL/Random.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            struct Varyings
            {
                float4 positionHCS          : SV_POSITION;
                float2 uv                   : TEXCOORD0;
                float4 particle             : TEXCOORD1;
                float2 edgeScale            : TEXCOORD2;
                float4 uvSizeAndPos         : TEXCOORD3;
            };

            StructuredBuffer<float4> _Particles;
            StructuredBuffer<ParticleSprite> _SpriteData;
            StructuredBuffer<EdgeSprite> _EdgeSpriteData;
            
            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            uint _ParticleOffset;

            float _DayNight;
            float3 _BlackColor;
            float _DayNightFactor;

            float4 _TrainBoundsMin;
            float4 _TrainBoundsSize;

            Varyings vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                Varyings o;

                float4 p = _Particles[_ParticleOffset];
                
                EdgeSprite e = _EdgeSpriteData[instanceID];

                uint edgeSpriteIndex = e.spriteIndex;
                float3 edgeOffset = e.offset.xyz;
                float2 edgeScale = e.scale.xy;

                ParticleSprite s = _SpriteData[edgeSpriteIndex];

                uint quadVertexID = vertexID % 6;
                float2 objPos = QUAD_TRIANGLE_OFFSETS[vertexID];

                float2 pivot = s.worldPivotAndSize.xy;
                float2 size = s.worldPivotAndSize.zw;

                objPos *= size;
                objPos *= edgeScale;
                objPos += pivot;
                objPos += edgeOffset.xy;

                float3 worldPos = float3(p.xy + objPos, p.z + edgeOffset.z);

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = QUAD_TRIANGLE_OFFSETS[quadVertexID];

                o.particle = p;
                o.edgeScale = edgeScale;
                o.uvSizeAndPos = s.uvSizeAndPos;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float4 p = i.particle;

                float2 edgeScale = i.edgeScale;

                float2 uvSize = i.uvSizeAndPos.xy;
                float2 uvPos = i.uvSizeAndPos.zw;

                i.uv *= edgeScale;
                i.uv = frac(i.uv);
                i.uv *= uvSize;
                i.uv += uvPos;

                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                clip(tex.a - 0.001);

                half color = tex.r;

                half minPos = _TrainBoundsMin.z + _TrainBoundsSize.z;
                half bayerFactor = (p.z - minPos) / (FAR_CLIP - minPos);
                half bayerValue = bayerFactor * (_DayNight * 1.75 - 0.875);
                half bayer = BayerX8((tex.r - bayerValue), i.positionHCS.y);

                half3 finalColor = bayer + _BlackColor;
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
