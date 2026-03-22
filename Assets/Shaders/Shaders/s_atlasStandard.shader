Shader "Custom/s_atlasStandard"
{
    Properties
    {
        [NoScaleOffset] _AtlasTexture("Texture Atlas", 2D) = "white"
    }

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
                uint instanceID : TEXCOORD1;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            Varyings vert(Attributes v)
            {
                Varyings o;

                AtlasSprite spriteData = _SpriteData[v.instanceID];

                float3 position = spriteData.position;
                float2 pivot = spriteData.pivot;
                float2 scale = spriteData.widthHeightFlip.xy;

                float2 localPos = v.positionOS.xy - pivot;
                localPos *= scale;

                float3 worldPos = float3(position.xy + localPos, position.z);

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.instanceID = v.instanceID;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                uint id = i.instanceID;
                AtlasSprite spriteData = _SpriteData[id];

                float4 uvSizeAndPos = spriteData.uvSizeAndPos;
                float4 widthHeightFlip = spriteData.widthHeightFlip;

                //i.uv *= widthHeightFlip.xy;
                //i.uv = frac(i.uv);
                //i.uv = (i.uv - 0.5) * widthHeightFlip.zw + 0.5;
                i.uv *= uvSizeAndPos.xy;
                i.uv += uvSizeAndPos.zw;
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);

                half3 finalColor = color.rgb;

                clip(color.a - 0.001);
                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}
