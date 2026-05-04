#define THREADS_PER_GROUP 64

static const float FORE_MIN = 1.0;
static const float FORE_SIZE = 1.0;

static const float MID_MIN = 48.0;
static const float MID_SIZE = 16.0;

static const float BACK_MIN = 65.0;
static const float BACK_SIZE = 63.0;

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