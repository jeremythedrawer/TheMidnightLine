float GetBayer2(uint x, uint y)
{
    uint bx = x & 1;
    uint by = y & 1;

    uint index = (by << 1) | bx; // matches your bayer2 layout
    return float(index) * (1.0 / 4.0) - 0.5;
}

float GetBayer4(uint x, uint y)
{
    uint bx0 = (x >> 0) & 1;
    uint bx1 = (x >> 1) & 1;
    uint by0 = (y >> 0) & 1;
    uint by1 = (y >> 1) & 1;

    uint index =
        (by1 << 3) |
        (bx1 << 2) |
        (by0 << 1) |
        (bx0 << 0);

    return float(index) * (1.0 / 16.0) - 0.5;
}
float GetBayer8(uint x, uint y)
{
    uint bx0 = (x >> 0) & 1;
    uint bx1 = (x >> 1) & 1;
    uint bx2 = (x >> 2) & 1;

    uint by0 = (y >> 0) & 1;
    uint by1 = (y >> 1) & 1;
    uint by2 = (y >> 2) & 1;

    uint index =
        (by2 << 5) |
        (bx2 << 4) |
        (by1 << 3) |
        (bx1 << 2) |
        (by0 << 1) |
        (bx0 << 0);

    return float(index) * (1.0 / 64.0) - 0.5;
}

void BayerMatrix_float(float value, int bayerIndex, float2 pixelCoord, out float output)
{
    int xi = int(pixelCoord.x);
    int yi = int(pixelCoord.y);

    if (bayerIndex == 0)
        output = floor(value + 0.5 + GetBayer2(xi, yi));
    else if (bayerIndex == 1)
        output = floor(value + 0.5 + GetBayer4(xi, yi));
    else
        output = floor(value + 0.5 + GetBayer8(xi, yi));
}
