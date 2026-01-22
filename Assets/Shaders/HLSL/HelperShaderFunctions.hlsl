float3 TangentToWorldNormal(float3 tangentNormal, float3 tangent, float3 bitangent, float3 normal)
{
    float3x3 TBN =
    {
        tangent.x, bitangent.x, normal.x,
        tangent.y, bitangent.y, normal.y,
        tangent.z, bitangent.z, normal.z
    };
    
    return mul(TBN, tangentNormal);
}
float DistLine(float2 a, float2 b)
{
    float2 distA = a - b;
    float distB = saturate(dot(a, distA) / dot(distA, distA));
    
    return length(a - distA * distB);
}

float4 TriplanarMap(float4 xTex, float4 yTex, float4 zTex, float3 normal, float sharpness)
{
    float3 n = abs(normal);
    n = pow(n, sharpness);
    n = n / (n.x + n.y + n.z);
    return xTex * n.x + yTex * n.y + zTex * n.z;
}