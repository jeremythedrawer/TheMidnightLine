Shader "Custom/s_atlasNPC"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.StencilOp)]
        _StencilOp("Stencil Operation", Float) = 1
    }


    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref 2
            Comp Always
            Pass [_StencilOp]
        }

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/AtlasSprites.hlsl"

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
                uint customBit : TEXCOORD5;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;
            
            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);
            float4 _AtlasTexture_TexelSize;

            TEXTURE2D(_CarriageBoundsTexture);
            SAMPLER(sampler_CarriageBoundsTexture);
            

            float3 _BlackColor;
            float3 _WhiteColor;

            float3 _MarkerColor1;
            float3 _MarkerColor2;
            
            float3 _MeridiaColor;
            float3 _VinroseColor;

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
                o.customBit = spriteData.customBit;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uvSize = i.uvSizeAndPos.xy;
                float2 uvPos = i.uvSizeAndPos.zw;

                float2 scale = i.scaleAndFlip.xy;
                float2 flip = i.scaleAndFlip.zw;

                float2 uv = i.uv;
                uv *= scale;
                uv = frac(i.uv);
                uv = (i.uv - 0.5) * flip + 0.5;
                uv *= uvSize;

                uv += uvPos;
                
                int bitMask = i.customBit;

                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, uv);

                half detail = tex.r;
                half outline = tex.g;
                int highlight = tex.b;

                int greenMask = saturate(bitMask & GREEN_BIT);
                int redMask = saturate(bitMask & RED_BIT);
                int blueMask = saturate(bitMask & BLUE_BIT);
                int meridiaColorMask = saturate(bitMask & MERIDIA_COLOR_BIT);
                int vinroseColorMask = saturate(bitMask & VINROSE_BIT);
                int invertMask = saturate(bitMask & INVERT_BIT);
                int oscillateMask = saturate(bitMask & OSCILLATE_BIT);

                detail *= redMask;
                outline *= greenMask;
                highlight *= blueMask;


                half3 meridiaColor = meridiaColorMask * _MeridiaColor;
                half3 vinroseColor = vinroseColorMask * _VinroseColor;
                
                half3 blackColor = (1 - (meridiaColorMask * vinroseColorMask)) * _BlackColor;

                half stripes = round(frac(i.uv.y * 2)); 
                int colKeyMask0 = saturate(bitMask & COLOR_KEY_BIT_0) * stripes;
                int colKeyMask1 = saturate(bitMask & COLOR_KEY_BIT_1) * (1 - stripes);

                half3 colKey0 = colKeyMask0 * _MarkerColor1;
                half3 colKey1 = colKeyMask1 * _MarkerColor2;

                int finalMask = max(detail, outline);
                finalMask = (finalMask ^ highlight);
                finalMask = lerp(finalMask, 1-finalMask, invertMask);

                half3 finalColor = finalMask + colKey0 + colKey1 + blackColor + meridiaColor + vinroseColor;
                finalColor = min(finalColor, _WhiteColor);

                float2 worldToTrain = (i.worldPos.xy - _TrainBoundsMin.xy) / _TrainBoundsSize.xy;
                half4 carriageSDF = SAMPLE_TEXTURE2D(_CarriageBoundsTexture, sampler_CarriageBoundsTexture, worldToTrain);
                float bayer = BayerX8(carriageSDF.r + 0.5,  i.positionHCS.y);

                float outside = max(step(worldToTrain.x, 0.0), step(1.0, worldToTrain.x));
                outside = max(outside,max(step(worldToTrain.y, 0.0),step(1.0, worldToTrain.y)));

                outside = max(outside, step(_TrainBoundsMin.z, i.worldPos.z));
                float camPos = -39;
                outside = max(outside, step(i.worldPos.z, camPos));
                
                float alpha = max(bayer, outside) * tex.a;
                clip(alpha - 0.001);

                return half4 (finalColor, 1);

            }
            ENDHLSL
        }
    }
}
