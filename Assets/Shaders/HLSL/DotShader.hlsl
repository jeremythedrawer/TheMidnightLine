void Dots_float(float2 uv, float uvScale, float radius, float size, out float output)
{
    float2 f = frac(uv * uvScale);

    float minDist = 1;
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 cell = float2(x, y);
            float2 cellCenter = cell + 0.5;
            
            float d = distance(cellCenter, f);
            minDist = min(minDist, d);
        }
    }
    
    output = step(minDist, radius * size);
}
