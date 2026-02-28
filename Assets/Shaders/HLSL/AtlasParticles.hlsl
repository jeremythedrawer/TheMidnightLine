#define THREADS_PER_GROUP 64

struct ZoneOutput
{
    float3 position;
    float parallaxFactor;
    float rand01;
    uint alive;
    uint randID;
};

struct NPCOutput
{
    float3 spawnPosition;
    float3 position;
    uint alive;
};
//NOTE: Adjust the stride in the particle buffer in Spawner if the struct has changed