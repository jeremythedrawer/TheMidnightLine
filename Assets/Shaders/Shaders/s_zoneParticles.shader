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
            #include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"
            #include "Assets/Shaders/HLSL/AtlasParticles.hlsl"
            #include "Assets/Shaders/HLSL/Random.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            StructuredBuffer<ZoneOutput> _Particles;

            TEXTURE2D(_Atlas);
            SAMPLER(sampler_Atlas);

            float _DayNight;
            float3 _MainColor;
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
                half4 tex = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, i.uv);

                half color = tex.r;

                int maxPos = BACK_MIN + BACK_SIZE;
                int minPos = MID_MIN;

                half bayerFactor = (p.position.z - MID_MIN) / (maxPos - minPos);
                bayerFactor = bayerFactor * 0.5 + 0.5;
                half bayerValue = bayerFactor * (_DayNight * 1.75 - 0.875);

                half bayer = BayerX8((color - bayerValue), i.positionHCS.y);
                half3 finalColor = bayer + _MainColor;
                //finalColor -= (1 - p.parallaxFactor) * (_DayNight * 2 - 1);
                clip(tex.a - 0.001);
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
