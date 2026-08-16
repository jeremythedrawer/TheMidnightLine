Shader "Custom/s_atlasNPCPickerIcons"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
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

            TEXTURE2D(_DiagonalTexture);
            SAMPLER(sampler_DiagonalTexture);
            float4 _DiagonalTexture_TexelSize;

            TEXTURE2D(_StripesTexture);
            SAMPLER(sampler_StripesTexture);

            float3 _BlackColor;
            float3 _ColorKey0;
            float3 _ColorKey1;
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

                i.uv *= scale;
                i.uv = frac(i.uv);
                i.uv = (i.uv - 0.5) * flip + 0.5;
                i.uv *= uvSize;
                
                float2 diagonalTexUV = i.uv;
                i.uv += uvPos;

                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                half4 diagonalTex = SAMPLE_TEXTURE2D(_DiagonalTexture, sampler_DiagonalTexture, diagonalTexUV);
                half4 stripesTex = SAMPLE_TEXTURE2D(_StripesTexture, sampler_StripesTexture, diagonalTexUV);
                
                int bitMask = i.customBit;

                int diagonalMask = saturate(bitMask & DIAGONAL_TEXTURE_BIT);
                half3 diagonal = diagonalMask * diagonalTex.r;
                
                int invertMask = saturate(bitMask & INVERT_BIT);
                
                int colorMask = bitMask & 0x03;

                int colKeyMask0 = colorMask == COLOR_KEY_BIT_0;
                int colKeyMask1 = colorMask == COLOR_KEY_BIT_1;
                int colKeyMask01 = colorMask == (COLOR_KEY_BIT_0 | COLOR_KEY_BIT_1);

                half3 colKey0 = colKeyMask0 * _ColorKey0;
                half3 colKey1 = colKeyMask1 * _ColorKey1;

                half invertPatternR = 1 - stripesTex.r;

                half3 colKey01 = colKeyMask01 * ((_ColorKey0 * stripesTex.r) + (_ColorKey1 * invertPatternR));

                int meridiaColorMask = saturate(bitMask & MERIDIA_COLOR_BIT);
                half3 meridiaColor = meridiaColorMask * _MeridiaColor;

                half3 blackColor = (1 - meridiaColorMask) * _BlackColor;

                half3 finalColor = lerp(tex.r, 1 - tex.r, invertMask);
                finalColor += diagonal + colKey0 + colKey1 + colKey01 + blackColor + meridiaColor;
                
                return half4 (finalColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}