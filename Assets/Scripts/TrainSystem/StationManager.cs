using Proselyte.Sigils;
using System;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    public StationsDataSO stationsData;
    public TrainStatsSO trainStats;
    public AtlasSpawnerStatsSO spawnerStats;
    public GameEventDataSO gameEventData;

    StationSO curStation;
    float spawnThreshold;
    private void Start()
    {
        curStation = stationsData.stations[0];
        spawnThreshold = curStation.metersPosition - spawnerStats.trainToMaxBoundDist;
        spawnerStats.trainToMaxBoundDist = spawnerStats.spawnMaxPos.x - curStation.metersPosition; // Note: Using the first stations position as the train will arrive there anyways
    }
    private void Update()
    {
        //if (spawnThreshold > trainStats.metersTravelled)
        //{
        //    SpawnStation();
        //}
    }
    private void SpawnStation()
    {
        curStation = stationsData.stations[trainStats.nextStationIndex];
        spawnThreshold = curStation.metersPosition - spawnerStats.trainToMaxBoundDist;
        Station nextStation = Instantiate(curStation.stationPrefab, transform);
        float stationXPos = spawnerStats.spawnMaxPos.x + (nextStation.transform.position.x - nextStation.platformCollider.bounds.min.x);
        nextStation.transform.position = new Vector3(stationXPos, 0, 0);
    }
}
