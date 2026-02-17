using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static Atlas;
using static AtlasSpawn;
public static class AtlasSpawn
{
    static int floatSize = sizeof(float);
    static int float3Size = floatSize * 3;
    static int float4Size = floatSize * 4;
    static int intSize = sizeof(uint);

    public const int SPAWNER_COUNT = 32;
    public const int MAX_PARTICLE_COUNT = 1024;
    public const int MAX_LOD_COUNT = 4; // NOTE: If changing this number, this will need to be changed on the compute shader as well

    public static int MAX_VERTEX_COUNT = MAX_PARTICLE_COUNT * 6;
    public static int PARTICLE_OUTPUT_STRIDE = float3Size + (floatSize * 2) + (intSize * 3);
    public static int PARTICLE_INPUT_STRIDE = intSize + (floatSize * 2);

    public static int updateKernelID;
    public static int initKernelID;
    [Flags] public enum BackgroundType
    {
        None = 0,
        Trees = 1 << 0,
        Houses = 1 << 1,
        Buildings = 1 << 2,
        Powerlines = 1 << 3,
    }

    [Serializable] public struct ParticleData
    {
        public BackgroundType backgroundType;
        public AtlasSO particleAtlas;
        public float spawnHeightPosition;
        public float spawnHeightRange;

        public LODArray[] uvSizeAndPositionsLODS;

        public bool active;
    }

    [Serializable] public struct LODArray
    {
        public Vector4[] uvSizeAndPositionsArray;
    }
    [Serializable] public struct Biomes
    {
        public float metersPosition;
        public BackgroundType backgroundType;
    }

    [Serializable] public struct SpawnerData
    {
        public MaterialPropertyBlock mpb;
        public GraphicsBuffer uvSizeAndPositionBuffer;
        public int lod;
        public bool active;
    }

    [Serializable] public struct BackgroundParticleInputs
    {
        public int bgMask;
        public float heightRange;
        public float heightPos;
    }

    public static SpawnerData[] InitializeSpawnDataArray(ComputeBuffer outputComputeBuffer, MaterialIDSO materialIDs)
    {
        SpawnerData[] spawnerDataArray = new SpawnerData[SPAWNER_COUNT];


        for (int i = 0; i < SPAWNER_COUNT; i++)
        {
            SpawnerData spawnerData = new SpawnerData();
            spawnerData.mpb = new MaterialPropertyBlock();
            spawnerData.active = false;
            spawnerData.lod = i % 3;



            spawnerData.mpb.SetBuffer(materialIDs.ids.bgParticleOutputs, outputComputeBuffer);

            spawnerDataArray[i] = spawnerData;
        }

        return spawnerDataArray;
    }

    public static void InitializeParticleDataArray(AtlasSpawnerSettingsSO settings)
    {

        for (int i = 0; i < settings.particleData.Length; i++)
        {

            ParticleData particleData = settings.particleData[i];

            List<LODArray> lodList = new List<LODArray>();

            HashSet<int> lodHash = new HashSet<int>();
            for (int j = 0; j < particleData.particleAtlas.particleSprites.Length; j++)
            {
                ParticleSprite curParticleSprite = particleData.particleAtlas.particleSprites[j];
                if (!lodHash.Contains(curParticleSprite.LOD))
                {
                    lodHash.Add(curParticleSprite.LOD);
                    lodList.Add(new LODArray());
                }
            }

            particleData.uvSizeAndPositionsLODS = lodList.ToArray();

            for (int j = 0; j < particleData.uvSizeAndPositionsLODS.Length; j++)
            {
                List<Vector4> uvSizeAndPosList = new List<Vector4>();

                for (int k = 0; k < particleData.particleAtlas.particleSprites.Length; k++)
                {
                    ParticleSprite curParticleSprite = particleData.particleAtlas.particleSprites[k];

                    if (curParticleSprite.LOD == j)
                    {
                        uvSizeAndPosList.Add(curParticleSprite.sprite.uvSizeAndPos);
                    }
                }
                particleData.uvSizeAndPositionsLODS[j].uvSizeAndPositionsArray = uvSizeAndPosList.ToArray();
            }

            settings.particleData[i] = particleData;
        }
    }
    public static void ChangeBiome(AtlasSpawnerStatsSO stats, AtlasSpawnerSettingsSO settings, MaterialIDSO materialIDs)
    {
        int curBGMask = (int)stats.curBackgroundTypes;
        stats.backgroundMaskCount = 0;

        for (int i = 0; i < SPAWNER_COUNT; i++)
        {
            if ((curBGMask & (1 << i)) == 0) continue; // Find an active background type flag

            int activeBGMask = 1 << i;

            BackgroundParticleInputs bgParticleInputs = stats.backgroundInputsArray[stats.backgroundMaskCount];

            bgParticleInputs.bgMask = activeBGMask;

            ParticleData particleData = stats.particleDataDict[(BackgroundType)activeBGMask];

            int particleBGMask = (int)particleData.backgroundType;

            int activeSpawnersForParticle = 0;

            for (int j = 0; j < stats.spawnerDataArray.Length; j++)
            {
                SpawnerData spawnerData = stats.spawnerDataArray[j];

                if (spawnerData.active) continue; // Find an unused spawner

                SpawnerData curSpawnData = stats.spawnerDataArray[j];

                curSpawnData.uvSizeAndPositionBuffer?.Dispose();
                curSpawnData.uvSizeAndPositionBuffer?.Release();

                int lodIndex = j % particleData.uvSizeAndPositionsLODS.Length;

                curSpawnData.uvSizeAndPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleData.uvSizeAndPositionsLODS[lodIndex].uvSizeAndPositionsArray.Length, float4Size);
                curSpawnData.uvSizeAndPositionBuffer.SetData(particleData.uvSizeAndPositionsLODS[lodIndex].uvSizeAndPositionsArray);

                curSpawnData.mpb.SetInt(materialIDs.ids.spriteCount, particleData.uvSizeAndPositionsLODS[lodIndex].uvSizeAndPositionsArray.Length);
                curSpawnData.mpb.SetTexture(materialIDs.ids.atlas, particleData.particleAtlas.texture);
                curSpawnData.mpb.SetBuffer(materialIDs.ids.uvSizeAndPos, curSpawnData.uvSizeAndPositionBuffer);
                curSpawnData.mpb.SetInt(materialIDs.ids.backgroundMask, (int)particleData.backgroundType);

                curSpawnData.active = true;

                stats.spawnerDataArray[j] = curSpawnData;

                bgParticleInputs.heightPos = particleData.spawnHeightPosition;
                bgParticleInputs.heightRange = particleData.spawnHeightRange;
                stats.backgroundInputsArray[stats.backgroundMaskCount] = bgParticleInputs;

                stats.backgroundMaskCount++;

                activeSpawnersForParticle++;

                if (activeSpawnersForParticle == particleData.uvSizeAndPositionsLODS.Length) break;
            }
        }
        settings.backgroundParticleCompute.SetInt(materialIDs.ids.backgroundMaskCount, stats.backgroundMaskCount);
        stats.inputComputeBuffer.SetData(stats.backgroundInputsArray);
        settings.backgroundParticleCompute.SetBuffer(stats.updateKernelID, materialIDs.ids.bgParticleInputs, stats.inputComputeBuffer);
        settings.backgroundParticleCompute.SetBuffer(stats.initKernelID, materialIDs.ids.bgParticleInputs, stats.inputComputeBuffer);
    }
    public static void InitializeCompute(ComputeShader computeShader, MaterialIDSO materialIDs, AtlasSpawnerStatsSO stats)
    {
        updateKernelID = computeShader.FindKernel("CSUpdate");
        initKernelID = computeShader.FindKernel("CSInit");
        int computeGroups = Mathf.CeilToInt(MAX_PARTICLE_COUNT / 64);

        computeShader.SetVector(materialIDs.ids.spawnerMinPos, stats.spawnMinPos);
        computeShader.SetVector(materialIDs.ids.spawnerMaxPos, stats.spawnMaxPos);
        computeShader.SetVector(materialIDs.ids.spawnerSize, stats.spawnSize);
        computeShader.SetInt(materialIDs.ids.particleCount, MAX_PARTICLE_COUNT);
        computeShader.SetFloat(materialIDs.ids.trainVelocity, 0);
        computeShader.SetBuffer(initKernelID, materialIDs.ids.bgParticleOutputs, stats.outputComputeBuffer);

        computeShader.Dispatch(initKernelID, computeGroups, 1, 1);
    }

    public static Dictionary<BackgroundType, ParticleData> SetParticleDataDictionary(ParticleData[] particleDataArray)
    {
        Dictionary<BackgroundType, ParticleData> dict = new Dictionary<BackgroundType, ParticleData>();

        BackgroundType[] backgroundTypes = (BackgroundType[])Enum.GetValues(typeof(BackgroundType));

        for (int i = 0; i < backgroundTypes.Length; i++)
        {
            BackgroundType backgroundType = backgroundTypes[i];

            for (int j = 0; j < particleDataArray.Length; j++)
            {
                if (backgroundType == particleDataArray[j].backgroundType)
                {
                    dict.Add(backgroundType, particleDataArray[j]);
                }
            }
        }

        return dict;
    }
}
