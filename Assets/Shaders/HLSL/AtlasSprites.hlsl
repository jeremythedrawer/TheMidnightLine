#define METERS_TRAVELLED_DIVISOR 1000

#define COLOR_KEY_BIT_0 1 << 0
#define COLOR_KEY_BIT_1 1 << 1
#define COLOR_KEY_BIT_2 1 << 2
#define DIAGONAL_TEXTURE_BIT 1 << 3
#define MERIDIA_COLOR_BIT 1 << 4


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