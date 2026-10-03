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
            float3 _WhiteColor;
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
                float2 uv = SpriteUV(i.uv, i.uvSizeAndPos, i.scaleAndFlip);

                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, uv);
                half whiteTex = tex.r * tex.g * tex.b;

                int bitMask = i.customBit;

                int redMask = saturate(bitMask & RED_BIT);
                int greenMask = saturate(bitMask & GREEN_BIT);
                int blueMask = saturate(bitMask & BLUE_BIT);

                float time = i.custom.x;
                half detail = tex.r * time * redMask;
                half cross = tex.b * time * blueMask;
                
                float2 centerUV = i.uv * 2 - 1;
                half circle = length(centerUV);

                half revealMask = max(detail, cross);
                revealMask = BayerX8(revealMask - circle, i.positionHCS.y);
                half hover = tex.g * greenMask;

                half finalMask = saturate(revealMask + hover + whiteTex);
                half3 finalColor = finalMask + _BlackColor;
                clip(tex.a - 0.001);
                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}
