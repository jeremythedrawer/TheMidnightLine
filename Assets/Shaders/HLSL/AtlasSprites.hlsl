#define METERS_TRAVELLED_DIVISOR 1000;

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