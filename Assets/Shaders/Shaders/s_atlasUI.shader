Shader "Custom/s_atlasUI"
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
            #include "Assets/Shaders/HLSL/ColorSpace.hlsl"
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
                float3 worldPos : TEXCOORD1;
                float4 uvSizeAndPos : TEXCOORD2;
                float4 scaleAndFlip : TEXCOORD3;
                float4 custom : TEXCOORD4;
                int customBit : TEXCOORD5;
                float id : TEXCOORD6;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            TEXTURE2D(_CarriageBoundsTexture);
            SAMPLER(sampler_CarriageBoundsTexture);

            float3 _BlackColor;
            float3 _MeridiaColor;
            float3 _WhiteColor;

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

                o.id = v.instanceID;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {

                //return half4(i.id.xxx / 9 ,1 );
                float2 uvSize = i.uvSizeAndPos.xy;
                float2 uvPos = i.uvSizeAndPos.zw;
                
                float2 scale = i.scaleAndFlip.xy;
                float2 flip = i.scaleAndFlip.zw;

                i.uv *= scale;
                i.uv = frac(i.uv);
                i.uv = (i.uv - 0.5) * flip + 0.5;
                i.uv *= uvSize;
                i.uv += uvPos;

                int bitMask = i.customBit;
                int meridiaColorMask = saturate(bitMask & MERIDIA_COLOR_BIT);
                float3 meridiaColor = meridiaColorMask * _MeridiaColor;
                float3 blackColor = (1 - meridiaColorMask) * _BlackColor;
                blackColor += meridiaColor;

                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);

                int redMask = saturate(bitMask & RED_BIT);
                int greenMask = saturate(bitMask & GREEN_BIT);
                int blueMask = saturate(bitMask & BLUE_BIT);

                half whiteTex = tex.r * tex.g * tex.b;

                half redTex = tex.r * redMask;
                half greenTex = tex.g * greenMask;
                half blueTex = tex.b * blueMask;


                half fullMask = saturate(whiteTex + redTex + greenTex + blueTex);

                half invertTex = 1 - fullMask;

                int invertMask = saturate(bitMask & INVERT_BIT);
                half t = lerp(fullMask, invertTex, invertMask);

                float tCol = round(LinearLightness(i.custom.rgb));

                half useCol = saturate(ceil(i.custom.r + i.custom.g + i.custom.b));

                half3 darkCol = lerp(i.custom.rgb, blackColor, tCol);
                half3 lightCol = lerp(_WhiteColor, i.custom.rgb, tCol);
                darkCol = lerp(blackColor, darkCol, useCol);
                lightCol = lerp(_WhiteColor, lightCol, useCol);                
                half3 finalColor = lerp(darkCol, lightCol, t);
                
                
                clip((tex.a * i.custom.a) - 0.001);
                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}