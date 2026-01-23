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
    float spawnTimer;
    int spawnCount;
    GraphicsBuffer indexBuffer;
    public Spawn.SpawnerData[] spawnerDataArray;
    private void Awake()
    {
        stats.backgroundMasksArray = new int[32];
        InitializeBoundParameters();
    }
    private void OnDisable()
    {
        ReleaseBuffers();
    }
    private void Start()
    {
        InitializeIndexBuffer();
        InitializeCompute();
        InitializeSpawnMaterialData();
        stats.curBackgroundTypes = settings.timeStamp[0].backgroundType;
        UpdateSpawners();
    }
    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= 0.3f)
        {
            spawnTimer -= 0.3f;
            spawnCount = Mathf.Min(spawnCount + 1, settings.maxParticleCount);
        }

        UpdateCompute();
        for (int i = 0; i < spawnerDataArray.Length; i++)
        {
            if (spawnerDataArray[i].active)
            {
                Graphics.RenderPrimitivesIndexed(spawnerDataArray[i].renderParams, MeshTopology.Quads, indexBuffer, settings.maxParticleCount * 4);
            }
        }
    }
    private void InitializeBoundParameters()
    {
        float camMeterWidth = camSettings.maxProjectionSize * camStats.aspect;
        stats.topRightFront.x = ((stationsData.stations[0].metersPosition + trainStats.trainHalfLength) + camMeterWidth);
        stats.topRightFront.y = (trainStats.trainMaxHeight + camSettings.maxProjectionSize);
        stats.bottomLeftFront.x = ((stationsData.stations[0].metersPosition - trainStats.trainHalfLength) - camMeterWidth);
        stats.bottomLeftFront.y =  -camSettings.maxProjectionSize;
        Vector3 bufferOffet = new Vector3(settings.bufferAmount, settings.bufferAmount, 0f);
        stats.topRightFront += bufferOffet;
        stats.bottomLeftFront -= bufferOffet;
        stats.topRightBack = stats.topRightFront + new Vector3(0, 0, settings.spawnDepth);
        stats.bottomLeftBack = stats.bottomLeftFront + new Vector3(0, 0, settings.spawnDepth);
        stats.lodPosition0 = settings.spawnDepth / 3;
        stats.lodPosition1 = stats.lodPosition0 * 2;

        transform.position = new Vector3(stats.topRightFront.x, settings.spawnHeight, stats.topRightFront.z);
    }
    private void InitializeCompute()
    {
        stats.kernel = settings.backgroundParticleCompute.FindKernel("CSMain");
        stats.computeGroups = Mathf.CeilToInt(settings.maxParticleCount / 64);

        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.worldFarClipPlane, camStats.worldFarClipPlane);
        settings.backgroundParticleCompute.SetVector(materialIDs.ids.spawnerPosition, transform.position);
        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.spawnDepth, settings.spawnDepth);
        settings.backgroundParticleCompute.SetInt(materialIDs.ids.particleCount, spawnCount);
        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.minBoundsWorldXPos, stats.bottomLeftFront.x);
        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.trainVelocity, 0);
        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.lodLevelThreshold0, stats.lodPosition0);
        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.lodLevelThreshold1, stats.lodPosition1);
        settings.backgroundParticleCompute.SetInt(materialIDs.ids.particleCount, settings.maxParticleCount);
        int floatSize = sizeof(float);
        int float3Size = floatSize * 3;
        int intSize = sizeof(int);
        int bgAttributeStride = float3Size + floatSize + intSize;

        stats.particleComputeBuffer = new ComputeBuffer(settings.maxParticleCount, bgAttributeStride);
        stats.backgroundMaskBuffer = new ComputeBuffer(stats.backgroundMasksArray.Length, sizeof(int));
        settings.backgroundParticleCompute.SetBuffer(stats.kernel, materialIDs.ids.bgParticles, stats.particleComputeBuffer);
        settings.backgroundParticleCompute.SetBuffer(stats.kernel, materialIDs.ids.backgroundMasks, stats.backgroundMaskBuffer);
        settings.backgroundParticleCompute.Dispatch(stats.kernel, stats.computeGroups, 1, 1);
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
        stats.renderParamsBounds = new Bounds(transform.position, new Vector3(1000, 1000, 1000)); //TODO: write proper bounds sizes
        spawnerDataArray = new Spawn.SpawnerData[32];
        for (int i = 0; i < spawnerDataArray.Length; i++)
        {
            Spawn.SpawnerData m_spawnerData = spawnerDataArray[i];
            m_spawnerData.material = Instantiate(settings.backgroundMaterial);
            m_spawnerData.renderParams = new RenderParams(m_spawnerData.material) { worldBounds = stats.renderParamsBounds };
            m_spawnerData.material.SetBuffer(materialIDs.ids.bgParticles, stats.particleComputeBuffer);
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
            stats.backgroundMasksArray[stats.backgroundMaskCount] = activeBGMask;
            stats.backgroundMaskCount++;

            for (int j = 0; j < settings.particleData.Length; j++)
            {
                Spawn.ParticleData particleData = settings.particleData[j];
                int particleBGMask = (int)particleData.backgroundType;
                
                if ((particleBGMask & activeBGMask) == 0) continue; // Match particleData to the curBGMask

                for (int k = 0; k < spawnerDataArray.Length; k++)
                {
                    Spawn.SpawnerData spawnerData = spawnerDataArray[k];
                    
                    if (spawnerData.active) continue; // Find an unused spawner

                    spawnerDataArray[k] = SetSpawnData(spawnerData, particleData);
                    break;
                }
            }

        }
        settings.backgroundParticleCompute.SetInt(materialIDs.ids.backgroundMaskCount, stats.backgroundMaskCount);

        stats.backgroundMaskBuffer.SetData(stats.backgroundMasksArray);

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
            settings.backgroundParticleCompute.Dispatch(stats.kernel, stats.computeGroups, 1, 1);
        }
    }
    private void ReleaseBuffers()
    {
        indexBuffer?.Release();
        stats.particleComputeBuffer?.Release();
        stats.backgroundMaskBuffer?.Release();
        for(int i = 0; i < spawnerDataArray.Length; i++)
        {
            spawnerDataArray[i].uvPositionsBuffer?.Release();
            spawnerDataArray[i].uvSizesBuffer?.Release();
        }
    }
    private void OnDrawGizmosSelected()
    {
        InitializeBoundParameters();

        Vector3 tlf = new Vector3(stats.bottomLeftFront.x, stats.topRightFront.y, stats.topRightFront.z);
        Vector3 brf = new Vector3(stats.topRightFront.x, stats.bottomLeftFront.y, stats.topRightFront.z);

        Vector3 tlb = new Vector3(stats.bottomLeftFront.x, stats.topRightFront.y, stats.topRightBack.z);
        Vector3 brb = new Vector3(stats.topRightFront.x, stats.bottomLeftFront.y, stats.topRightBack.z);

        Vector3 spawnLB = new Vector3(stats.bottomLeftBack.x, settings.spawnHeight, stats.bottomLeftBack.z);
        Vector3 spawnLF = new Vector3(stats.bottomLeftFront.x, settings.spawnHeight, stats.bottomLeftFront.z);
        Vector3 spawnRB = new Vector3(stats.topRightBack.x, settings.spawnHeight, stats.topRightBack.z);
        Vector3 spawnRF = new Vector3(stats.topRightFront.x, settings.spawnHeight, stats.topRightFront.z);
        Gizmos.color = Color.crimson;
        // Front face
        Gizmos.DrawLine(stats.topRightFront, tlf);
        Gizmos.DrawLine(tlf, stats.bottomLeftFront);
        Gizmos.DrawLine(stats.bottomLeftFront, brf);
        Gizmos.DrawLine(brf, stats.topRightFront);
        // Back face
        Gizmos.DrawLine(stats.topRightBack, tlb);
        Gizmos.DrawLine(tlb, stats.bottomLeftBack);
        Gizmos.DrawLine(stats.bottomLeftBack, brb);
        Gizmos.DrawLine(brb, stats.topRightBack);

        // Connecting edges
        Gizmos.DrawLine(stats.topRightFront, stats.topRightBack);
        Gizmos.DrawLine(tlf, tlb);
        Gizmos.DrawLine(stats.bottomLeftFront, stats.bottomLeftBack);
        Gizmos.DrawLine(brf, brb);

        Gizmos.DrawLine(spawnLB, spawnLF);
        Gizmos.DrawLine(spawnRB, spawnRF);

        Gizmos.DrawLine(new Vector3(stats.topRightFront.x, settings.spawnHeight, stats.lodPosition0), new Vector3(stats.bottomLeftFront.x, settings.spawnHeight, stats.lodPosition0));
        Gizmos.DrawLine(new Vector3(stats.topRightFront.x, settings.spawnHeight, stats.lodPosition1), new Vector3(stats.bottomLeftFront.x, settings.spawnHeight, stats.lodPosition1));
    }
}
