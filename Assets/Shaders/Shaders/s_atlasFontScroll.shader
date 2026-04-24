Shader "Custom/s_atlasFontScroll"
{
    Properties
    {
        [NoScaleOffset] _AtlasTexture("Texture Atlas", 2D) = "white"
        _ScrollSpeed("Scroll Speed", Float) = 0.2
        _Spacing("Spacing", Float) = 0.2
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
                float2 objPos : TEXCOORD1;
                uint instanceID : TEXCOORD2;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            CBUFFER_START(UnityPerMaterial)
                float _ScrollSpeed;
                float _Spacing;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;

                AtlasSprite spriteData = _SpriteData[v.instanceID];


                float3 position = spriteData.position.xyz;
                
                float2 pivot = spriteData.pivotAndSize.xy;
                float2 size = spriteData.pivotAndSize.zw;

                float2 objPos = v.positionOS.xy;

                objPos *= size;
                float time = _Time.y * _ScrollSpeed;
                //objPos += pivot;

                objPos.x += fmod(-time + pivot.x, spriteData.custom.x + _Spacing) * (spriteData.custom.y) + (spriteData.custom.y); //custom.x is the total bounds of the text
                float3 worldPos = float3(position.xy + objPos, position.z);

                o.objPos = objPos;
                o.positionHCS = TransformWorldToHClip(worldPos);
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

                i.uv = frac(i.uv);
                i.uv *= uvSize;
                i.uv += uvPos;
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);

                half3 finalColor = color.rgb + 1;

                float mask = 10; //TODO: Set on custom.y
                half rightMask = step(i.objPos.x, spriteData.custom.y);
                half leftMask = step(0,i.objPos.x);
                half alpha = color.a * rightMask * leftMask;
                clip(alpha - 0.001);
                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}
