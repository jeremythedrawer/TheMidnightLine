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
                int customBit : TEXCOORD4;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            float3 _BlackColor;
            float3 _MeridiaColor;

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
                o.customBit = spriteData.customBit;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 coveredUVSize = i.uvSizeAndPos.xy;
                float2 coveredUVPos = i.uvSizeAndPos.zw;
                
                float2 scale = i.scaleAndFlip.xy;
                float2 flip = i.scaleAndFlip.zw;

                float2 normUV = i.uv;
                i.uv *= coveredUVSize;
                i.uv += coveredUVPos;
                
                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                tex.r = lerp(tex.r, 1 - tex.r, i.custom.w);

                int toReveal = saturate(i.customBit & DIAGONAL_TEXTURE_BIT);
                int toHide = floor(1 - toReveal);

                float revealT = (i.custom.x * toReveal) * 2 - 1;
                float hideT = (i.custom.x * toHide) * 2 - 1.5;

                float2 centerUV = normUV * 2 - 1;
                float radial = length(centerUV);
                half alpha = radial - revealT;
                alpha = saturate(alpha); 
                alpha = BayerMatrix(alpha, 1, i.positionHCS.xy);

                float diagonalGradient = normUV.y + normUV.x;

                half diagonal = diagonalGradient + hideT;
                diagonal = saturate(diagonal) * 0.125;
                diagonal = BayerX8(diagonal, i.positionHCS.x - i.positionHCS.y);

                int meridiaColorMask = saturate(i.customBit & MERIDIA_COLOR_BIT);
                float3 meridiaColor = meridiaColorMask * _MeridiaColor;
                float3 blackColor = (1 - meridiaColorMask) * _BlackColor;

                half3 finalColor = tex.r + blackColor + diagonal + meridiaColor;
                clip(alpha - 0.001);

                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}
