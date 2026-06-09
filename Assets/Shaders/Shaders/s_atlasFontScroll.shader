Shader "Custom/s_atlasFontScroll"
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
                float2 objPos : TEXCOORD1;
                float4 uvSizeAndPos : TEXCOORD2;
                float scrollBoundWidth : TEXCOORD3;

            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);

            Varyings vert(Attributes v)
            {
                Varyings o;

                AtlasSprite spriteData = _SpriteData[v.instanceID];

                float3 position = spriteData.position.xyz;
                float2 pivot = spriteData.pivotAndSize.xy;
                float2 size = spriteData.pivotAndSize.zw;

                float textBoundWidth = spriteData.custom.x;
                float scrollBoundWidth = spriteData.custom.y; 
                float scrollSpeed = spriteData.custom.z;
                float scrollingBackwards = spriteData.custom.w;

                float buffer = 0.05;
                float repeatWidth = max(scrollBoundWidth, textBoundWidth);
                
                float time = _Time.y * scrollSpeed;

                float2 objPos = v.positionOS.xy;
                objPos *= size;

                float bufferSign =  buffer * (scrollingBackwards * 2 - 1);
                objPos.x += fmod(time + pivot.x, repeatWidth + (buffer * 2)) + bufferSign;
                objPos.x += repeatWidth * scrollingBackwards;
                
                objPos.y += pivot.y;
                float3 worldPos = float3(position.xy + objPos, position.z);

                o.objPos = objPos;
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.uvSizeAndPos = spriteData.uvSizeAndPos;
                o.scrollBoundWidth = scrollBoundWidth;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uvSize = i.uvSizeAndPos.xy;
                float2 uvPos = i.uvSizeAndPos.zw;
                i.uv *= uvSize;
                i.uv += uvPos;
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);

                half3 finalColor = color.rgb;

                half rightMask = step(i.objPos.x, i.scrollBoundWidth);
                half leftMask = step(0,i.objPos.x);
                half alpha = color.a * rightMask * leftMask;

                clip(alpha - 0.001);
                return half4 (finalColor, 1);
            }
            ENDHLSL
        }
    }
}
