using Proselyte.Sigils;
using System;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    public StationsDataSO stationsData;
    public TrainStatsSO trainStats;
    public ZoneSpawnerStatsSO spawnerStats;
    public GameEventDataSO gameEventData;

    public float curMetersSpawnThreshold = float.MaxValue;
    public float trainToMaxSpawnDist;
    private void Awake()
    {
        InitStationManager();
    }
    private void OnEnable()
    {
        gameEventData.OnStationLeave.RegisterListener(SetStation);
        gameEventData.OnTrainArrivedAtStartPosition.RegisterListener(InitOnTrainArrivedAtStartPosition);
    }

    private void OnDisable()
    {
        gameEventData.OnStationLeave.UnregisterListener(SetStation);
        gameEventData.OnTrainArrivedAtStartPosition.UnregisterListener(InitOnTrainArrivedAtStartPosition);
    }

    private void OnApplicationQuit()
    {
        InitStationManager();
    }
    private void Start()
    {
    }
    private void Update()
    {
        if (trainStats.metersTravelled > curMetersSpawnThreshold && !stationsData.curStation.hadSpawned)
        {
            SpawnStation();
        }
    }
    private void SpawnStation()
    {
        Station nextStation = Instantiate(stationsData.curStation.station_prefab, null);
        float stationXPos = spawnerStats.spawnMaxPos.x + (nextStation.transform.position.x - nextStation.platformCollider.bounds.min.x);
        nextStation.transform.position = new Vector3(stationXPos, 0, 0);
        stationsData.curStation.hadSpawned = true;
    }

    private void InitStationManager()
    {
        stationsData.curStationIndex = 0;
        stationsData.curStation = stationsData.stations[0];

        for (int i = 0; i < stationsData.stations.Length; i++)
        {
            stationsData.stations[i].hadSpawned = false;
        }
    }

    private void InitOnTrainArrivedAtStartPosition()
    {
        trainToMaxSpawnDist = spawnerStats.spawnMaxPos.x - stationsData.stations[0].metersPosition;
    }
    private void SetStation()
    {
        stationsData.curStationIndex++;
        stationsData.curStation = stationsData.stations[stationsData.curStationIndex];
        curMetersSpawnThreshold = stationsData.curStation.metersPosition - Mathf.Abs(stationsData.curStation.station_prefab.platformRenderer.transform.localPosition.x) - trainToMaxSpawnDist ;
    }
}
