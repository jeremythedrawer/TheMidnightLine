Shader "Custom/s_atlasScroll"
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
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint spriteID : TEXCOORD1;
                uint particleID : TEXCOORD2;
            };

            StructuredBuffer<float4> _Particles;
            StructuredBuffer<ParticleSprites> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            uint _SpriteIndex;
            uint _SpriteCount;

            uint _ParticleOffset;

            float _DayNight;
            float3 _MainColor;
            float _DayNightFactor;


            Varyings vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                Varyings o;

                uint particleID = floor(instanceID / _SpriteCount) + _ParticleOffset;
                float4 p = _Particles[particleID];

                uint spriteID = _SpriteIndex + (instanceID % _SpriteCount);

                ParticleSprites s = _SpriteData[spriteID];

                uint quadVertexID = vertexID % 6;
                float2 objPos = QUAD_TRIANGLE_OFFSETS[vertexID];

                float2 pivot = s.worldPivotAndSize.xy;
                float2 size = s.worldPivotAndSize.zw;
                float2 scale = s.scaleAndFlip.xy;

                objPos *= size * scale;
                objPos += pivot;

                float3 worldPos = float3(p.xy + objPos, p.z);

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = QUAD_TRIANGLE_OFFSETS[quadVertexID];

                o.spriteID = spriteID;
                o.particleID = particleID;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                ParticleSprites s = _SpriteData[i.spriteID];
                float4 p = _Particles[i.particleID];

                float2 scale  = s.scaleAndFlip.xy;
                float2 uvSize = s.uvSizeAndPos.xy;
                float2 uvPos = s.uvSizeAndPos.zw;

                i.uv *= scale;
                i.uv = frac(i.uv);
                i.uv *= uvSize;
                i.uv += uvPos;

                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                clip(tex.a - 0.001);

                half color = tex.r;

                return color.xxxx;

                int maxPos = BACK_MIN + BACK_SIZE;
                int minPos = MID_MIN;

                half bayerFactor = (p.z - MID_MIN) / (maxPos - minPos);
                bayerFactor = bayerFactor * 0.5 + 0.5;
                half bayerValue = bayerFactor * (_DayNight * 1.75 - 0.875);

                half bayer = BayerX8((color - bayerValue), i.positionHCS.y);
                half3 finalColor = bayer + _MainColor;
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
