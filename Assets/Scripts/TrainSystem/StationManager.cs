using Proselyte.Sigils;
using System;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    public TripSO trip;
    public TrainStatsSO trainStats;
    public ZoneSpawnerSO spawner;
    public GameEventDataSO gameEventData;

    public Station[] stations;

    public float curMetersSpawnThreshold = float.MaxValue;
    public float trainToMaxSpawnDist;

    public int stationSpawnFlag;
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
        InitStationManager();
    }
    private void Start()
    {
        SpawnFirstStation();
    }
    private void Update()
    {
        if (trainStats.metersTravelled > curMetersSpawnThreshold && (stationSpawnFlag & 1 << trip.curStationIndex) == 0)
        {
            SpawnStation();
        }
    }
    private void SpawnFirstStation()
    {
        Station nextStation = stations[trip.curStationIndex];
        float stationXPos = nextStation.station.metersPosition;
        nextStation.transform.position = new Vector3(stationXPos, 0, 0);

        stationSpawnFlag |= 1 << trip.curStationIndex;

        nextStation.gameObject.SetActive(true);
        nextStation.SpawnNPCs();
    }
    private void SpawnStation()
    {
        Station nextStation = stations[trip.curStationIndex];
        float stationXPos = spawner.spawnMaxPos.x + (nextStation.transform.position.x - nextStation.platformRenderer.bounds.min.x);
        nextStation.transform.position = new Vector3(stationXPos, 0, 0);

        stationSpawnFlag |= 1 << trip.curStationIndex;

        nextStation.gameObject.SetActive(true);
        nextStation.SpawnNPCs();
    }

    private void InitStationManager()
    {
        trip.curStationIndex = 0;
        trip.curStation = trip.stationsDataArray[0];

        stations = new Station[trip.stationsDataArray.Length];

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO stationData = trip.stationsDataArray[i];
            stationData.stationName = stationData.station_prefab.name;

            Station station = Instantiate(stationData.station_prefab, transform);
            stations[i] = station;
            station.gameObject.SetActive(false);
        }
    }

    private void InitOnTrainArrivedAtStartPosition()
    {
        trainToMaxSpawnDist = spawner.spawnMaxPos.x - trip.stationsDataArray[0].metersPosition;
    }
    private void SetStation()
    {
        trip.curStationIndex++;
        trip.curStation = trip.stationsDataArray[trip.curStationIndex];
        trainStats.curPassengerCount = 0;
        trainStats.targetPassengerCount = trip.curStation.traitorSpawnAmount + trip.curStation.bystanderProfiles.Length + trip.curStation.traitorProfiles.Length;
        curMetersSpawnThreshold = trip.curStation.metersPosition - Mathf.Abs(trip.curStation.station_prefab.transform.localPosition.x) - trainToMaxSpawnDist - (trainStats.trainWorldWidth / 2);
    }
}
