Shader "Custom/s_atlasPattern"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite On
        ZTest LEqual
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/AtlasSprites.hlsl"
            #include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"
            #include "Assets/Shaders/HLSL/ColorSpace.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 uvSizeAndPos : TEXCOORD2;
                float4 scaleAndFlip : TEXCOORD3;
                float4 custom : TEXCOORD4;
                int customBit : TEXCOORD5;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);
            float4 _AtlasTexture_TexelSize;

            TEXTURE2D(_PatternTexture);
            SAMPLER(sampler_PatternTexture);
            float4 _PatternTexture_TexelSize;

            float3 _BlackColor;
            float3 _WhiteColor;
            float3 _MeridiaColor;

            Varyings vert(Attributes v)
            {
                Varyings o;

                AtlasSprite spriteData = _SpriteData[v.instanceID];

                float3 position = spriteData.position.xyz;
                
                float2 pivot = spriteData.pivotAndSize.xy;
                float2 size = spriteData.pivotAndSize.zw;
                
                float2 scale = spriteData.scaleAndFlip.xy;
                float2 objPos = v.positionOS.xy;

                objPos *= size * scale;
                objPos += pivot;

                float3 worldPos = float3(position.xy + objPos, position.z);
                o.worldPos = worldPos;
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.uvSizeAndPos = spriteData.uvSizeAndPos;
                o.scaleAndFlip = spriteData.scaleAndFlip;
                o.custom = spriteData.custom;
                o.customBit = spriteData.customBit;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uvSize = i.uvSizeAndPos.xy;
                float2 uvPos = i.uvSizeAndPos.zw;
                
                float2 scale = i.scaleAndFlip.xy;
                float2 flip = i.scaleAndFlip.zw;

                float2 atlasUV = i.uv;
                float2 patternUV = i.uv;

                atlasUV *= scale;
                atlasUV = frac(i.uv);
                atlasUV = (i.uv - 0.5) * flip + 0.5;
                atlasUV *= uvSize;
                atlasUV += uvPos;

                patternUV *= i.custom.xy;
                patternUV += i.custom.zw;
                
                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, atlasUV);
                clip((tex.a) - 0.001);
                half4 patternTex = SAMPLE_TEXTURE2D(_PatternTexture, sampler_PatternTexture, patternUV);

                half3 finalCol = lerp (_BlackColor, _WhiteColor, tex.r * patternTex.r);

                return half4 (finalCol, 1);
            }
            ENDHLSL
        }
    }
}