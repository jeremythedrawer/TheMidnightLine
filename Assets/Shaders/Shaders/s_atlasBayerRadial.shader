Shader "Custom/s_atlasBayerRadial"
{
    Properties
    {
        [NoScaleOffset] _AtlasTexture("Texture Atlas", 2D) = "white"
        [NoScaleOffset] _NoiseTexture("Noise Texture", 2D) = "white"
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
                float2 uv : TEXCOORD0;
                uint instanceID : TEXCOORD1;
            };

            StructuredBuffer<AtlasSprite> _SpriteData;

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);
            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);
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

                float2 normUV = i.uv;
                i.uv *= scale;
                i.uv = frac(i.uv);
                i.uv = (i.uv - 0.5) * flip + 0.5;
                i.uv *= uvSize;
                i.uv += uvPos;
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, i.uv);

                half4 noiseTex = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, normUV);
                float2 centerUV = normUV * 2 - 1;
                float radial = length(centerUV);
                
                float noise = noiseTex.r * 2 - 1;
                half alpha = color.a + ((radial) - spriteData.custom.x);
                //return alpha.xxxx;
                alpha = saturate(alpha); 
                alpha = BayerMatrix(alpha, 1, i.positionHCS.xy);
                clip(alpha - 0.001);

                return half4 (color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
