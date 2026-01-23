int HashIntToInt(int seed)
{
    seed = (seed ^ 61u) ^ (seed >> 16u);
    seed *= 9u;
    seed = seed ^ (seed >> 4u);
    seed *= 0x27d4eb2du;
    seed = seed ^ (seed >> 15u);
    return seed;
}

float HashIntTo01Float(int seed)
{
    seed = (seed ^ 12345391) * 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return (seed & 0x7FFFFFFF) / (float) 0x7FFFFFFF;
}