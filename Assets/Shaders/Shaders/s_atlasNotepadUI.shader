Shader "Custom/s_atlasNotepadUI"
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
                float4 pivotAndSize : TEXCOORD4;
                float4 custom : TEXCOORD5;
                int customBit : TEXCOORD6;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            TEXTURE2D(_PageFlipMaskTexture);
            SAMPLER(sampler_PageFlipMaskTexture);
            float4 _PageFlipMaskTexture_TexelSize;

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
                o.pivotAndSize = spriteData.pivotAndSize;
                o.custom = spriteData.custom;
                o.customBit = spriteData.customBit;
                return o;

            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = SpriteUV(i.uv, i.uvSizeAndPos, i.scaleAndFlip);
                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, uv);

                half4 finalColor = UIColor(i.customBit, tex, i.custom, _BlackColor, _WhiteColor, _MeridiaColor, i.positionHCS.y);

                float2 normScreenUV = i.positionHCS.xy / _ScreenParams.xy;
                half4 pageFlipTex = SAMPLE_TEXTURE2D(_PageFlipMaskTexture, sampler_PageFlipMaskTexture, normScreenUV);

                int invertMask = saturate(i.customBit & INVERT_NOTEPAD_BIT);

                half pageFlipMask = lerp(pageFlipTex.a, 1 - pageFlipTex.a, invertMask);
                half alpha = finalColor.a * pageFlipMask;


                clip(alpha - 0.001);

                return finalColor;
            }
            ENDHLSL
        }
    }
}