#define TWOPI 6.28318530718
float2 QuadraticBezier(float2 start, float2 middle, float2 end, float t)
{
    float oneMinusT = 1.0 - t;
    return oneMinusT * oneMinusT * start + 2.0 * oneMinusT * t * middle + t * t * end;
}
float2 PolarCoordinate(float r, float t)
{
    return float2(r * sin(t * TWOPI), r * cos(t * TWOPI));
}
float DistanceLine(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

void BezierCurve_float(float2 topLeft, float2 bottomRight, float2 start, float2 middle, float2 end, int sampleRate, out float curve)
{
    curve = 1e6;
    float2 prevPoint = QuadraticBezier(start, middle, end, 0.0);
    float2 prevPos = lerp(topLeft, bottomRight, 0.0);
    for (int i = 1; i <= sampleRate; i++)
    {
        float t = (float) i / (float) sampleRate;
        float2 curPoint = QuadraticBezier(start, middle, end, t);
        float2 pos = lerp(topLeft, bottomRight, t);
        float2 posDelta = pos - prevPos;
        float dist = DistanceLine(pos, prevPoint + posDelta, curPoint);
        curve = min(curve, dist);
        prevPoint = curPoint;
        prevPos = pos;
    }
}


void RadioSignal_float(float2 uv, float time, int sampleRate, int freq, float arc, float radius, out float signal)
{
    signal = 1e6;
    for (int i = 0; i <= freq; i++)
    {        
        float modTime = (time - i) % freq;
        float r = radius + modTime;
        float2 prevPoint = PolarCoordinate(r, -arc);
        for (int j = 1; j <= sampleRate; j++)
        {
            float t = (float) j / (float) sampleRate;
            t = t * arc * 2 - arc;
            float2 curPoint = PolarCoordinate(r, t);
            float dist = DistanceLine(uv, prevPoint, curPoint);
            signal = min(signal, dist);
            prevPoint = curPoint;
        }
    }
}