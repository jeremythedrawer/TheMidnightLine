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

    public enum ZoneParticleType
    {
        TreesLOD0,
        TreesLOD1,
        HousesLOD0,
        HousesLOD1,
    }

    public enum ZoneSpawner
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
        public ZoneSpawner spawnerArea;
    }

    [Serializable] public struct SpawnerData
    {
        public MaterialPropertyBlock mpb;
        public GraphicsBuffer uvSizeAndPositionBuffer;
        public ComputeBuffer particleBuffer;
        public int computeGroupSize;
        public int kernelID_init;
        public int kernelID_update;
        public bool active;
    }

    [Serializable] public struct ZoneSpawnerData
    {
        public ZoneSpawner zoneSpawner;
        public SpawnerData spawnerData;
    }

    [Serializable] public struct NPCParticleData
    {
        public SpawnerData spawnerData;
        public Vector3 spawnerPosition;
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
    public static ZoneSpawnerData[] InitializeSpawnData(ComputeShader computeShader, MaterialIDSO materialIDs)
    {
        ZoneSpawnerData[] spawnerDataArray = new ZoneSpawnerData[SPAWNER_COUNT];

        for (int i = 0; i < SPAWNER_COUNT; i++)
        {
            ZoneSpawnerData zoneSpawnerData = new ZoneSpawnerData();

            zoneSpawnerData.spawnerData.mpb = new MaterialPropertyBlock();
            zoneSpawnerData.spawnerData.particleBuffer = new ComputeBuffer(PARTICLE_COUNTS[i], PARTICLE_OUTPUT_STRIDE);
            zoneSpawnerData.spawnerData.kernelID_init = computeShader.FindKernel(INIT_KERNEL_STRINGS[i]);
            zoneSpawnerData.spawnerData.kernelID_update = computeShader.FindKernel(UPDATE_KERNEL_STRINGS[i]);
            zoneSpawnerData.spawnerData.computeGroupSize = Mathf.CeilToInt(PARTICLE_COUNTS[i] / (float)THREADS_PER_GROUP);
            zoneSpawnerData.zoneSpawner = (ZoneSpawner)i;
            computeShader.SetBuffer(zoneSpawnerData.spawnerData.kernelID_init, BUFFER_STRINGS[i], zoneSpawnerData.spawnerData.particleBuffer);
            computeShader.SetBuffer(zoneSpawnerData.spawnerData.kernelID_update, BUFFER_STRINGS[i], zoneSpawnerData.spawnerData.particleBuffer);
            zoneSpawnerData.spawnerData.mpb.SetBuffer(materialIDs.ids.particles, zoneSpawnerData.spawnerData.particleBuffer);

            spawnerDataArray[i] = zoneSpawnerData;
        }

        return spawnerDataArray;
    }

    public static void ChangeSpawner(Zone zone, MaterialIDSO materialIDs, AtlasSpawnerStatsSO stats, ComputeShader computeShader)
    {
        for (int i = 0; i < stats.spawnerDataArray.Length; i++)
        {
            if (stats.spawnerDataArray[i].zoneSpawner != zone.spawnerArea) continue;

            ref ZoneSpawnerData activeSpawner = ref stats.spawnerDataArray[i];

            activeSpawner.spawnerData.active = true;
            activeSpawner.spawnerData.uvSizeAndPositionBuffer?.Dispose();
            activeSpawner.spawnerData.uvSizeAndPositionBuffer?.Release();
            activeSpawner.spawnerData.uvSizeAndPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, zone.atlas.particleSprites.Length, FLOAT4_SIZE);
            activeSpawner.spawnerData.uvSizeAndPositionBuffer.SetData(zone.atlas.particleUVSizeAndPosArray);
            activeSpawner.spawnerData.mpb.SetInt(materialIDs.ids.spriteCount,  zone.atlas.particleSprites.Length);

            activeSpawner.spawnerData.mpb.SetBuffer(materialIDs.ids.uvSizeAndPos, activeSpawner.spawnerData.uvSizeAndPositionBuffer);
            activeSpawner.spawnerData.mpb.SetTexture(materialIDs.ids.atlasTexture, zone.atlas.texture);

            Debug.Log(activeSpawner.spawnerData.kernelID_init);

            computeShader.Dispatch(activeSpawner.spawnerData.kernelID_init, activeSpawner.spawnerData.computeGroupSize, 1, 1);
            break;
        }

    }

    public static void UpdateParticles(AtlasSpawnerStatsSO stats, ComputeShader computeShader, SpyStatsSO spyStats)
    {
        computeShader.SetFloat("_Velocity", spyStats.moveVelocity.x * Time.deltaTime);
        for (int i = 0; i < stats.spawnerDataArray.Length; i++)
        {
            ref ZoneSpawnerData activeSpawner = ref stats.spawnerDataArray[i];

            if (!activeSpawner.spawnerData.active) continue;

            computeShader.Dispatch(activeSpawner.spawnerData.kernelID_update, activeSpawner.spawnerData.computeGroupSize, 1, 1);
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
