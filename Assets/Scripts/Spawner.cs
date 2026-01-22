using UnityEngine;
using UnityEngine.VFX;

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
    float spawnTimer;
    int spawnCount;
    GraphicsBuffer indexBuffer;
    public Spawn.SpawnerData[] spawnerData;
    private void Awake()
    {
        InitializeBoundParameters();
    }
    private void OnDisable()
    {
        indexBuffer?.Release();
    }
    private void Start()
    {
        InitializeIndexBuffer();
        InitializeCompute();
        InitializeSpawnMaterialData();
        stats.curBackgroundTypes = Spawn.BackgroundType.Trees;
        stats.spawnerMask = 0;
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
        for (int i = 0; i < spawnerData.Length; i++)
        {
            if (spawnerData[i].active)
            {
                Graphics.RenderPrimitivesIndexed(spawnerData[i].renderParams, MeshTopology.Quads, indexBuffer, settings.maxParticleCount * 4);
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
        stats.lodPositions = new Vector3[settings.LODThresholdAmount - 1];

        float lodIncrementSize = settings.spawnDepth / settings.LODThresholdAmount;
        for (int i = 0; i < stats.lodPositions.Length; i++)
        {
            stats.lodPositions[i] = new Vector3(stats.topRightFront.x, settings.spawnHeight, lodIncrementSize * (i + 1));
        }

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

        int floatSize = sizeof(float);
        int float3Size = floatSize * 3;
        int intSize = sizeof(int);
        int bgAttributeStride = float3Size + floatSize + intSize;

        stats.backgroundComputeBuffer = new ComputeBuffer(settings.maxParticleCount, bgAttributeStride);
        
        settings.backgroundParticleCompute.SetBuffer(stats.kernel, materialIDs.ids.bgParticles, stats.backgroundComputeBuffer);

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
        spawnerData = new Spawn.SpawnerData[32];
        for (int i = 0; i < spawnerData.Length; i++)
        {
            Spawn.SpawnerData m_spawnerData = spawnerData[i];
            m_spawnerData.material = Instantiate(settings.backgroundMaterial);
            m_spawnerData.renderParams = new RenderParams(m_spawnerData.material) { worldBounds = stats.renderParamsBounds };
            m_spawnerData.material.SetBuffer(materialIDs.ids.bgParticles, stats.backgroundComputeBuffer);
            spawnerData[i] = m_spawnerData;
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
        for (int i = 0; i < 32; i++)
        {
            if ((stats.spawnerMask & (1 << i)) == 0) // Finding an unused spawner
            {
                Spawn.SpawnerData m_spawnerData = spawnerData[i];

                int treeMask = (int)Spawn.BackgroundType.Trees;
                if ((curBGMask & treeMask) != 0) // Am I about to render trees?
                {
                    for (int j = 0; j < settings.particleData.Length; j++)
                    {
                        Spawn.ParticleData particleData = settings.particleData[j];
                        if ((particleData.backgroundType & Spawn.BackgroundType.Trees) != 0) // Which settings has the tree mask
                        {
                            m_spawnerData.material.SetTexture(materialIDs.ids.atlas, particleData.atlas);

                            m_spawnerData.uvPositionsBuffer?.Dispose();
                            m_spawnerData.uvPositionsBuffer?.Release();
                            m_spawnerData.uvPositionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleData.uvPositions.Length, sizeof(float) * 2);
                            m_spawnerData.uvPositionsBuffer.SetData(particleData.uvPositions);
                            m_spawnerData.material.SetBuffer(materialIDs.ids.uvPositions, m_spawnerData.uvPositionsBuffer);

                            m_spawnerData.uvSizesBuffer?.Dispose();
                            m_spawnerData.uvSizesBuffer?.Release();
                            m_spawnerData.uvSizesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleData.uvSizes.Length, sizeof(float) * 2);
                            m_spawnerData.uvSizesBuffer.SetData(particleData.uvSizes);
                            m_spawnerData.material.SetBuffer(materialIDs.ids.uvSizes, m_spawnerData.uvSizesBuffer);
                            m_spawnerData.material.SetInt(materialIDs.ids.spriteCount, particleData.uvSizes.Length);
                            m_spawnerData.active = true;
                        }
                    }
                    curBGMask ^= treeMask;
                }

                int houseMask = (int)Spawn.BackgroundType.Houses;
                if ((curBGMask &  houseMask) != 0)
                {
                    for (int j = 0; j < settings.particleData.Length; j++)
                    {
                        if ((settings.particleData[j].backgroundType & Spawn.BackgroundType.Houses) != 0) // Which settings has the tree mask
                        {
                            m_spawnerData.material.SetTexture(materialIDs.ids.atlas, settings.particleData[j].atlas);
                        }
                    }
                    curBGMask ^= houseMask;
                }

                int buildingMask = (int)Spawn.BackgroundType.Buildings;
                if ((curBGMask & buildingMask) != 0)
                {

                }

                spawnerData[i] = m_spawnerData;
                stats.spawnerMask |= 1 << i;
            }
        }   
    }
    private void UpdateCompute()
    {
        settings.backgroundParticleCompute.SetInt(materialIDs.ids.particleCount, spawnCount);
        settings.backgroundParticleCompute.SetFloat(materialIDs.ids.trainVelocity, trainStats.curVelocity);
        settings.backgroundParticleCompute.Dispatch(stats.kernel, stats.computeGroups, 1, 1);
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

        for (int i = 0; i < stats.lodPositions.Length; i++)
        {
            Gizmos.DrawLine(stats.lodPositions[i], new Vector3(stats.bottomLeftFront.x, stats.lodPositions[i].y, stats.lodPositions[i].z));
        }
    }
}
