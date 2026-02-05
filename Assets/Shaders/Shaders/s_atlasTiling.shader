Shader "Custom/s_atlasTiling"
{
    Properties
    {
        _AtlasTexture ("Atlas Texture", 2D) = "white" {}
        _UVPosition ("UV Position", Vector) = (0, 0, 0, 0)
        _UVSize ("UV Size", Vector) = (0, 0, 0, 0)
        _PPU ("Pixels Per Unit", Float) = 0
        _UVSlice ("UV Slice", Vector) = (0, 0, 0, 0)
        _Width ("Width", Float) = 0
        _MetersTravelled ("Meters Travelled", Float) = 0
    }

    SubShader
    {
        Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline"}
        ZWrite On
        Cull Off
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD1;
                float uvWidth : TEXCOORD2;
                float2 testUV : TEXCOORD3;
            };

            TEXTURE2D(_AtlasTexture);
            SAMPLER(sampler_AtlasTexture);
            float4 _AtlasTexture_TexelSize;
            float4 _AtlasTexture_ST;
            CBUFFER_START(UnityPerMaterial)
                float2 _UVPosition;
                float2 _UVSize;
                float _PPU;
                float2 _UVSlice;
                float _Width;
                float _MetersTravelled;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                float aspect = _UVSize.x / _UVSize.y;

                float2 scrollUV = v.positionOS.xy + float2(_MetersTravelled, 0);
                scrollUV.x /= aspect;
                scrollUV = (_PPU / (_AtlasTexture_TexelSize.z * _UVSize.y)) * scrollUV;
                scrollUV *= _UVSize;

                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = scrollUV;
                o.uvWidth = (_Width * _PPU) / _AtlasTexture_TexelSize.z;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 spritePos = _UVPosition + i.uv;
                float2 totalSlice = _UVSlice * 2;

                float2 rightUVMask = (1 - totalSlice) - i.uv.x;
                rightUVMask = ((i.uvWidth - rightUVMask) + rightUVMask) < i.uv.x;

                float leftUVMask = _UVSlice.r > i.uv.x;
                float totalUVMask = rightUVMask || leftUVMask;
                float2 innerUVSize = _UVSize - totalSlice;

                float2 leftInnerUVSize = (i.uvWidth - _UVSlice) % innerUVSize + _UVSlice;
                float2 outerUV = float2(i.uvWidth - leftInnerUVSize.x, 0);
                outerUV *= rightUVMask;
                outerUV = spritePos - outerUV;
                float2 repeatingInnerUV =  (i.uv - _UVSlice) % innerUVSize;

                float2 innerUV = repeatingInnerUV + _UVPosition + _UVSlice;

                float2 finalUV = totalUVMask ? outerUV : innerUV;
                half4 color = SAMPLE_TEXTURE2D(_AtlasTexture, sampler_AtlasTexture, finalUV);

                float2 spriteTopRight = _UVSize + _UVPosition;
                spriteTopRight.x += i.uvWidth;
                spriteTopRight.x -= leftInnerUVSize;
                
                float finalAlpha =  spritePos.x > _UVPosition.x &&
                                    spritePos.y > _UVPosition.y && 
                                    spritePos.x < spriteTopRight.x && 
                                    spritePos.y < spriteTopRight.y;
                finalAlpha *= color.a;
                clip((finalAlpha * color.a) - 0.5);
                return half4 (color.xyz, 1);
            }
            ENDHLSL
        }
    }
}
