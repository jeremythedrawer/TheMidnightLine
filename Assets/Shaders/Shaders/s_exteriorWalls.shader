Shader "Custom/s_exteriorWalls"
{
    Properties
    {
        [NoScaleOffset] _AtlasTexture("Texture Atlas", 2D) = "white"
        _UVSizeAndPos ("UV Size And Pos", Vector) = (0,0,0,0)
        _WidthHeightFlip ("Width Height And Flip", Vector) = (0,0,0,0)
        _Alpha("Alpha", Float) = 0
        _WorldClip("World Clip", Float) = 0
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
                float3 worldPos : TEXCOORD1;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float  _Alpha;
                float _WorldClip;
            CBUFFER_END

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            Varyings vert(Attributes v)
            {
                Varyings o;

                AtlasSprite spriteData = _SpriteData[v.instanceID];


                float3 position = spriteData.position;
                
                float2 pivot = spriteData.pivotAndSize.xy;
                float2 size = spriteData.pivotAndSize.zw;
                
                float2 scale = spriteData.scaleAndFlip.xy;
                float2 objPos = v.positionOS.xy;

                objPos *= size * scale;
                objPos -= pivot;

                o.worldPos = float3(position.xy + objPos, position.z);

                o.worldPos.y -= (1 - _Alpha) * 3.3;

                o.positionHCS = TransformWorldToHClip(o.worldPos);
                o.uv = v.uv;
                o.instanceID = v.instanceID;

                return o;
            }


            half4 frag(Varyings i) : SV_Target
            {
                uint id = i.instanceID;
                AtlasSprite spriteData = _SpriteData[id];

                float2 uvSize = spriteData.uvSizeAndPos.xy;
                float2 uvPos = spriteData.uvSizeAndPos.zw;
                
                float2 scale = spriteData.scaleAndFlip.xy;
                float2 flip = spriteData.scaleAndFlip.zw;

                i.uv *= scale.xy;
                i.uv = frac(i.uv);
                i.uv = (i.uv - 0.5) * flip + 0.5;
                i.uv *= uvSize;
                i.uv += uvPos;
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);

                half3 finalColor = color.rgb;

                half worldClip = step(_WorldClip, i.worldPos.y);
                float alpha = color.a * worldClip;
                clip(alpha - 0.001);
                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}
