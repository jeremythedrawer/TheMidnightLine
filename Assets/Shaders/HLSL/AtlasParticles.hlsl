#define THREADS_PER_GROUP 64

static const float FORE_MIN = 1.0;
static const float FORE_SIZE = 1.0;

static const float MID_MIN = 48.0;
static const float MID_SIZE = 16.0;

static const float BACK_MIN = 65.0;
static const float BACK_SIZE = 63.0;

static const float2 QUAD_OFFSETS[4] =
{
    float2(0, 0),
    float2(0, 1),
    float2(1, 1),
    float2(1, 0)
};

static const float2 QUAD_TRIANGLE_OFFSETS[6] =
{
    float2(0, 0),
    float2(0, 1),
    float2(1, 1),

    float2(0, 0),
    float2(1, 1),
    float2(1, 0)
};

struct ParticleSprite
{
    float4 uvSizeAndPos;
    float4 worldPivotAndSize;
};