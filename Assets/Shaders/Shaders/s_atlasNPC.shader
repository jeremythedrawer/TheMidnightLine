Shader "Custom/s_atlasNPC"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
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
                float4 uvSizeAndPos : TEXCOORD0;
                float4 scaleAndFlip: TEXCOORD1;
                float4 custom : TEXCOORD2;

                float3 worldPos : TEXCOORD3;
                
                float2 uv : TEXCOORD4;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;
            
            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);
            float4 _AtlasTexture_TexelSize;

            TEXTURE2D(_CarriageBoundsTexture);
            SAMPLER(sampler_CarriageBoundsTexture);

            TEXTURE2D(_DiagonalTexture);
            SAMPLER(sampler_DiagonalTexture);

            float3 _BlackColor;
            float3 _ColorKey0;
            float3 _ColorKey1;
            float3 _ColorKey2;

            float4 _TrainBoundsMin;
            float4 _TrainBoundsSize;

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

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.uvSizeAndPos = spriteData.uvSizeAndPos;
                o.scaleAndFlip = spriteData.scaleAndFlip;
                o.custom = spriteData.custom;
                o.worldPos = worldPos;
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

                float outline = 0;
                for (int index = 0; index < 4; index++)
                {
                    float2 uvOffset = i.uv + (BOX_BLUR_OFFSET[index] / _AtlasTexture_TexelSize.zw);
                    half4 blurTex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, uvOffset);
                    outline += blurTex.a;
                }
                outline /= 4;
                outline *= (1 - outline);
                outline = ceil(outline);

                uint colorKeyMask = (uint)i.custom.x;

                uint colKeyMask0 = (colorKeyMask & (1 << 0)) != 0;
                uint colKeyMask1 = (colorKeyMask & (1 << 1)) != 0;
                uint colKeyMask2 = (colorKeyMask & (1 << 2)) != 0;
                uint diagonalMask = (colorKeyMask & (1 << 3)) != 0;

                half4 diagonalTex = SAMPLE_TEXTURE2D(_DiagonalTexture, sampler_DiagonalTexture, diagonalTexUV);

                half3 colKey0 = colKeyMask0 * _ColorKey0;
                half3 colKey1 = colKeyMask1 * _ColorKey1;
                half3 colKey2 = colKeyMask2 * _ColorKey2;
                half3 diagonal = diagonalMask * diagonalTex.rgb;

                half mouseColor = i.custom.y;
                half ticketCheckMask = i.custom.z;
                half ticketCheckHover = i.custom.w;

                outline = lerp(outline, 1 - outline, ticketCheckHover);

                half3 finalColor = (tex.rgb * ticketCheckMask) + diagonal + colKey0 + colKey1 + colKey2 + _BlackColor + (outline * (1 - ticketCheckMask));

                float bayerColMask = BayerX8(mouseColor * 0.5, i.positionHCS.y);
                half3 bayerColor = lerp(finalColor, 1 - ticketCheckHover, bayerColMask);
                
                float2 worldToTrain = (i.worldPos.xy - _TrainBoundsMin.xy) / _TrainBoundsSize.xy;
                half4 carriageSDF = SAMPLE_TEXTURE2D(_CarriageBoundsTexture, sampler_CarriageBoundsTexture, worldToTrain);
                float bayer = BayerX8(carriageSDF.r + 0.5,  i.positionHCS.y);

                float outside = max(step(worldToTrain.x, 0.0), step(1.0, worldToTrain.x));
                outside = max(outside,max(step(worldToTrain.y, 0.0),step(1.0, worldToTrain.y)));
                outside = max(outside, step(_TrainBoundsMin.z,i.worldPos.z));
                float alpha = max(bayer, outside) * tex.a;
                clip(alpha - 0.001);

                return half4 (bayerColor, 1);
            }
            ENDHLSL
        }
    }
}
