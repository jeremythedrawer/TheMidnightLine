using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static Atlas;
using static AtlasSpawn;
public static class AtlasSpawn
{
    public static int FLOAT_SIZE = sizeof(float);
    public static int FLOAT3_SIZE = FLOAT_SIZE * 3;
    public static int FLOAT4_SIZE = FLOAT_SIZE * 4;
    public static int INT_SIZE = sizeof(uint);

    public const int SPAWNER_COUNT = 8;

    public const int THREADS_PER_GROUP = 64;
    public const int FORE_PARTICLE_COUNT = 64;
    public const int MID_PARTICLE_COUNT = 256;
    public const int BACK_PARTICLE_COUNT = 128;

    public const int MAX_LOD_COUNT = 4;

    public static readonly int MAX_VERTEX_COUNT = (FORE_PARTICLE_COUNT + MID_PARTICLE_COUNT + BACK_PARTICLE_COUNT) * 4;
    public static readonly int PARTICLE_OUTPUT_STRIDE = FLOAT3_SIZE + (FLOAT_SIZE * 2) + (INT_SIZE * 2);
    public static readonly int PARTICLE_INPUT_STRIDE = INT_SIZE + (FLOAT_SIZE * 2);

    //TODO Need to set buffer strings to ids if this buffer idea works
    public static string[] BUFFER_STRINGS = { "_Foreground0", "_Middleground0", "_Middleground1", "_Middleground2", "_Middleground3", "_Background0", "_Background1", "_Background2" };
    public static readonly string[] INIT_KERNEL_STRINGS = { "CSForeground0_Init", "CSMiddleground0_Init", "CSMiddleground1_Init", "CSMiddleground2_Init", "CSMiddleground3_Init", "CSBackground0_Init", "CSBackground1_Init", "CSBackground2_Init" };
    public static readonly string[] UPDATE_KERNEL_STRINGS = { "CSForeground0_Update", "CSMiddleground0_Update", "CSMiddleground1_Update", "CSMiddleground2_Update", "CSMiddleground3_Update", "CSBackground0_Update", "CSBackground1_Update", "CSBackground2_Update" };

    public static readonly int[] PARTICLE_COUNTS = { FORE_PARTICLE_COUNT, MID_PARTICLE_COUNT, MID_PARTICLE_COUNT, MID_PARTICLE_COUNT, MID_PARTICLE_COUNT, BACK_PARTICLE_COUNT, BACK_PARTICLE_COUNT, BACK_PARTICLE_COUNT };

    public enum ParticleType
    {
        TreesLOD0,
        TreesLOD1,
        HousesLOD0,
        HousesLOD1,
    }

    public enum Spawner
    {
        Foreground0,
        Middleground0,
        Middleground1,
        Middleground2,
        Middleground3,
        Background0,
        Background1,
        Background2,

    }

    [Serializable] public struct Zone
    {
        public AtlasSO atlas;
        public int metersStart;
        public int metersLength;
        public Spawner spawner;
    }

    [Serializable] public struct SpawnerData
    {
        public Spawner spawner;
        public MaterialPropertyBlock mpb;
        public GraphicsBuffer uvSizeAndPositionBuffer;
        public ComputeBuffer particleBuffer;
        public int computeGroupSize;
        public int kernelID_init;
        public int kernelID_update;
        public bool active;
    }

    public static void InitializeAtlasCompute(ComputeShader computeShader, MaterialIDSO materialIDs, AtlasSpawnerStatsSO stats)
    {
        computeShader.SetVector(materialIDs.ids.spawnerMinPos, stats.spawnMinPos);
        computeShader.SetVector(materialIDs.ids.spawnerMaxPos, stats.spawnMaxPos);
        computeShader.SetVector(materialIDs.ids.spawnerSize, stats.spawnBoundsSize);
        computeShader.SetInt("_ForegroundParticleCount", FORE_PARTICLE_COUNT);
        computeShader.SetInt("_MiddlegroundParticleCount", MID_PARTICLE_COUNT);
        computeShader.SetInt("_BackgroundParticleCount", BACK_PARTICLE_COUNT);
        computeShader.SetFloat(materialIDs.ids.trainVelocity, 0);
    }
    public static SpawnerData[] InitializeSpawnData(ComputeShader computeShader, MaterialIDSO materialIDs)
    {
        SpawnerData[] spawnerDataArray = new SpawnerData[SPAWNER_COUNT];

        for (int i = 0; i < SPAWNER_COUNT; i++)
        {
            SpawnerData spawnerData = new SpawnerData();

            spawnerData.mpb = new MaterialPropertyBlock();
            spawnerData.particleBuffer = new ComputeBuffer(PARTICLE_COUNTS[i], PARTICLE_OUTPUT_STRIDE);
            spawnerData.kernelID_init = computeShader.FindKernel(INIT_KERNEL_STRINGS[i]);
            spawnerData.kernelID_update = computeShader.FindKernel(UPDATE_KERNEL_STRINGS[i]);
            spawnerData.computeGroupSize = Mathf.CeilToInt(PARTICLE_COUNTS[i] / (float)THREADS_PER_GROUP);
            spawnerData.spawner = (Spawner)i;
            computeShader.SetBuffer(spawnerData.kernelID_init, BUFFER_STRINGS[i], spawnerData.particleBuffer);
            computeShader.SetBuffer(spawnerData.kernelID_update, BUFFER_STRINGS[i], spawnerData.particleBuffer);
            spawnerData.mpb.SetBuffer(materialIDs.ids.particles, spawnerData.particleBuffer);

            spawnerDataArray[i] = spawnerData;
        }

        return spawnerDataArray;
    }

    public static void ChangeSpawner(Zone zone, MaterialIDSO materialIDs, AtlasSpawnerStatsSO stats, ComputeShader computeShader)
    {
        for (int i = 0; i < stats.spawnerDataArray.Length; i++)
        {
            if (stats.spawnerDataArray[i].spawner != zone.spawner) continue;

            ref SpawnerData activeSpawner = ref stats.spawnerDataArray[i];

            activeSpawner.active = true;

            activeSpawner.uvSizeAndPositionBuffer?.Dispose();
            activeSpawner.uvSizeAndPositionBuffer?.Release();

            activeSpawner.uvSizeAndPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, zone.atlas.particleSprites.Length, FLOAT4_SIZE);
            activeSpawner.uvSizeAndPositionBuffer.SetData(zone.atlas.particleUVSizeAndPosArray);
            activeSpawner.mpb.SetInt(materialIDs.ids.spriteCount,  zone.atlas.particleSprites.Length);

            activeSpawner.mpb.SetBuffer(materialIDs.ids.uvSizeAndPos, activeSpawner.uvSizeAndPositionBuffer);
            activeSpawner.mpb.SetTexture(materialIDs.ids.atlasTexture, zone.atlas.texture);

            computeShader.Dispatch(activeSpawner.kernelID_init, activeSpawner.computeGroupSize, 1, 1);
            break;
        }

    }

    public static void UpdateParticles(AtlasSpawnerStatsSO stats, ComputeShader computeShader, SpyStatsSO spyStats)
    {
        computeShader.SetFloat("_Velocity", spyStats.moveVelocity.x * Time.deltaTime);
        for (int i = 0; i < stats.spawnerDataArray.Length; i++)
        {
            ref SpawnerData activeSpawner = ref stats.spawnerDataArray[i];

            if (!activeSpawner.active) continue;

            computeShader.Dispatch(activeSpawner.kernelID_update, activeSpawner.computeGroupSize, 1, 1);
        }
    }

    public static Queue<Zone> SetZoneQueue(TripSO trip)
    {
        Queue<Zone> queue = new Queue<Zone>();

        for (int i = 0; i < trip.zones.Length; i++)
        {
            Zone zone = trip.zones[i];
            queue.Enqueue(zone);
        }
        return queue;
    }
}
