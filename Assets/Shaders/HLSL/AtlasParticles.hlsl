#define THREADS_PER_GROUP 64

struct ZoneOutput
{
    float4 uvSizeAndPos;
    float4 position;
    float4 scale;
    float4 worldPivotAndSize;
    float parallaxFactor;
    float rand01;
    uint alive;
    uint randID;
};

struct ZoneInput
{
    float4 uvSizeAndPos;
    float4 worldPivotAndSize;
    float4 sliceOffsetAndSize;
};
