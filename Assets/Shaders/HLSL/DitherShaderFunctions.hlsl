static const int bayer2[2 * 2] =
{
    0, 2,
    3, 1
};

static const int bayer4[4 * 4] =
{
    0, 8, 2, 10,
    12, 4, 14, 6,
    3, 11, 1, 9,
    15, 7, 13, 5
};

static const int bayer8[8 * 8] =
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

float GetBayer2(int x, int y)
{
    return float(bayer2[(x % 2) + (y % 2) * 2]) * (1.0 / 4.0) - 0.5;
}

float GetBayer4(int x, int y)
{
    return float(bayer4[(x % 4) + (y % 4) * 4]) * (1.0 / 16.0) - 0.5;
}

float GetBayer8(int x, int y)
{
    return float(bayer8[(x % 8) + (y % 8) * 8]) * (1.0 / 64.0) - 0.5;
}

void BayerMatrix_float(float value, int bayerIndex, float2 pixelCoord, out float output)
{
    
    float bayerValues[3] = { 0, 0, 0 };
    bayerValues[0] = GetBayer2(pixelCoord.x, pixelCoord.y);
    bayerValues[1] = GetBayer4(pixelCoord.x, pixelCoord.y);
    bayerValues[2] = GetBayer8(pixelCoord.x, pixelCoord.y);
    
    output = round(value + bayerValues[bayerIndex]);
}
