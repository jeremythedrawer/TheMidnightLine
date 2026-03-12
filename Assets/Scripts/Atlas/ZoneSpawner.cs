using UnityEngine;
using static AtlasSpawn;

[ExecuteAlways]
public class ZoneSpawner : MonoBehaviour
{
    [SerializeField] TrainStatsSO trainStats;
    [SerializeField] CameraSettingsSO camSettings;
    [SerializeField] CameraStatsSO camStats;
    [SerializeField] MaterialIDSO materialIDs;
    [SerializeField] AtlasSpawnerSettingsSO spawnerSettings;
    [SerializeField] ZoneSpawnerStatsSO spawnerStats;
    [SerializeField] TripSO trip;
    [SerializeField] GameEventDataSO gameData;
    [SerializeField] SpyStatsSO spyStats;

    private void OnEnable()
    {
        InitializeBoundParameters();
        InitializeZoneCompute(spawnerSettings.atlasCompute, materialIDs, spawnerStats);
        spawnerStats.zoneSpawnerDataArray = InitializeZoneSpawnData(spawnerSettings.atlasCompute, materialIDs);
        //spawnerSettings.currentTrip.zoneQueue = SetZoneQueue(spawnerSettings.currentTrip);
        //Zone nextZone = spawnerSettings.currentTrip.zoneQueue.Dequeue(); // TODO: This is just to show what the pattern should be at runtime
        for (int i = 0; i < trip.zones.Length; i++)
        {
            if (trip.zones[i].metersStart <= trainStats.metersTravelled)
            {
                ChangeZoneSpawner(trip.zones[i], materialIDs, spawnerStats, spawnerSettings.atlasCompute);
            }
        }

    }
    private void OnDisable()
    {
        ReleaseBuffers(spawnerStats);
    }

    private void OnDestroy()
    {
        ReleaseBuffers(spawnerStats);
    }

    private void Update()
    {
        UpdateZoneParticles(spawnerStats, spawnerSettings.atlasCompute, camStats, spyStats, trainStats);
    }
    private void InitializeBoundParameters()
    {
        float camMeterWidth = camSettings.maxProjectionSize * camStats.aspect;
        float firstStationPos = trip.stations[0].metersPosition;

        spawnerStats.spawnMaxPos.x = firstStationPos + spawnerSettings.spawnerSize.x;
        spawnerStats.spawnMaxPos.y = trainStats.trainMaxHeight + camSettings.maxProjectionSize + spawnerSettings.spawnerSize.y;
        spawnerStats.spawnMaxPos.z = 64; //NOTE: Furthest postion. If changed this needs to be changed on the compute shader as well

        spawnerStats.spawnMinPos.x = firstStationPos  - spawnerSettings.spawnerSize.x;
        spawnerStats.spawnMinPos.y = -camSettings.maxProjectionSize - spawnerSettings.spawnerSize.y;
        spawnerStats.spawnMinPos.z = Camera.main.transform.position.z;

        spawnerStats.spawnCenter = (spawnerStats.spawnMinPos + spawnerStats.spawnMaxPos) * 0.5f;
        spawnerStats.spawnBoundsSize = spawnerStats.spawnMaxPos - spawnerStats.spawnMinPos;

        transform.position = spawnerStats.spawnMinPos;
    }

    private void OnDrawGizmos()
    {
        InitializeBoundParameters();

        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawnerStats.spawnCenter, spawnerStats.spawnBoundsSize);
    }
}
