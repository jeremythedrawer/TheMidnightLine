using Proselyte.Sigils;
using System;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    public TripSO trip;
    public TrainStatsSO trainStats;
    public ZoneSpawnerSO spawner;

    public Station[] stations;

    public int stationSpawnFlag;
    private void Awake()
    {
        InitStationManager();
    }
    private void OnEnable()
    {
    }

    private void OnDisable()
    {
        InitStationManager();
    }
    private void Start()
    {
        SpawnFirstStation();
    }
    private void Update()
    {
        if (trainStats.distToNextStation < trainStats.distToSpawnNextStation && (stationSpawnFlag & 1 << trip.curStationIndex) == 0)
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
        float stationXPos = spawner.spawnMaxPos.x - nextStation.platformRenderer.transform.localPosition.x;
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
}
