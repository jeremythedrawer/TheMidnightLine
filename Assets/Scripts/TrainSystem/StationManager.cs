using Proselyte.Sigils;
using System;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    public TripSO trip;
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
        if (trainStats.metersTravelled > curMetersSpawnThreshold && !trip.curStation.hadSpawned)
        {
            SpawnStation();
        }
    }
    private void SpawnStation()
    {
        Station nextStation = Instantiate(trip.curStation.station_prefab, null);
        float stationXPos = spawnerStats.spawnMaxPos.x + (nextStation.transform.position.x - nextStation.platformRenderer.bounds.min.x);
        nextStation.transform.position = new Vector3(stationXPos, 0, 0);
        trip.curStation.hadSpawned = true;
    }

    private void InitStationManager()
    {
        trip.curStationIndex = 0;
        trip.curStation = trip.stations[0];

        for (int i = 0; i < trip.stations.Length; i++)
        {
            trip.stations[i].hadSpawned = false;
        }
    }

    private void InitOnTrainArrivedAtStartPosition()
    {
        trainToMaxSpawnDist = spawnerStats.spawnMaxPos.x - trip.stations[0].metersPosition;
    }
    private void SetStation()
    {
        trip.curStationIndex++;
        trip.curStation = trip.stations[trip.curStationIndex];
        trainStats.curPassengerCount = 0;
        trainStats.targetPassengerCount = trip.curStation.traitorSpawnAmount + trip.curStation.bystanderSpawnAmount;
        curMetersSpawnThreshold = trip.curStation.metersPosition - Mathf.Abs(trip.curStation.station_prefab.platformRenderer.transform.localPosition.x) - trainToMaxSpawnDist - (trainStats.trainWorldWidth / 2);
    }
}
