#define METERS_TRAVELLED_DIVISOR 1000

#define COLOR_KEY_BIT_0 1 << 0
#define COLOR_KEY_BIT_1 1 << 1
#define COLOR_KEY_BIT_2 1 << 2

#define DIAGONAL_TEXTURE_BIT 1 << 3
#define MERIDIA_COLOR_BIT 1 << 4
#define INVERT_BIT 1 << 5
#define OUTLINE_BIT 1 << 6
#define TEXTURE_BIT 1 << 7
#define RED_BIT 1 << 8
#define GREEN_BIT 1 << 9
#define BLUE_BIT 1 << 10
#define INVERT_NOTEPAD_BIT 1 << 11

#include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"
#include "Assets/Shaders/HLSL/ColorSpace.hlsl"

static const float2 BOX_BLUR_OFFSET[4] =
{
    float2 (-1, -1),
    float2 (1, -1),
    float2 (-1, 1),
    float2 (1, 1),
};

struct AtlasSprite
{
    float4 position;
    float4 pivotAndSize;
    float4 uvSizeAndPos;
    float4 scaleAndFlip;
    float4 custom;
    int customBit;
};


half4 UIColor(int bitMask, half4 tex, float4 custom, float3 blackColor, float3 whiteColor, float3 meridiaColor, float posHCSY)
{
    int meridiaColorMask = saturate(bitMask & MERIDIA_COLOR_BIT);
    meridiaColor *= meridiaColorMask;
    blackColor = (1 - meridiaColorMask) * blackColor;
    blackColor += meridiaColor;

    half alpha = BayerX8((tex.a * custom.a), posHCSY);

    int redMask = saturate(bitMask & RED_BIT);
    int greenMask = saturate(bitMask & GREEN_BIT);
    int blueMask = saturate(bitMask & BLUE_BIT);

    half whiteTex = tex.r * tex.g * tex.b;

    half redTex = tex.r * redMask;
    half greenTex = tex.g * greenMask;
    half blueTex = tex.b * blueMask;
    half fullMask = saturate(whiteTex + redTex + greenTex + blueTex);
    half invertTex = 1 - fullMask;
    int invertMask = saturate(bitMask & INVERT_BIT);
    half t = lerp(fullMask, invertTex, invertMask);

    float tCol = round(LinearLightness(custom.rgb));

    half useCol = saturate(ceil(custom.r + custom.g + custom.b));

    half3 darkCol = lerp(custom.rgb, blackColor, tCol);
    half3 lightCol = lerp(whiteColor, custom.rgb, tCol);
    darkCol = lerp(blackColor, darkCol, useCol);
    lightCol = lerp(whiteColor, lightCol, useCol);
    half3 finalColor = lerp(darkCol, lightCol, t);
    
    return half4(finalColor, alpha);
}
float2 SpriteUV(float2 uv, float4 uvSizeAndPos, float4 scaleAndFlip)
{
    float2 uvSize = uvSizeAndPos.xy;
    float2 uvPos = uvSizeAndPos.zw;
                
    float2 scale = scaleAndFlip.xy;
    float2 flip = scaleAndFlip.zw;

    uv *= scale;
    uv = frac(uv);
    uv = (uv - 0.5) * flip + 0.5;
    uv *= uvSize;
    uv += uvPos;
    
    return uv;
}