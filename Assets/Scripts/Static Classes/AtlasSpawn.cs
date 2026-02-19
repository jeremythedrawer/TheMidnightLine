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

    public static readonly int MAX_VERTEX_COUNT = (FORE_PARTICLE_COUNT + MID_PARTICLE_COUNT + BACK_PARTICLE_COUNT) * 6;
    public static readonly int PARTICLE_OUTPUT_STRIDE = FLOAT3_SIZE + (FLOAT_SIZE * 2) + (INT_SIZE * 2);
    public static readonly int PARTICLE_INPUT_STRIDE = INT_SIZE + (FLOAT_SIZE * 2);

    //TODO Need to set buffer strings to ids if this buffer idea works
    public static string[] BUFFER_STRINGS = { "_Foreground0 ", "_Middleground0", "_Middleground1", "_Middleground2", "_Middleground3", "_Background0", "_Background1", "_Background2" };
    public static readonly string[] KERNEL_STRINGS = { "CSForeground0_Init", "CSMiddleground0_Init", "CSMiddleground1_Init", "CSMiddleground2_Init", "CSMiddleground3_Init", "CSBackground0_Init", "CSBackground1_Init", "CSBackground2_Init" };

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
        public AtlasSO atlasParticles;
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
        public int computeGroup;
        public int kernelID;
        public bool active;
    }

    public static SpawnerData[] InitializeSpawnDataArray(ComputeShader computeShader, MaterialIDSO materialIDs, AtlasSpawnerStatsSO stats)
    {
        SpawnerData[] spawnerDataArray = new SpawnerData[SPAWNER_COUNT];

        computeShader.SetVector(materialIDs.ids.spawnerMinPos, stats.spawnMinPos);
        computeShader.SetVector(materialIDs.ids.spawnerMaxPos, stats.spawnMaxPos);
        computeShader.SetVector(materialIDs.ids.spawnerSize, stats.spawnBoundsSize);
        computeShader.SetInt("_ForegroundParticleCount", FORE_PARTICLE_COUNT);
        computeShader.SetInt("_MiddlegroundParticleCount", MID_PARTICLE_COUNT);
        computeShader.SetInt("_BackgroundParticleCount", BACK_PARTICLE_COUNT);
        computeShader.SetFloat(materialIDs.ids.trainVelocity, 0);

        for (int i = 0; i < SPAWNER_COUNT; i++)
        {
            SpawnerData spawnerData = new SpawnerData();

            spawnerData.mpb = new MaterialPropertyBlock();
            spawnerData.particleBuffer = new ComputeBuffer(PARTICLE_COUNTS[i], PARTICLE_OUTPUT_STRIDE);
            spawnerData.kernelID = computeShader.FindKernel(KERNEL_STRINGS[i]);
            spawnerData.computeGroup = Mathf.CeilToInt(PARTICLE_COUNTS[i] / THREADS_PER_GROUP);

            computeShader.SetBuffer(spawnerData.kernelID, BUFFER_STRINGS[i], spawnerData.particleBuffer);
            spawnerData.mpb.SetBuffer(materialIDs.ids.particles, spawnerData.particleBuffer);

            spawnerDataArray[i] = spawnerData;
        }

        return spawnerDataArray;
    }
    public static void ChangeSpawner(AtlasSO activeParticleAtlas, SpawnerData activeSpawner, MaterialIDSO materialIDs)
    {
        activeSpawner.uvSizeAndPositionBuffer?.Dispose();
        activeSpawner.uvSizeAndPositionBuffer?.Release();

        activeSpawner.uvSizeAndPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, activeParticleAtlas.particleSprites.Length, FLOAT4_SIZE);
        activeSpawner.uvSizeAndPositionBuffer.SetData(activeParticleAtlas.particleUVSizeAndPosArray);

        activeSpawner.mpb.SetBuffer(materialIDs.ids.uvSizeAndPos, activeSpawner.uvSizeAndPositionBuffer);
        activeSpawner.mpb.SetTexture(materialIDs.ids.atlasTexture, activeParticleAtlas.texture);
    }
    //public static void ChangeBiome(AtlasSpawnerStatsSO stats, AtlasSpawnerSettingsSO settings, MaterialIDSO materialIDs)
    //{
    //    int curBGMask = (int)stats.curBackgroundTypes;
    //    stats.backgroundMaskCount = 0;

    //    for (int i = 0; i < SPAWNER_COUNT; i++)
    //    {
    //        if ((curBGMask & (1 << i)) == 0) continue; // Find an active background type flag

    //        int activeBGMask = 1 << i;

    //        BackgroundParticleInputs bgParticleInputs = stats.backgroundInputsArray[stats.backgroundMaskCount];

    //        bgParticleInputs.bgMask = activeBGMask;

    //        ParticleData particleData = stats.particleDataDict[(BackgroundType)activeBGMask];

    //        int particleBGMask = (int)particleData.backgroundType;

    //        int lodIndex = 0;
    //        for (int j = 0; j < stats.spawnerDataArray.Length; j++)
    //        {
    //            SpawnerData spawnerData = stats.spawnerDataArray[j];

    //            if (spawnerData.active) continue; // Find an unused spawner

    //            SpawnerData curSpawnData = stats.spawnerDataArray[j];

    //            curSpawnData.uvSizeAndPositionBuffer?.Dispose();
    //            curSpawnData.uvSizeAndPositionBuffer?.Release();

    //            lodIndex = j % particleData.uvSizeAndPositionsLODS.Length;

    //            curSpawnData.uvSizeAndPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleData.uvSizeAndPositionsLODS[lodIndex].uvSizeAndPositionsArray.Length, FLOAT4_SIZE);
    //            curSpawnData.uvSizeAndPositionBuffer.SetData(particleData.uvSizeAndPositionsLODS[lodIndex].uvSizeAndPositionsArray);

    //            curSpawnData.mpb.SetInt(materialIDs.ids.spriteCount, particleData.uvSizeAndPositionsLODS[lodIndex].uvSizeAndPositionsArray.Length);
    //            curSpawnData.mpb.SetTexture(materialIDs.ids.atlasTexture, particleData.particleAtlas.texture);
    //            curSpawnData.mpb.SetBuffer(materialIDs.ids.uvSizeAndPos, curSpawnData.uvSizeAndPositionBuffer);
    //            curSpawnData.mpb.SetInt(materialIDs.ids.backgroundMask, particleBGMask);
    //            curSpawnData.active = true;

    //            stats.spawnerDataArray[j] = curSpawnData;

    //            bgParticleInputs.heightPos = particleData.spawnHeightPosition;
    //            bgParticleInputs.heightRange = particleData.spawnHeightRange;
    //            stats.backgroundInputsArray[stats.backgroundMaskCount] = bgParticleInputs;

    //            stats.backgroundMaskCount++;

    //            lodIndex++;
    //            settings.atlasCompute.SetBuffer(curSpawnData.kernelID, materialIDs.ids.particleInputs, )
    //            settings.atlasCompute.Dispatch(curSpawnData.kernelID, curSpawnData.computeGroup, 1, 1);
    //                //TODO: Dispatch appropiate compute shaders
    //            if (lodIndex == particleData.uvSizeAndPositionsLODS.Length) break;
    //        }
    //    }

    //    settings.atlasCompute.SetInt(materialIDs.ids.backgroundMaskCount, stats.backgroundMaskCount);
    //    stats.inputComputeBuffer.SetData(stats.backgroundInputsArray);
    //    settings.atlasCompute.SetBuffer(stats.updateKernelID, materialIDs.ids.particleInputs, stats.inputComputeBuffer);
    //    settings.atlasCompute.SetBuffer(stats.initKernelID, materialIDs.ids.particleInputs, stats.inputComputeBuffer);

    //}
    //public static Dictionary<BackgroundType, AtlasParticleSO> SetParticleDataDictionary(ParticleData[] particleDataArray)
    //{
    //    Dictionary<BackgroundType, ParticleData> dict = new Dictionary<BackgroundType, ParticleData>();

    //    BackgroundType[] backgroundTypes = (BackgroundType[])Enum.GetValues(typeof(BackgroundType));

    //    for (int i = 0; i < backgroundTypes.Length; i++)
    //    {
    //        BackgroundType backgroundType = backgroundTypes[i];

    //        for (int j = 0; j < particleDataArray.Length; j++)
    //        {
    //            if (backgroundType == particleDataArray[j].backgroundType)
    //            {
    //                dict.Add(backgroundType, particleDataArray[j]);
    //            }
    //        }
    //    }

    //    return dict;
    //}
}
