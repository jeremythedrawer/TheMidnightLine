Shader "Custom/s_atlasBayerRadial"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/AtlasSprites.hlsl"
            #include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"

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
                float4 uvSizeAndPos : TEXCOORD1;
                float4 scaleAndFlip : TEXCOORD2;
                float4 custom : TEXCOORD3;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            float3 _MainColor;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);
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
                objPos -= pivot;

                float3 worldPos = float3(position.xy + objPos, position.z);

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.uvSizeAndPos = spriteData.uvSizeAndPos;
                o.scaleAndFlip = spriteData.scaleAndFlip;
                o.custom = spriteData.custom;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                int uncoveredSpriteIndex = (int)i.custom.x;

                AtlasSprite uncoveredSprite = _SpriteData[uncoveredSpriteIndex];

                float2 coveredUVSize = i.uvSizeAndPos.xy;
                float2 coveredUVPos = i.uvSizeAndPos.zw;
                
                float2 scale = i.scaleAndFlip.xy;
                float2 flip = i.scaleAndFlip.zw;

                float2 normUV = i.uv;
                float2 coveredSpriteUV = i.uv;

                coveredSpriteUV *= coveredUVSize ;
                coveredSpriteUV += coveredUVPos ;
                
                half4 coveredSprite = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, coveredSpriteUV);

                float2 centerUV = normUV * 2 - 1;
                float radial = length(centerUV);

                float t = i.custom.x * 2 - 1;
                half alpha = (radial) - t;

                alpha = saturate(alpha); 
                alpha = BayerMatrix(alpha, 1, i.positionHCS.xy);

                half3 finalColor = coveredSprite.rgb + _MainColor;
                clip(alpha - 0.001);

                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}
