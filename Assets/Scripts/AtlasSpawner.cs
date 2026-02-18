using UnityEngine;
using static AtlasSpawn;

public class AtlasSpawner : MonoBehaviour
{
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] StationsDataSO stationsData;
    [SerializeField] CameraSettingsSO camSettings;
    [SerializeField] CameraStatsSO camStats;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] AtlasSpawnerSettingsSO spawnerSettings;
    [SerializeField] AtlasSpawnerStatsSO spawnerStats;
    [SerializeField] GameEventDataSO gameData;
    [SerializeField] SpyStatsSO spyStats;


    private void OnValidate()
    {
        InitializeBoundParameters();
        
        spawnerStats.backgroundInputsArray = new BackgroundParticleInputs[SPAWNER_COUNT];
        spawnerStats.outputComputeBuffer = new ComputeBuffer(MAX_PARTICLE_COUNT, PARTICLE_OUTPUT_STRIDE);
        spawnerStats.inputComputeBuffer = new ComputeBuffer(SPAWNER_COUNT, PARTICLE_INPUT_STRIDE);
        
        spawnerStats.lodWriteOffsets = new int[MAX_LOD_COUNT];
        spawnerStats.lodWriteOffsetsBuffer = new ComputeBuffer(MAX_LOD_COUNT, sizeof(int), ComputeBufferType.Structured);

        spawnerStats.lodThresholdBuffer = new ComputeBuffer(MAX_LOD_COUNT - 1, sizeof(int), ComputeBufferType.Structured);

        spawnerStats.lodBuffers = new ComputeBuffer[MAX_LOD_COUNT];

        for (int i = 0; i < MAX_LOD_COUNT; i++)
        {
            spawnerStats.lodBuffers[i] = new ComputeBuffer(MAX_LOD_PARTICLE_COUNT, PARTICLE_INPUT_STRIDE);
        }

        InitializeParticleDataArray(spawnerSettings);
        spawnerStats.particleDataDict = SetParticleDataDictionary(spawnerSettings.particleData);

        InitializeCompute(spawnerSettings.backgroundParticleCompute, materialIDs, spawnerStats);
        spawnerStats.spawnerDataArray = InitializeSpawnDataArray(spawnerStats.lodBuffers, materialIDs);

        ChangeBiome(spawnerStats, spawnerSettings, materialIDs);
    }

    private void Start()
    {
        InitializeBoundParameters();
        ChangeBiome(spawnerStats, spawnerSettings, materialIDs);
        spawnerStats.curBackgroundTypes = spawnerSettings.biomes[0].backgroundType;

    }
    private void Update()
    {
        UpdateCompute();

    }
    private void InitializeBoundParameters()
    {
        float camMeterWidth = camSettings.maxProjectionSize * camStats.aspect;
        float firstStationPos = stationsData.stations[0].metersPosition;

        spawnerStats.spawnMaxPos.x = firstStationPos + trainStats.trainHalfLength + camMeterWidth + spawnerSettings.boundBufferAmount;
        spawnerStats.spawnMaxPos.y = trainStats.trainMaxHeight + camSettings.maxProjectionSize + spawnerSettings.boundBufferAmount;
        spawnerStats.spawnMaxPos.z = spawnerSettings.maxSpawnDepth;

        spawnerStats.spawnMinPos.x = firstStationPos - trainStats.trainHalfLength - camMeterWidth - spawnerSettings.boundBufferAmount;
        spawnerStats.spawnMinPos.y = -camSettings.maxProjectionSize - spawnerSettings.boundBufferAmount;
        spawnerStats.spawnMinPos.z = spawnerSettings.minSpawnDepth;

        spawnerStats.spawnCenter = (spawnerStats.spawnMinPos + spawnerStats.spawnMaxPos) * 0.5f;
        spawnerStats.spawnBoundsSize = spawnerStats.spawnMaxPos - spawnerStats.spawnMinPos;

        spawnerStats.lodThresholds = new float[MAX_LOD_COUNT - 1];
        for (int i = 0; i < spawnerStats.lodThresholds.Length; i++)
        {
            spawnerStats.lodThresholds[i] = spawnerStats.spawnBoundsSize.z * ((i + 1) / (float)MAX_LOD_COUNT);
        }


        transform.position = spawnerStats.spawnMinPos;
        spawnerStats.renderParamsBounds = new Bounds(spawnerStats.spawnCenter, spawnerStats.spawnBoundsSize);


    }

    private void UpdateCompute()
    {
        if (spyStats.onTrain)
        {
            spawnerSettings.backgroundParticleCompute.SetFloat(materialIDs.ids.trainVelocity, trainStats.curVelocity);
            spawnerSettings.backgroundParticleCompute.Dispatch(spawnerStats.updateKernelID, spawnerStats.computeGroups, 1, 1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawnerStats.spawnCenter, spawnerStats.spawnBoundsSize);


        for (int i = 0; i < spawnerStats.lodThresholds.Length; i++)
        {
            Vector3 right = new Vector3(spawnerStats.spawnMinPos.x, spawnerSettings.spawnHeight, spawnerStats.lodThresholds[i]);
            Vector3 left = new Vector3(spawnerStats.spawnMaxPos.x, spawnerSettings.spawnHeight, spawnerStats.lodThresholds[i]);
            Gizmos.DrawLine(left, right);
        }
    }
}
