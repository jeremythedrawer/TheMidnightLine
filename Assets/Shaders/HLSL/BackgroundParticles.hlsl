struct ParticleOutput
{
    float3 position;
    float parallaxFactor;
    float rand01;
    uint alive;
    uint randID;
};
//NOTE: Adjust the stride in the particle buffer in Spawner if the struct has changed