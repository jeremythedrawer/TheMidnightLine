static const int bayer2[4] =
{
    0, 2,
    3, 1
};

static const int bayer4[16] =
{
    0, 8, 2, 10,
    12, 4, 14, 6,
    3, 11, 1, 9,
    15, 7, 13, 5
};

static const int bayer8[64] =
{
    0, 32, 8, 40, 2, 34, 10, 42,
    48, 16, 56, 24, 50, 18, 58, 26,
    12, 44, 4, 36, 14, 46, 6, 38,
    60, 28, 52, 20, 62, 30, 54, 22,
    3, 35, 11, 43, 1, 33, 9, 41,
    51, 19, 59, 27, 49, 17, 57, 25,
    15, 47, 7, 39, 13, 45, 5, 37,
    63, 31, 55, 23, 61, 29, 53, 21
};

float GetBayer2(uint x, uint y)
{
    return float(bayer2[(x % 2) + (y % 2) * 2]) * (0.25) - 0.5;
}

float GetBayer4(uint x, uint y)
{
    return float(bayer4[(x % 4) + (y % 4) * 4]) * (0.0625) - 0.5;
}
float GetBayer8(uint x, uint y)
{
    return float(bayer8[(x % 8) + (y % 8) * 8]) * (0.015625) - 0.5;
}

float HalftoneDot(float value, float2 pixelCoord, float scale)
{

    float2 p = pixelCoord / scale;

    float2 cell = floor(p);
    float2 f = frac(p) - 0.5;

    float dist = length(f) * 2.0;
    float radius = saturate(value);

    return step(dist, radius);
}

float2 Rotate(float2 p, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float BayerMatrix(float value, float bayerIndex, float2 pixelCoord)
{    
    //float bayerValues = GetBayer8(pixelCoord.x, pixelCoord.y);
    const int N = 8;

    int y = (int) pixelCoord.y % N;

    // 1D Bayer sequence (evenly distributed)
    int pattern[8] = { 0, 4, 2, 6, 1, 5, 3, 7 };

    float threshold = (pattern[y] + 0.5) / N;

    return value >= threshold ? 1.0 : 0.0;
}

float1 BayerX8(float value, float2 pixelCoord)
{
    const int N = 8;
    int y = (int) pixelCoord.y % N;
    int pattern[8] = { 0, 4, 2, 6, 1, 5, 3, 7 };
    float threshold = (pattern[y] + 0.5) / N;
    return step(threshold, value);
}

float1 BayerX2(float value, float2 pixelCoord)
{
    float bayer = (pixelCoord.y % 2) - 0.5;
    return step(bayer, value);
}

float1 BayerX4(float value, float2 pixelCoord)
{
    int y = (int) pixelCoord.y;

    float bayer = (y % 4 == 0) ? 1.0 : 0.0;

    return step(bayer, value);
}
float BayerX(half y, half stepSize)
{
    int pattern = y % stepSize;
    return pattern == 0 ? 0.0 : 1.0;
}
float BayerX248(float value, float2 pixelCoord)
{
    int y = (int) pixelCoord.y;

    float full = 1.0;
    float x2 = BayerX(y, 2);
    float x4 = BayerX(y, 4);
    float x8 = BayerX(y, 8);
    float none = 0.0;

    float m1 = 1.0 - smoothstep(0, 0.20, value);
    float m2 = smoothstep(0.15, 0.20, value) * (1.0 - smoothstep(0.35, 0.40, value));
    float m3 = smoothstep(0.35, 0.40, value) * (1.0 - smoothstep(0.55, 0.60, value));
    float m4 = smoothstep(0.55, 0.60, value) * (1.0 - smoothstep(0.75, 0.80, value));
    float m5 = smoothstep(0.75, 1, value);

    return
        none * m1 +
        x2 * m2 +
        x4 * m3 +
        x8 * m4 +
        full * m5;
}