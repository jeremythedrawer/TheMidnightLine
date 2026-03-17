using UnityEngine;
using static AtlasSpawn;

[ExecuteAlways]
public class ZoneMaster : MonoBehaviour
{

    public ZoneSpawnerSO spawner;
    public CameraSettingsSO camSettings;
    public CameraStatsSO camStats;
    public TripSO trip;
    public TrainStatsSO trainStats;
    public MaterialIDSO materialIDs;
    public SpyStatsSO spyStats;

    public ZoneSpawner[] zoneSpawners;
    private void OnEnable()
    {
        InitBoundParameters();
        InitZoneCompute();
        InitZoneSpawners();
        spawner.atlasCompute.SetInt("_Awake", 1);

    }

    private void OnDisable()
    {
        DisposeZoneSpawners();
    }
    private void Update()
    {
        spawner.atlasCompute.SetFloat("_CamVelocity", (camStats.curVelocity.x * Time.deltaTime));

        if (spyStats.onTrain)
        {
            spawner.atlasCompute.SetFloat("_TrainVelocity", (trainStats.curVelocity * Time.deltaTime));

        }
    }
    private void InitBoundParameters()
    {
        float camMeterWidth = camSettings.maxProjectionSize * camStats.aspect;
        float firstStationPos = trip.stations[0].metersPosition;

        spawner.spawnMaxPos.x = firstStationPos + spawner.spawnerSize.x;
        spawner.spawnMaxPos.y = trainStats.trainMaxHeight + camSettings.maxProjectionSize + spawner.spawnerSize.y;
        spawner.spawnMaxPos.z = 64; //NOTE: Furthest postion. If changed this needs to be changed on the compute shader as well

        spawner.spawnMinPos.x = firstStationPos - spawner.spawnerSize.x;
        spawner.spawnMinPos.y = -camSettings.maxProjectionSize - spawner.spawnerSize.y;
        spawner.spawnMinPos.z = Camera.main.transform.position.z;

        spawner.spawnCenter = (spawner.spawnMinPos + spawner.spawnMaxPos) * 0.5f;
        spawner.spawnBoundsSize = spawner.spawnMaxPos - spawner.spawnMinPos;

        transform.position = spawner.spawnMinPos;
    }
    private void InitZoneCompute()
    {
        spawner.atlasCompute.SetVector("_SpawnerMinPos", spawner.spawnMinPos);
        spawner.atlasCompute.SetVector("_SpawnerMaxPos", spawner.spawnMaxPos);
        spawner.atlasCompute.SetVector("_SpawnerSize", spawner.spawnBoundsSize);
        spawner.atlasCompute.SetInt("_ForegroundParticleCount", FORE_PARTICLE_COUNT);
        spawner.atlasCompute.SetInt("_MiddlegroundParticleCount", MID_PARTICLE_COUNT);
        spawner.atlasCompute.SetInt("_BackgroundParticleCount", BACK_PARTICLE_COUNT);
        spawner.atlasCompute.SetFloat(materialIDs.ids.trainVelocity, 0);
    }
    private void InitZoneSpawners()
    {
        spawner.atlasCompute.SetInt("_Awake", 0);
        for (int i = 0; i < zoneSpawners.Length; i++)
        {
            ZoneSpawner zoneSpawner = zoneSpawners[i];
            zoneSpawner.InitializeZoneSpawnData();
        }
    }

    private void DisposeZoneSpawners()
    {
        for (int i = 0; i < zoneSpawners.Length; i++)
        {
            ZoneSpawner zoneSpawner = zoneSpawners[i];
            zoneSpawner.Dispose();
        }
    }

    private void OnDrawGizmos()
    {
        InitBoundParameters();

        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawner.spawnCenter, spawner.spawnBoundsSize);
    }
}
