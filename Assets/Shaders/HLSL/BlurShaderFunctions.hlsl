#define TWOPI 6.28318530718f
#define E 2.71828f

float Gaussian(int x, float spread)
{
    float sigmaSqu = spread * spread;
    return (1 / sqrt(TWOPI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
}

void GaussianBlur_float(UnityTexture2D tex, UnitySamplerState sample, float2 uv, float mipLevel, float2 texelSize, float spread, int samples, out float4 output)
{
    float2 size = texelSize * spread;
    float gridSum = 0;
    float4 col = float4(0, 0, 0, 0);
    
    for (int x = -samples; x <= samples; x++)
    {
        float weight = Gaussian(x, spread);
        gridSum += weight;

        float2 offsetUV = uv + float2(size.x * x, 0);
        col += weight * SAMPLE_TEXTURE2D_LOD(tex, sample, offsetUV, mipLevel);
    }
    output = col / gridSum;
    gridSum = 0;
    for (int y = -samples; y <= samples; y++)
    {
        float weight = Gaussian(y, spread);
        gridSum += weight;

        float2 offsetUV = uv + float2(0, size.y * y);
        col += weight * SAMPLE_TEXTURE2D_LOD(tex, sample, offsetUV, mipLevel);
    }
    output = col / gridSum;
}

void BoxBlur_float(UnityTexture2D tex, UnitySamplerState sample, float2 uv, float blurSize, out float4 output)
{
    float sum = 0;

    static const float2 offsets[4] =
    {
        float2(-1, -1),
        float2(-1, 1),
        float2(1, -1),
        float2(1, 1)
    };

    for (int i = 0; i < 4; i++)
    {
        float2 offsetUV = uv + offsets[i] * blurSize;
        sum += SAMPLE_TEXTURE2D(tex, sample, offsetUV);
    }

    output = sum * 0.25;
}
