using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static Spawn;

public class Spawner : MonoBehaviour
{
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] StationsDataSO stationsData;
    [SerializeField] CameraSettingsSO camSettings;
    [SerializeField] CameraStatsSO camStats;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] SpawnerSettingsSO settings;
    [SerializeField] SpawnerStatsSO stats;
    [SerializeField] GameEventDataSO gameData;
    [SerializeField] SpyStatsSO spyStats;

    GraphicsBuffer indexBuffer;
    public Spawn.SpawnerData[] spawnerDataArray;
    private void OnDisable()
    {
        ReleaseBuffers();
    }
    private void Start()
    {
        CreateBuffers();
        InitializeBoundParameters();
        InitializeSpawnMaterialData();
        stats.curBackgroundTypes = settings.timeStamp[0].backgroundType;
        UpdateSpawners();
        InitializeIndexBuffer();
        InitializeCompute();
    }
    private void Update()
    {

        UpdateCompute();
        for (int i = 0; i < spawnerDataArray.Length; i++)
        {
            if (spawnerDataArray[i].active)
            {
                Graphics.RenderPrimitivesIndexed(spawnerDataArray[i].renderParams, MeshTopology.Quads, indexBuffer, settings.maxParticleCount * 4);
            }
        }
    }
    private void CreateBuffers()
    {
        int floatSize = sizeof(float);
        int float3Size = floatSize * 3;
        int intSize = sizeof(uint);
        int bgParticleOutputsStride = float3Size + (floatSize * 2) + (intSize * 4);
        int bgParticleInputsStride = intSize + (floatSize * 2);

        stats.backgroundInputsArray = new Spawn.BackgroundParticleInputs[32];
        
        stats.particleComputeBuffer = new ComputeBuffer(settings.maxParticleCount, bgParticleOutputsStride);
        stats.backgroundParticleInputBuffer = new ComputeBuffer(stats.backgroundInputsArray.Length, bgParticleInputsStride);
    }
    private void InitializeBoundParameters()
    {
        float camMeterWidth = camSettings.maxProjectionSize * camStats.aspect;
        float firstStationPos = stationsData.stations[0].metersPosition;

        stats.spawnMaxPos.x = firstStationPos + trainStats.trainHalfLength + camMeterWidth + settings.bufferAmount;
        stats.spawnMaxPos.y = trainStats.trainMaxHeight + camSettings.maxProjectionSize + settings.bufferAmount;
        stats.spawnMaxPos.z = settings.spawnDepth;

        stats.spawnMinPos.x = firstStationPos - trainStats.trainHalfLength - camMeterWidth - settings.bufferAmount;
        stats.spawnMinPos.y = -camSettings.maxProjectionSize - settings.bufferAmount;
        stats.spawnMinPos.z = 0;

        stats.spawnCenter = (stats.spawnMinPos + stats.spawnMaxPos) * 0.5f;
        stats.spawnSize = stats.spawnMaxPos - stats.spawnMinPos;

        stats.lodZPosition0 = settings.spawnDepth / 3;
        stats.lodZPosition1 = stats.lodZPosition0 * 2;

        transform.position = stats.spawnMinPos;
        stats.renderParamsBounds = new Bounds(stats.spawnCenter, stats.spawnSize);
    }
    private void InitializeCompute()
    {
        stats.updateKernelID = settings.backgroundParticleCompute.FindKernel("CSUpdate");
        stats.initKernelID = settings.backgroundParticleCompute.FindKernel("CSInit");
        stats.computeGroups = Mathf.CeilToInt(settings.maxParticleCount / 64);

        settings.backgroundParticleCompute.SetVector(materialIDs.ids.spawnerMinPos, stats.spawnMinPos);
        settings.backgroundParticleCompute.SetVector(materialIDs.ids.spawnerMaxPos, stats.spawnMaxPos);
        settings.backgroundParticleCompute.SetVector(materialIDs.ids.spawnerSize, stats.spawnSize);

        settings.backgroundParticleCompute.SetInt(materialIDs.ids.particleCount, settings.maxParticleCount);

        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.trainVelocity, 0);
        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.lodLevelThreshold0, stats.lodZPosition0);
        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.lodLevelThreshold1, stats.lodZPosition1);

        settings.backgroundParticleCompute.SetBuffer(stats.initKernelID, materialIDs.ids.bgParticleOutputs, stats.particleComputeBuffer);
        settings.backgroundParticleCompute.SetBuffer(stats.updateKernelID, materialIDs.ids.bgParticleOutputs, stats.particleComputeBuffer);

        settings.backgroundParticleCompute.Dispatch(stats.initKernelID, stats.computeGroups, 1, 1);
    }
    private void InitializeIndexBuffer()
    {
        int particleCount = settings.maxParticleCount;
        int indexCount = particleCount * 6; // Each particle is assumed to be a quad which is two triangles and therefore 6 indices.

        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indexCount, sizeof(uint));

        uint[] indices = new uint[indexCount];

        for (int i = 0; i < particleCount; i++)
        {
            int v = i * 4; // vertex index of the quad
            int t = i * 6; // vertex index of the two triangles in the quad
            indices[t + 0] = (uint)(v + 0);
            indices[t + 1] = (uint)(v + 1);
            indices[t + 2] = (uint)(v + 2);
            indices[t + 3] = (uint)(v + 2);
            indices[t + 4] = (uint)(v + 3);
            indices[t + 5] = (uint)(v + 0);
        }

        indexBuffer.SetData(indices);
    }
    private void InitializeSpawnMaterialData()
    {

        spawnerDataArray = new Spawn.SpawnerData[32];
        for (int i = 0; i < spawnerDataArray.Length; i++)
        {
            Spawn.SpawnerData m_spawnerData = spawnerDataArray[i];
            m_spawnerData.material = Instantiate(settings.backgroundMaterial);
            m_spawnerData.renderParams = new RenderParams(m_spawnerData.material) { worldBounds = stats.renderParamsBounds };
            m_spawnerData.material.SetBuffer(materialIDs.ids.bgParticleOutputs, stats.particleComputeBuffer);
            spawnerDataArray[i] = m_spawnerData;
        }

        for (int i = 0; i < settings.particleData.Length; i++)
        {
            Spawn.ParticleData particleData = settings.particleData[i];
            particleData.uvPositions = new Vector2[particleData.atlasSprites.Length];
            particleData.uvSizes = new Vector2[particleData.atlasSprites.Length];
            for(int j = 0; j < particleData.atlasSprites.Length; j++)
            {
                Texture2D atlas = particleData.atlas;
                Sprite curSprite = particleData.atlasSprites[j];

                particleData.uvPositions[j].x = curSprite.textureRect.min.x / atlas.width;
                particleData.uvPositions[j].y = curSprite.textureRect.min.y / atlas.height;
                particleData.uvSizes[j].x = curSprite.textureRect.size.x / atlas.width;
                particleData.uvSizes[j].y = curSprite.textureRect.size.y / atlas.height;
            }
            settings.particleData[i] = particleData;
        }
    }
    private void UpdateSpawners()
    {
        int curBGMask = (int)stats.curBackgroundTypes;
        stats.backgroundMaskCount = 0;
        for(int i = 0; i < 32; i++)
        {
            if ((curBGMask & (1 << i)) == 0) continue; // Find an active background type flag
            int activeBGMask = 1 << i;
            Spawn.BackgroundParticleInputs bgParticleInputs = stats.backgroundInputsArray[stats.backgroundMaskCount];
            bgParticleInputs.bgMask = activeBGMask;

            for (int j = 0; j < settings.particleData.Length; j++)
            {
                Spawn.ParticleData particleData = settings.particleData[j];
                int particleBGMask = (int)particleData.backgroundType;
                
                if ((particleBGMask & activeBGMask) == 0) continue; // Match particleData to the curBGMask

                Debug.Log(stats.backgroundInputsArray[0].heightRange);

                for (int k = 0; k < spawnerDataArray.Length; k++)
                {
                    Spawn.SpawnerData spawnerData = spawnerDataArray[k];
                    
                    if (spawnerData.active) continue; // Find an unused spawner

                    spawnerDataArray[k] = SetSpawnData(spawnerData, particleData);
                    bgParticleInputs.heightPos = particleData.spawnHeightPosition;
                    bgParticleInputs.heightRange = particleData.spawnHeightRange;
                    stats.backgroundInputsArray[stats.backgroundMaskCount] = bgParticleInputs;
                    stats.backgroundMaskCount++;
                    break;
                }
            }

        }
        settings.backgroundParticleCompute.SetInt(materialIDs.ids.backgroundMaskCount, stats.backgroundMaskCount);
        stats.backgroundParticleInputBuffer.SetData(stats.backgroundInputsArray);
        settings.backgroundParticleCompute.SetBuffer(stats.updateKernelID, materialIDs.ids.bgParticleInputs, stats.backgroundParticleInputBuffer);
        settings.backgroundParticleCompute.SetBuffer(stats.initKernelID, materialIDs.ids.bgParticleInputs, stats.backgroundParticleInputBuffer);

    }
    private Spawn.SpawnerData SetSpawnData(Spawn.SpawnerData spawnData, Spawn.ParticleData particleData)
    {
        spawnData.uvPositionsBuffer?.Dispose();
        spawnData.uvPositionsBuffer?.Release();
        spawnData.uvPositionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleData.uvPositions.Length, sizeof(float) * 2);
        spawnData.uvPositionsBuffer.SetData(particleData.uvPositions);

        spawnData.uvSizesBuffer?.Dispose();
        spawnData.uvSizesBuffer?.Release();
        spawnData.uvSizesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleData.uvSizes.Length, sizeof(float) * 2);
        spawnData.uvSizesBuffer.SetData(particleData.uvSizes);

        spawnData.material.SetTexture(materialIDs.ids.atlas, particleData.atlas);
        spawnData.material.SetBuffer(materialIDs.ids.uvPositions, spawnData.uvPositionsBuffer);
        spawnData.material.SetBuffer(materialIDs.ids.uvSizes, spawnData.uvSizesBuffer);
        spawnData.material.SetInt(materialIDs.ids.spriteCount, particleData.uvSizes.Length);
        spawnData.material.SetInt(materialIDs.ids.backgroundMask, (int)particleData.backgroundType);
        spawnData.material.SetInt(materialIDs.ids.lodLevel, particleData.LODLevel);

        spawnData.active = true;
        return spawnData;
    }
    private void UpdateCompute()
    {
        if (spyStats.onTrain)
        {
            settings.backgroundParticleCompute.SetFloat(materialIDs.ids.trainVelocity, trainStats.curVelocity);
            settings.backgroundParticleCompute.Dispatch(stats.updateKernelID, stats.computeGroups, 1, 1);
        }
    }
    private void ReleaseBuffers()
    {
        indexBuffer?.Release();
        stats.particleComputeBuffer?.Release();
        stats.backgroundParticleInputBuffer?.Release();
        for(int i = 0; i < spawnerDataArray.Length; i++)
        {
            spawnerDataArray[i].uvPositionsBuffer?.Release();
            spawnerDataArray[i].uvSizesBuffer?.Release();
        }
    }
    private void OnDrawGizmosSelected()
    {
        InitializeBoundParameters();

        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(stats.spawnCenter, stats.spawnSize);

        Vector3 lod0Right = new Vector3(stats.spawnMinPos.x, settings.spawnHeight, stats.lodZPosition0);
        Vector3 lod0Left = new Vector3(stats.spawnMaxPos.x, settings.spawnHeight, stats.lodZPosition0);

        Vector3 lod1Right = new Vector3(stats.spawnMinPos.x, settings.spawnHeight, stats.lodZPosition1);
        Vector3 lod1Left = new Vector3(stats.spawnMaxPos.x, settings.spawnHeight, stats.lodZPosition1);
        Gizmos.DrawLine(lod0Left, lod0Right);
        Gizmos.DrawLine(lod1Left, lod1Right);
    }
}
