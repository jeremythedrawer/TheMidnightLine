using UnityEngine;
using static AtlasSpawn;
using static Train;
using static Spy;
[ExecuteAlways]
public class SpawnMaster : MonoBehaviour
{
    public SpawnSO spawner;
    public CameraSettingsSO camSettings;
    public CameraStatsSO camStats;
    public TripSO trip;
    public TrainStatsSO trainStats;
    public SpyStatsSO spyStats;

    public ZoneSpawner[] zoneSpawners;
    private void OnEnable()
    {
        InitBoundParameters();
        InitZoneCompute();
        InitZoneSpawners();
        InitScrollShader();
    }
    private void Update()
    {
        spawner.zoneCompute.SetFloat("_CamVelocity", (camStats.curVelocity.x));

        if (spyStats.curLocationState != LocationState.Station)
        {
            spawner.zoneCompute.SetFloat("_TrainVelocity", (trainStats.curVelocity * Time.deltaTime));
        }
    }
    private void InitBoundParameters()
    {
        spawner.bounds.center = new Vector3(TRAIN_WORLD_POS, 0, FAR_CLIP * 0.5f);

        spawner.bounds.size = new Vector3(trip.stationsDataArray[0].station_prefab.frontPlatformRenderer.bounds.size.x + camStats.camBounds.size.x, trainStats.totalBounds.size.y + camStats.camBounds.size.y, FAR_CLIP);

        transform.position = spawner.bounds.min;
    }
    private void InitZoneCompute()
    {
        spawner.zoneCompute.SetVector("_SpawnerMinPos", spawner.bounds.min);
        spawner.zoneCompute.SetVector("_SpawnerMaxPos", spawner.bounds.max);
        spawner.zoneCompute.SetVector("_SpawnerSize", spawner.bounds.size);
        spawner.zoneCompute.SetInt("_ForegroundParticleCount", FORE_PARTICLE_COUNT);
        spawner.zoneCompute.SetInt("_MiddlegroundParticleCount", MID_PARTICLE_COUNT);
        spawner.zoneCompute.SetInt("_BackgroundParticleCount", BACK_PARTICLE_COUNT);
        spawner.zoneCompute.SetFloat("_TrainVelocity", 0);
    }
    private void InitScrollShader()
    {
        Shader.SetGlobalVector("_SpawnerSize", spawner.bounds.size);
    }
    private void InitZoneSpawners()
    {
        spawner.zoneCompute.SetInt("_Awake", 0);
        for (int i = 0; i < zoneSpawners.Length; i++)
        {
            ZoneSpawner zoneSpawner = zoneSpawners[i];
            zoneSpawner.gameObject.SetActive(true);
            zoneSpawner.InitializeZoneSpawnData();
        }
       spawner.zoneCompute.SetInt("_Awake", 1);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawner.bounds.center, spawner.bounds.size);
    }
}
