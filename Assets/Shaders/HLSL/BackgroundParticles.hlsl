struct BackgroundParticleOutput
{
    float3 position;
    float parallaxFactor;
    float rand01;
    
    uint alive;
    uint backgroundMask;
    uint lodLevel;
    uint randID;
};

struct BackgroundParticleInput
{
    uint bgMask;
    float heightRange;
    float heightPos;
};
//NOTE: Adjust the stride in the particle buffer in Spawner if the struct has changed