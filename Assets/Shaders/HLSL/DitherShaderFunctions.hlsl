float GetBayer2(uint x, uint y)
{
    uint bx = x & 1;
    uint by = y & 1;

    uint index = (by << 1) | bx;
    return float(index) * (1.0 / 4.0) - 0.5;
}

float GetBayer4(uint x, uint y)
{
    uint x0 = x & 1;
    uint x1 = (x >> 1) & 1;
    uint y0 = y & 1;
    uint y1 = (y >> 1) & 1;

    uint index =
        (y1 << 3) |
        (x1 << 2) |
        ((y0 ^ x1) << 1) |
        (x0 ^ y1);

    return float(index) * (1.0 / 16.0) - 0.5;
}
float GetBayer8(uint x, uint y)
{
    uint x0 = x & 1;
    uint x1 = (x >> 1) & 1;
    uint x2 = (x >> 2) & 1;

    uint y0 = y & 1;
    uint y1 = (y >> 1) & 1;
    uint y2 = (y >> 2) & 1;

    uint index =
        (y2 << 5) |
        (x2 << 4) |
        ((y1 ^ x2) << 3) |
        (x1 << 2) |
        ((y0 ^ x1) << 1) |
        (x0 ^ y2);

    return float(index) * (1.0 / 64.0) - 0.5;
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

void BayerMatrix_float(float value, float scale, float2 pixelCoord, out float output)
{

    float2 p = Rotate(pixelCoord, 0.261799);

    output = HalftoneDot(value, p, scale);
}
