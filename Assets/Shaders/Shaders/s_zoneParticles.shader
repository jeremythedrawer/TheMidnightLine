Shader "Custom/s_zoneParticles"
{
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }

        ZWrite On
        ZTest LEqual
        Cull Off
        Blend Off
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
                float4 quadAndPivotScales   : TEXCOORD2;
                float4 uvSizeAndPos         : TEXCOORD3;
            };

            StructuredBuffer<float4> _Particles;
            StructuredBuffer<ParticleSprite> _SpriteData;
            StructuredBuffer<float4> _QuadAndPivotScales;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            uint _QuadScaleCount;

            uint _ParticleOffset;

            uint _SpriteCount;
            uint _SpritesPerParticle;

            float _DayNight;
            float3 _MainColor;
            float _DayNightFactor;


            Varyings vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                Varyings o;

                uint particleID = floor(instanceID / _SpritesPerParticle) + _ParticleOffset;
                float4 p = _Particles[particleID];

                uint spriteID = instanceID % _SpriteCount;
                ParticleSprite s = _SpriteData[spriteID];

                float4 quadAndPivotScales = _QuadAndPivotScales[instanceID % _QuadScaleCount];

                uint quadVertexID = vertexID % 6;
                float2 objPos = QUAD_TRIANGLE_OFFSETS[quadVertexID];

                float2 pivot = s.worldPivotAndSize.xy;
                float2 size = s.worldPivotAndSize.zw;
                float2 quadScale = quadAndPivotScales.xy;
                float2 pivotScale = quadAndPivotScales.zw;

                objPos *= size;
                objPos *= quadScale;
                objPos += pivot;
                objPos += pivotScale;

                float3 worldPos = float3(p.xy + objPos, p.z);

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = QUAD_TRIANGLE_OFFSETS[quadVertexID];
                o.particle = p;
                o.quadAndPivotScales = quadAndPivotScales;
                o.uvSizeAndPos = s.uvSizeAndPos;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float4 p = i.particle;

                float2 quadScale = i.quadAndPivotScales.xy;
                float2 uvSize = i.uvSizeAndPos.xy;
                float2 uvPos = i.uvSizeAndPos.zw;

                i.uv *= quadScale;
                i.uv = frac(i.uv);
                i.uv *= uvSize;
                i.uv += uvPos;

                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                clip(tex.a - 0.001);

                half color = tex.r;

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
