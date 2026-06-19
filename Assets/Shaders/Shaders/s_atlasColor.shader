Shader "Custom/s_atlasColor"
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
            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            TEXTURE2D(_CarriageBoundsTexture);
            SAMPLER(sampler_CarriageBoundsTexture);

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
                i.uv += uvPos;

                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);

                
                half border = saturate(tex.r + tex.g);
                half invertT = i.custom.a;
                half3 invertTex = lerp(tex.rgb, 1 - tex.rgb, invertT);
                half whiteTex = saturate(invertTex.g - invertTex.r);
                half blackTex = (1- invertTex.g);
                
                half t = round(LinearLightness(i.custom.rgb));

                half3 finalCol = lerp((blackTex + i.custom.rgb) * border , whiteTex * i.custom.rgb, t);

                clip((tex.a) - 0.001);
                return half4 (finalCol.rgb, 1);
            }
            ENDHLSL
        }
    }
}