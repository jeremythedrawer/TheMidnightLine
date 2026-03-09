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
    public static int FLOAT2_SIZE = FLOAT_SIZE * 2;
    public static int FLOAT3_SIZE = FLOAT_SIZE * 3;
    public static int FLOAT4_SIZE = FLOAT_SIZE * 4;
    public static int INT_SIZE = sizeof(uint);

    public const int ZONE_SPAWNER_COUNT = 8;

    public const int THREADS_PER_GROUP = 64;
    public const int FORE_PARTICLE_COUNT = 64;
    public const int MID_PARTICLE_COUNT = 256;
    public const int BACK_PARTICLE_COUNT = 128;

    public static readonly int MAX_VERTEX_COUNT = (FORE_PARTICLE_COUNT + MID_PARTICLE_COUNT + BACK_PARTICLE_COUNT) * 4;
    public static readonly int ZONE_STRIDE = FLOAT3_SIZE + (FLOAT_SIZE * 2) + (INT_SIZE * 2);
    public static readonly int GLYPH_STRIDE = (FLOAT3_SIZE * 2) + INT_SIZE;

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
        public GraphicsBuffer worldSizeBuffer;
        public ComputeBuffer particleBuffer;
        public int computeGroupSize;
        public int kernelID_init;
        public int kernelID_update;
        public bool active;
    }
    [Serializable] public struct ZoneSpawnerData
    {
        public ZoneSpawner zoneSpawner;
        public SpawnerData zoneSpawnerData;
    }
    [Serializable] public struct GlyphSpawnerData
    {
        public SpawnerData spawnerData;
        public Vector3 spawnerPosition;
    }
    public static void InitializeZoneCompute(ComputeShader computeShader, MaterialIDSO materialIDs, ZoneSpawnerStatsSO stats)
    {
        computeShader.SetVector(materialIDs.ids.spawnerMinPos, stats.spawnMinPos);
        computeShader.SetVector(materialIDs.ids.spawnerMaxPos, stats.spawnMaxPos);
        computeShader.SetVector(materialIDs.ids.spawnerSize, stats.spawnBoundsSize);
        computeShader.SetInt("_ForegroundParticleCount", FORE_PARTICLE_COUNT);
        computeShader.SetInt("_MiddlegroundParticleCount", MID_PARTICLE_COUNT);
        computeShader.SetInt("_BackgroundParticleCount", BACK_PARTICLE_COUNT);
        computeShader.SetFloat(materialIDs.ids.trainVelocity, 0);
    }
    public static ZoneSpawnerData[] InitializeZoneSpawnData(ComputeShader computeShader, MaterialIDSO materialIDs)
    {
        ZoneSpawnerData[] spawnerDataArray = new ZoneSpawnerData[ZONE_SPAWNER_COUNT];

        for (int i = 0; i < ZONE_SPAWNER_COUNT; i++)
        {
            ZoneSpawnerData zoneSpawnerData = new ZoneSpawnerData();

            zoneSpawnerData.zoneSpawnerData.mpb = new MaterialPropertyBlock();
            zoneSpawnerData.zoneSpawnerData.particleBuffer = new ComputeBuffer(PARTICLE_COUNTS[i], ZONE_STRIDE);
            zoneSpawnerData.zoneSpawnerData.kernelID_init = computeShader.FindKernel(INIT_KERNEL_STRINGS[i]);
            zoneSpawnerData.zoneSpawnerData.kernelID_update = computeShader.FindKernel(UPDATE_KERNEL_STRINGS[i]);
            zoneSpawnerData.zoneSpawnerData.computeGroupSize = Mathf.CeilToInt(PARTICLE_COUNTS[i] / (float)THREADS_PER_GROUP);
            zoneSpawnerData.zoneSpawner = (ZoneSpawner)i;
            computeShader.SetBuffer(zoneSpawnerData.zoneSpawnerData.kernelID_init, BUFFER_STRINGS[i], zoneSpawnerData.zoneSpawnerData.particleBuffer);
            computeShader.SetBuffer(zoneSpawnerData.zoneSpawnerData.kernelID_update, BUFFER_STRINGS[i], zoneSpawnerData.zoneSpawnerData.particleBuffer);
            zoneSpawnerData.zoneSpawnerData.mpb.SetBuffer(materialIDs.ids.particles, zoneSpawnerData.zoneSpawnerData.particleBuffer);

            spawnerDataArray[i] = zoneSpawnerData;
        }

        return spawnerDataArray;
    }
    public static void ChangeZoneSpawner(Zone zone, MaterialIDSO materialIDs, ZoneSpawnerStatsSO stats, ComputeShader computeShader)
    {
        for (int i = 0; i < stats.zoneSpawnerDataArray.Length; i++)
        {
            if (stats.zoneSpawnerDataArray[i].zoneSpawner != zone.spawnerArea) continue;

            ref ZoneSpawnerData activeSpawner = ref stats.zoneSpawnerDataArray[i];

            activeSpawner.zoneSpawnerData.active = true;
            activeSpawner.zoneSpawnerData.uvSizeAndPositionBuffer?.Dispose();
            activeSpawner.zoneSpawnerData.uvSizeAndPositionBuffer?.Release();
            activeSpawner.zoneSpawnerData.uvSizeAndPositionBuffer = null;
            activeSpawner.zoneSpawnerData.uvSizeAndPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, zone.atlas.particleSprites.Length, FLOAT4_SIZE);
            activeSpawner.zoneSpawnerData.uvSizeAndPositionBuffer.SetData(zone.atlas.particleUVSizeAndPosArray);

            activeSpawner.zoneSpawnerData.worldSizeBuffer?.Dispose();
            activeSpawner.zoneSpawnerData.worldSizeBuffer?.Release();
            activeSpawner.zoneSpawnerData.worldSizeBuffer = null;
            activeSpawner.zoneSpawnerData.worldSizeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, zone.atlas.particleSprites.Length, FLOAT2_SIZE);
            activeSpawner.zoneSpawnerData.worldSizeBuffer.SetData(zone.atlas.worldSizesArray);

            activeSpawner.zoneSpawnerData.mpb.SetInt(materialIDs.ids.spriteCount,  zone.atlas.particleSprites.Length);

            activeSpawner.zoneSpawnerData.mpb.SetBuffer(materialIDs.ids.uvSizeAndPos, activeSpawner.zoneSpawnerData.uvSizeAndPositionBuffer);
            activeSpawner.zoneSpawnerData.mpb.SetBuffer("_WorldSize", activeSpawner.zoneSpawnerData.worldSizeBuffer);
            activeSpawner.zoneSpawnerData.mpb.SetTexture(materialIDs.ids.atlasTexture, zone.atlas.texture);

            computeShader.Dispatch(activeSpawner.zoneSpawnerData.kernelID_init, activeSpawner.zoneSpawnerData.computeGroupSize, 1, 1);
            break;
        }

    }
    public static void UpdateZoneParticles(ZoneSpawnerStatsSO stats, ComputeShader computeShader, CameraStatsSO camStats, SpyStatsSO spyStats, TrainStatsSO trainStats)
    {
        computeShader.SetFloat("_CamVelocity", (camStats.curVelocity.x * Time.deltaTime));

        if (spyStats.onTrain)
        {
            computeShader.SetFloat("_TrainVelocity", (trainStats.curVelocity * Time.deltaTime));
        }

        for (int i = 0; i < stats.zoneSpawnerDataArray.Length; i++)
        {
            ref ZoneSpawnerData activeSpawner = ref stats.zoneSpawnerDataArray[i];

            if (!activeSpawner.zoneSpawnerData.active) continue;

            computeShader.Dispatch(activeSpawner.zoneSpawnerData.kernelID_update, activeSpawner.zoneSpawnerData.computeGroupSize, 1, 1);
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
