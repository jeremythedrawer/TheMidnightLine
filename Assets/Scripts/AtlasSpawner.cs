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
        InitializeAtlasCompute(spawnerSettings.atlasCompute, materialIDs, spawnerStats);
        spawnerStats.spawnerDataArray = InitializeSpawnData(spawnerSettings.atlasCompute, materialIDs);
        spawnerSettings.currentTrip.zoneQueue = SetZoneQueue(spawnerSettings.currentTrip);
        Zone nextZone = spawnerSettings.currentTrip.zoneQueue.Dequeue();
        for (int i = 0; i < spawnerSettings.currentTrip.zones.Length; i++)
        {
            if (spawnerSettings.currentTrip.zones[i].metersStart <= trainStats.metersTravelled)
            {
                ChangeSpawner(spawnerSettings.currentTrip.zones[i], materialIDs, spawnerStats, spawnerSettings.atlasCompute);
            }
        }
    }


    private void Update()
    {
        UpdateParticles(spawnerStats, spawnerSettings.atlasCompute, spyStats);
    }
    private void InitializeBoundParameters()
    {
        float camMeterWidth = camSettings.maxProjectionSize * camStats.aspect;
        float firstStationPos = stationsData.stations[0].metersPosition;

        spawnerStats.spawnMaxPos.x = firstStationPos + trainStats.trainHalfLength + camMeterWidth + spawnerSettings.boundBufferAmount;
        spawnerStats.spawnMaxPos.y = trainStats.trainMaxHeight + camSettings.maxProjectionSize + spawnerSettings.boundBufferAmount;
        spawnerStats.spawnMaxPos.z = 32; //NOTE: Furthest postion. If changed this needs to be changed on the compute shader as well

        spawnerStats.spawnMinPos.x = firstStationPos - trainStats.trainHalfLength - camMeterWidth - spawnerSettings.boundBufferAmount;
        spawnerStats.spawnMinPos.y = -camSettings.maxProjectionSize - spawnerSettings.boundBufferAmount;
        spawnerStats.spawnMinPos.z = Camera.main.transform.position.z;

        spawnerStats.spawnCenter = (spawnerStats.spawnMinPos + spawnerStats.spawnMaxPos) * 0.5f;
        spawnerStats.spawnBoundsSize = spawnerStats.spawnMaxPos - spawnerStats.spawnMinPos;

        transform.position = spawnerStats.spawnMinPos;
    }

    private void OnDrawGizmosSelected()
    {
        InitializeBoundParameters();
        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawnerStats.spawnCenter, spawnerStats.spawnBoundsSize);
    }
}
