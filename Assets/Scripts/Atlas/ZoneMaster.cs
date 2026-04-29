using UnityEngine;
using static AtlasSpawn;
using static Train;
using static Parallax;
using static Spy;
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
        spawner.atlasCompute.SetFloat("_CamVelocity", (camStats.curVelocity.x));

        if (spyStats.curLocationState != LocationState.Station)
        {
            spawner.atlasCompute.SetFloat("_TrainVelocity", (trainStats.curVelocity * Time.deltaTime));
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
        spawner.atlasCompute.SetVector("_SpawnerMinPos", spawner.bounds.min);
        spawner.atlasCompute.SetVector("_SpawnerMaxPos", spawner.bounds.max);
        spawner.atlasCompute.SetVector("_SpawnerSize", spawner.bounds.size);
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
        //InitBoundParameters();

        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawner.bounds.center, spawner.bounds.size);
    }
}
