using Proselyte.Sigils;
using System;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    public StationsDataSO stationsData;
    public TrainStatsSO trainStats;
    public AtlasSpawnerStatsSO spawnerStats;
    public GameEventDataSO gameEventData;

    private void Awake()
    {
        for (int i = 1; i < stationsData.stations.Length; i++)
        {
            stationsData.stations[i].isSpawned = false;
        }
    }
    private void OnEnable()
    {
        gameEventData.OnSpawnStation.RegisterListener(SpawnStation);
    }

    private void OnDisable()
    {
        gameEventData.OnSpawnStation.UnregisterListener(SpawnStation);
    }

    private void SpawnStation()
    {
        Station nextStationPrefab = stationsData.stations[trainStats.nextStationIndex].stationPrefab;
        Station nextStation = Instantiate(nextStationPrefab, transform);
        float stationXPos = spawnerStats.spawnMaxPos.x + (nextStation.transform.position.x - nextStation.platformCollider.bounds.min.x);
        nextStation.transform.position = new Vector3(stationXPos, 0, 0);
    }
}
