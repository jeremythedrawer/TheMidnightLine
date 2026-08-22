Shader "Custom/s_exteriorWalls"
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
            #include "Assets/Shaders/HLSL/AtlasParticles.hlsl"
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 uvSizeAndPos : TEXCOORD1;
                float4 scaleAndFlip : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                float3 spritePos : TEXCOORD4;
                float4 custom : TEXCOORD5;
            };

            CBUFFER_START(UnityPerMaterial)
                float _WorldClip;
            CBUFFER_END

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            float3 _BlackColor;
            float3 _WhiteColor;
            float _DayNight;
            float _DayNightFactor;
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

                o.worldPos = float3(position.xy + objPos, position.z);

                o.worldPos.y -= spriteData.custom.z * size.y;
                o.spritePos = position;
                o.positionHCS = TransformWorldToHClip(o.worldPos);
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

                i.uv *= scale.xy;
                i.uv += i.custom.xy;
                i.uv = frac(i.uv);
                i.uv = (i.uv - 0.5) * flip + 0.5;
                i.uv *= uvSize;
                i.uv += uvPos;
                half4 tex = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);
                float divisor = 35;
                half normDepth = round(i.worldPos.z/divisor) * divisor / FAR_CLIP;
                half3 nightFactor = lerp(_WhiteColor, _BlackColor, _DayNight * normDepth);
                half grey = tex.r + (-(_DayNight * 1.1 - 0.9) * normDepth);
                half3 finalColor = lerp(_BlackColor, nightFactor, saturate(grey));

                half worldClip = step(i.spritePos.y, i.worldPos.y);
                float alpha = tex.a * worldClip;
                clip(alpha - 0.001);
                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}
